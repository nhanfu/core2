import { Spinner } from "./spinner.js";
import { EditableComponent } from "./editableComponent.js";
import { Component } from "./models/component.js";
import { Html } from "./utils/html.js";
import Sortable from "sortablejs";
import { Utils } from "./utils/utils.js";
import { Client } from "./clients/client.js";
import { EditForm } from "./index.js";
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
    MainSection;
    /**
     * @type {HTMLElement}
     */
    PaginationSection;
    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, entity) {
        super(ui);
        /** @type {Component} */
        this.Meta = ui;
        this.Entity = entity;
        this.Items = [];
        this._textEle = null;
        this.Options = {
            PageIndex: 0,
            PageSize: 50,
            Total: 0,
            CurrentPageCount: 0,
            StartIndex: 1,
            EndIndex: 50,
            ClickHandler: null
        };
    }

    HandleMessage(data) {
        if (data.Message.UpdatedBy === this.Token.UserId) {
            const item = this.Items.find(x => x.Entity.Id === data.Message.Id);
            if (item) {
                item.Entity = data.Message;
                item.UpdateView();
            }
            return;
        }
        const item = this.Items.find(x => x.Entity.Id === data.Message.Id);
        if (item) {
            if (this.Entity.Id !== data.Message.ProgressId) {
                this.Items = this.Items.filter(x => x !== item);
                item.Element?.remove();
            } else {
                item.Element?.remove();
                this.Items = this.Items.filter(x => x !== item);
                const column = new KanbanItem(this.Meta, data.Message);
                column.ParentElement = this.MainSection;
                column.EditForm = this.EditForm;
                column.Top = true;
                column.Render();
                this.Items.push(column);
            }
        } else {
            if (this.Entity.Id === data.Message.ProgressId) {
                const column = new KanbanItem(this.Meta, data.Message);
                column.ParentElement = this.MainSection;
                column.Top = true;
                column.EditForm = this.EditForm;
                column.Render();
                this.Items.push(column);
            }
        }
    }

    /**
     * Renders the button component into the DOM.
     */
    Render() {
        window.setTimeout(() => {
            var evt = "ShipmentTask" + this.Entity.Id;
            EditForm.NotificationClient.AddListener(evt, this.HandleMessage.bind(this));
        }, 1000);
        var group = this.Meta.Id;
        Html.Take(this.ParentElement).Div.ClassName("kanban-column");
        this.Element = Html.Context;
        Html.H2.Text(this.Entity.Title).End.Div.ClassName("kanban-items").Render();
        Html.Context["Entity"] = this.Entity;
        this.MainSection = Html.Context;
        new Sortable(this.MainSection, {
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
                item.Entity.ProgressId = toContainer.Entity.Id;
                const patchModel = this.GetPatchEntity(item.Entity);
                Client.Instance.PatchAsync(patchModel);
            }
        });
        this.RenderItemData();
        Html.End.Div.ClassName("kanban-pagination");
        this.PaginationSection = Html.Context;
        if (this.Entity.CanAdd) {
            Html.Take(this.MainSection.parentElement).Button.ClassName("add-task").Event("click", () => this.DispatchClick()).IText("+ Add Task").End.Render()
        }
    }

    /**
     * Updates pagination details based on total data and current page count.
     * @param {number} total The total number of records.
     * @param {number} currentPageCount The number of records in the current page.
     */
    UpdatePagination(total, currentPageCount) {
        this.Options.Total = total;
        this.Options.CurrentPageCount = currentPageCount;
        this.Options.PageNumber = (this.Options.PageIndex || 0) + 1;
        this.Options.StartIndex = (this.Options.PageIndex || 0) * (this.Options.PageSize || 50) + 1;
        this.Options.EndIndex = this.Options.StartIndex + this.Options.CurrentPageCount - 1;
        Html.Take(this.PaginationSection);
        Html.Clear();
        Html.Button.ClassName("prev-page").Event("click", this.PrevPage.bind(this)).Text("←").End.Span.ClassName("page-info").Text(this.Options.StartIndex + " - " + this.Options.EndIndex + " of " + this.Options.Total).End.Button.ClassName("next-page").Event("click", this.NextPage.bind(this)).Text("→").End.End.Render();
    }

    GetPatchEntity(entity) {
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
            patchDetail.OldVal = null;
            patchDetail.Value = val;
            dirtyPatch.push(patchDetail);
        });
        return {
            Changes: dirtyPatch,
            Table: "ShipmentTask"
        };
    }

    RenderItemData() {
        this.Items = [];
        this.MainSection.innerHTML = "";
        this.ReloadData().then((data) => {
            if (Array.isArray(data) && data.length > 0) {
                for (const item of data) {
                    var column = new KanbanItem(this.Meta, item);
                    column.ParentElement = this.MainSection;
                    column.EditForm = this.EditForm;
                    column.Render();
                    this.Items.push(column);
                }
            }
        });
    }

    async ReloadData() {
        let sql = this.GetSql(JSON.stringify(this.Entity));
        return await this.CustomQuery(sql);
    }

    async CustomQuery(vm) {
        const data = await Client.Instance.SubmitAsync({
            NoQueue: true,
            Url: `/api/feature/com`,
            Method: "POST",
            JsonData: JSON.stringify(vm),
        });
        this.UpdatePagination(data.count, !data.value ? 0 : data.value.length);
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
    NextPage() {
        const pages = Math.ceil(this.Options.Total / this.Options.PageSize);
        if (this.Options.PageNumber >= pages) return;

        this.Options.PageIndex++;
        if (this.Options.ClickHandler) this.Options.ClickHandler(this.Options.PageIndex, null);
        this.RenderItemData();
    }

    /**
     * Handle the event for navigating to the previous page.
     */
    PrevPage() {
        if (this.Options.PageIndex <= 0) return;

        this.Options.PageIndex--;
        if (this.Options.ClickHandler) this.Options.ClickHandler(this.Options.PageIndex, null);
        this.RenderItemData();
    }

    GetSql(vm) {
        let submitEntity = Utils.IsFunction(vm, true, this);
        if (!submitEntity) {
            submitEntity = {};
        }
        var skip = this.Options.PageIndex * this.Options.PageSize;
        /** @type {SqlViewModel} */
        var res = {
            ComId: this.Meta.Id,
            Params: submitEntity ? JSON.stringify(submitEntity) : null,
            OrderBy: "ds.InsertedDate desc",
            Count: true,
            Skip: skip || 0,
            Top: 50
        };
        return res;
    }

    /**
     * Dispatches the click event, handles UI changes for click action.
     */
    DispatchClick() {
        if (this.Meta.OnClick) {
            this.Meta.OnClick.call();
            return;
        }

        if (this.Disabled || this.Element.hidden) {
            return;
        }
        this.Disabled = true;
        try {
            Spinner.AppendTo();
            this.DispatchEvent(this.Meta.Events, "click", this, this.Entity).then(() => {
                this.Disabled = false;
                Spinner.Hide();
            });
        } finally {
            window.setTimeout(() => {
                this.Disabled = false;
            }, 2000);
        }
    }

    /**
     * Gets the value text from the button component.
     * @returns {string} The text value of the component.
     */
    GetValueText() {
        if (!this.Entity || !this.Name) {
            return this._textEle.textContent;
        }
        return this.FieldVal?.toString();
    }
}
