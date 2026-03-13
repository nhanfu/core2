import { Spinner } from "./spinner.js";
import { EditableComponent } from "./editableComponent.js";
import { Component } from "./models/component.js";
import { Html } from "./utils/html.js";
import EventType from "./models/eventType.js";
/**
 * Represents a button component that can be rendered and managed on a web page.
 */
export class KanbanItem extends EditableComponent {
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
        this.Columns = [];
        this._textEle = null;
    }
    /**
     * Renders the button component into the DOM.
     */
    render() {
        Html.take(this.parentElement);
        if (this.Top) {
            var itemTop = Html.div.className("kanban-item").context;
            this.element = Html.context;
            Html.context["Entity"] = this.entity;
            Html.event("dblclick", () => this.dispatchEvent(this.meta.events, EventType.dblClick, this, this.entity))
                .div.className("labels").div.className("labels2");
            if (this.entity.categoryId) {
                Html.div.className("label").style("background-color:" + this.entity.categoryId).attr("title", this.entity.categoryIdText).end.render();
            }
            if (this.entity.priorityLevelId) {
                Html.div.className("label").style("background-color:" + this.entity.priorityLevelId).attr("title", this.entity.priorityLevelIdText).end.render();
            }
            Html.end.render();
            if (this.entity.avatarReceiver) {
                Html.div.className("user-avatar label2").img.src(this.entity.avatarReceiver).end.a.className("full-name").text(this.entity.fullNameReceiver).end.end.render();
            }
            Html.end.render();
            if (this.entity.Code) {
                Html.div.className("bold").text(this.entity.Code).end.render();
            }
            Html.div.text(this.entity.jobName).end.render();
            if (this.entity.Tags) {
                Html.div.className("tag text-xs").text(this.entity.Tags).end.render();
            }
            Html.div.className("user-avatar")
                .img.src(this.entity.Avatar).end
                .a.className("full-name").text(this.entity.fullName).end
                .span.className("created-date").text(this.dayjs(this.entity.insertedDate).format("DD/MM/YY HH:MM"));
            this.parentElement.prepend(itemTop);
        }
        else {
            Html.div.className("kanban-item");
            this.element = Html.context;
            Html.context["Entity"] = this.entity;
            Html.event("dblclick", () => this.dispatchEvent(this.meta.events, EventType.dblClick, this, this.entity))
                .div.className("labels").div.className("labels2");
            if (this.entity.categoryId) {
                Html.div.className("label").style("background-color:" + this.entity.categoryId).attr("title", this.entity.categoryIdText).end.render();
            }
            if (this.entity.priorityLevelId) {
                Html.div.className("label").style("background-color:" + this.entity.priorityLevelId).attr("title", this.entity.priorityLevelIdText).end.render();
            }
            Html.end.render();
            if (this.entity.avatarReceiver) {
                Html.div.className("user-avatar label2").img.src(this.entity.avatarReceiver).end.a.className("full-name").text(this.entity.fullNameReceiver).end.end.render();
            }
            Html.end.render();
            if (this.entity.Code) {
                Html.div.className("bold").text(this.entity.Code).end.render();
            }
            Html.div.iText(this.entity.jobName).end.render();
            if (this.entity.Tags) {
                Html.div.className("tag text-xs").text(this.entity.Tags).end.render();
            }
            Html.div.className("user-avatar")
                .img.src(this.entity.Avatar).end
                .a.className("full-name").text(this.entity.fullName).end
                .span.className("created-date").text(this.dayjs(this.entity.insertedDate).format("DD/MM/YY HH:mm"));
        }
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

    updateView() {
        Html.take(this.element);
        Html.clear();
        Html.div.className("labels").div.className("labels2");
        if (this.entity.categoryId) {
            Html.div.className("label").style("background-color:" + this.entity.categoryId).attr("title", this.entity.categoryIdText).end.render();
        }
        if (this.entity.priorityLevelId) {
            Html.div.className("label").style("background-color:" + this.entity.priorityLevelId).attr("title", this.entity.priorityLevelIdText).end.render();
        }
        Html.end.render();
        if (this.entity.avatarReceiver) {
            Html.div.className("user-avatar label2").img.src(this.entity.avatarReceiver).end.a.className("full-name").text(this.entity.fullNameReceiver).end.end.render();
        }
        Html.end.render();
        if (this.entity.Code) {
            Html.div.className("bold").text(this.entity.Code).end.render();
        }
        Html.div.iText(this.entity.jobName).end.render();
        if (this.entity.Tags) {
            Html.div.className("tag text-xs").text(this.entity.Tags).end.render();
        }
        Html.div.className("user-avatar").img.src(this.entity.Avatar).end.a.className("full-name").text(this.entity.fullName).end.span.className("created-date").text(this.dayjs(this.entity.insertedDate).format("DD/MM/YY HH:MM")).end.end
        Html.end.render();
    }
}
