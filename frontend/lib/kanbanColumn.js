import { Spinner } from "./spinner.js";
import { EditableComponent } from "./editableComponent.js";
import { Component } from "./models/component.js";
import { Html } from "./utils/html.js";
import Sortable from "sortablejs";
import { Utils } from "./utils/utils.js";
import { Client } from "./clients/client.js";
import { KanbanItem } from "./kanbanItem.js";
import Decimal from "decimal.js";

/**
 * Represents a button component that can be rendered and managed on a web page.
 */
export class KanbanColumn extends EditableComponent {
    Items;
    /**
     * @type {HTMLElement}
     */
    mainSection;
    /**
     * @type {HTMLElement}
     */
    paginationSection;
    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, entity) {
        super(ui);
        /** @type {Component} */
        this.meta = ui;
        this.entity = entity;
        this.Items = [];
        this._textEle = null;
        this.Options = {
            pageIndex: 0,
            pageSize: 50,
            Total: 0,
            currentPageCount: 0,
            startIndex: 1,
            endIndex: 50,
            clickHandler: null
        };
    }

    handleMessage(data) {
        if (data.Message.updatedBy === this.Token.userId) {
            const item = this.Items.find(x => x.Entity.Id === data.Message.Id);
            if (item) {
                item.Entity = data.Message;
                item.updateView();
            }
            return;
        }
        const item = this.Items.find(x => x.Entity.Id === data.Message.Id);
        if (item) {
            if (this.entity.Id !== data.Message.progressId) {
                this.Items = this.Items.filter(x => x !== item);
                item.element?.remove();
            } else {
                item.element?.remove();
                this.Items = this.Items.filter(x => x !== item);
                const column = new KanbanItem(this.meta, data.Message);
                column.parentElement = this.mainSection;
                column.editForm = this.editForm;
                column.Top = true;
                column.render();
                this.Items.push(column);
            }
        } else {
            if (this.entity.Id === data.Message.progressId) {
                const column = new KanbanItem(this.meta, data.Message);
                column.parentElement = this.mainSection;
                column.Top = true;
                column.editForm = this.editForm;
                column.render();
                this.Items.push(column);
            }
        }
    }

    /**
     * Renders the button component into the DOM.
     */
    render() {
        var group = this.meta.Id;
        Html.take(this.parentElement).div.className("kanban-column");
        this.element = Html.context;
        Html.h2.text(this.entity.Title).end.div.className("kanban-items").render();
        Html.context["Entity"] = this.entity;
        this.mainSection = Html.context;
        new Sortable(this.mainSection, {
            group: group,
            handle: ".kanban-item",
            animation: 150,
            ghostClass: "blue-background-class",
            autoScroll: true,
            scrollSensitivity: 30,
            scrollSpeed: 10,
            onMove: function (evt) {
                return evt.from !== evt.to;
            },
            onEnd: evt => {
                const item = evt.item;
                const fromContainer = evt.from;
                const toContainer = evt.to;
                if (toContainer == fromContainer) {
                    return;
                }
                item.Entity.progressId = toContainer.Entity.Id;
                const patchModel = this.getPatchEntity(item.Entity);
                Client.instance.patchAsync(patchModel);
            }
        });
        this.renderItemData();
        Html.end.div.className("kanban-pagination");
        this.paginationSection = Html.context;
        if (this.entity.canAdd) {
            Html.take(this.mainSection.parentElement).button.className("add-task").event("click", () => this.dispatchClick()).iText("+ Add Task").end.render()
        }
    }

    /**
     * Updates pagination details based on total data and current page count.
     * @param {number} total The total number of records.
     * @param {number} currentPageCount The number of records in the current page.
     */
    updatePagination(total, currentPageCount) {
        this.Options.Total = total;
        this.Options.currentPageCount = currentPageCount;
        this.Options.pageNumber = (this.Options.pageIndex || 0) + 1;
        this.Options.startIndex = (this.Options.pageIndex || 0) * (this.Options.pageSize || 50) + 1;
        this.Options.endIndex = this.Options.startIndex + this.Options.currentPageCount - 1;
        Html.take(this.paginationSection);
        Html.clear();
        Html.button.className("prev-page").event("click", this.prevPage.bind(this)).text("←").end.span.className("page-info").text(this.Options.startIndex + " - " + this.Options.endIndex + " of " + this.Options.Total).end.button.className("next-page").event("click", this.nextPage.bind(this)).text("→").end.end.render();
    }

    getPatchEntity(entity) {
        var dirtyPatch = [];
        var row = entity;
        Object.getOwnPropertyNames(row).forEach(cell => {
            if (row[cell] instanceof Array || (row[cell] instanceof Object && !(row[cell] instanceof Decimal) && !(row[cell] instanceof Date)) || cell == this._groupKey) {
                return;
            }
            let val;
            if (typeof row[cell] === "boolean") {
                val = row[cell] ? "1" : "0";
            } else {
                val = row[cell];
            }

            let patchDetail = {};
            patchDetail.Label = cell;
            patchDetail.Field = cell;
            patchDetail.oldVal = null;
            patchDetail.Value = val;
            dirtyPatch.push(patchDetail);
        });
        return {
            Changes: dirtyPatch,
            Table: "shipmentTask"
        };
    }

    renderItemData() {
        this.Items = [];
        this.mainSection.innerHTML = "";
        this.reloadData().then((data) => {
            if (Array.isArray(data) && data.length > 0) {
                for (const item of data) {
                    var column = new KanbanItem(this.meta, item);
                    column.parentElement = this.mainSection;
                    column.editForm = this.editForm;
                    column.render();
                    this.Items.push(column);
                }
            }
        });
    }

    async reloadData() {
        let sql = this.getSql(JSON.stringify(this.entity));
        return await this.customQuery(sql);
    }

    async customQuery(vm) {
        const data = await Client.instance.submitAsync({
            noQueue: true,
            Url: `/api/feature/com`,
            Method: "POST",
            jsonData: JSON.stringify(vm),
        });
        this.updatePagination(data.count, !data.value ? 0 : data.value.length);
        if (!data.value || data.value.length === 0) {
            return [];
        }
        else {
            let rows = [...data.value];
            return rows;
        }
    }

    /**
     * Handle the event for navigating to the next page.
     */
    nextPage() {
        const pages = Math.ceil(this.Options.Total / this.Options.pageSize);
        if (this.Options.pageNumber >= pages) return;

        this.Options.pageIndex++;
        if (this.Options.clickHandler) this.Options.clickHandler(this.Options.pageIndex, null);
        this.renderItemData();
    }

    /**
     * Handle the event for navigating to the previous page.
     */
    prevPage() {
        if (this.Options.pageIndex <= 0) return;

        this.Options.pageIndex--;
        if (this.Options.clickHandler) this.Options.clickHandler(this.Options.pageIndex, null);
        this.renderItemData();
    }

    getSql(vm) {
        let submitEntity = Utils.isFunction(vm, true, this);
        if (!submitEntity) {
            submitEntity = {};
        }
        var skip = this.Options.pageIndex * this.Options.pageSize;
        /** @type {SqlViewModel} */
        var res = {
            comId: this.meta.Id,
            params: submitEntity ? JSON.stringify(submitEntity) : null,
            orderBy: "ds.insertedDate desc",
            Count: true,
            Skip: skip || 0,
            Top: 50
        };
        return res;
    }

    /**
     * Dispatches the click event, handles UI changes for click action.
     */
    dispatchClick() {
        if (this.meta.onClick) {
            this.meta.onClick.call();
            return;
        }

        if (this.disabled || this.element.hidden) {
            return;
        }
        this.disabled = true;
        try {
            Spinner.appendTo();
            this.dispatchEvent(this.meta.events, "click", this, this.entity).then(() => {
                this.disabled = false;
                Spinner.Hide();
            });
        } finally {
            window.setTimeout(() => {
                this.disabled = false;
            }, 2000);
        }
    }

    /**
     * Gets the value text from the button component.
     * @returns {string} The text value of the component.
     */
    getValueText() {
        if (!this.entity || !this.Name) {
            return this._textEle.textContent;
        }
        return this.fieldVal?.toString();
    }
}
