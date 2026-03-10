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
        this.Meta = ui;
        this.Entity = entity;
        this.Columns = [];
        this._textEle = null;
    }
    /**
     * Renders the button component into the DOM.
     */
    Render() {
        Html.take(this.ParentElement);
        if (this.Top) {
            var itemTop = Html.div.className("kanban-item").Context;
            this.Element = Html.Context;
            Html.Context["Entity"] = this.Entity;
            Html.event("dblclick", () => this.DispatchEvent(this.Meta.Events, EventType.DblClick, this, this.Entity))
                .div.className("labels").div.className("labels2");
            if (this.Entity.CategoryId) {
                Html.div.className("label").style("background-color:" + this.Entity.CategoryId).attr("title", this.Entity.CategoryIdText).end.render();
            }
            if (this.Entity.PriorityLevelId) {
                Html.div.className("label").style("background-color:" + this.Entity.PriorityLevelId).attr("title", this.Entity.PriorityLevelIdText).end.render();
            }
            Html.end.render();
            if (this.Entity.AvatarReceiver) {
                Html.div.className("user-avatar label2").img.src(this.Entity.AvatarReceiver).end.a.className("full-name").text(this.Entity.FullNameReceiver).end.end.render();
            }
            Html.end.render();
            if (this.Entity.Code) {
                Html.div.className("bold").text(this.Entity.Code).end.render();
            }
            Html.div.text(this.Entity.JobName).end.render();
            if (this.Entity.Tags) {
                Html.div.className("tag text-xs").text(this.Entity.Tags).end.render();
            }
            Html.div.className("user-avatar")
                .img.src(this.Entity.Avatar).end
                .a.className("full-name").text(this.Entity.FullName).end
                .span.className("created-date").text(this.dayjs(this.Entity.InsertedDate).format("DD/MM/YY HH:MM"));
            this.ParentElement.prepend(itemTop);
        }
        else {
            Html.div.className("kanban-item");
            this.Element = Html.Context;
            Html.Context["Entity"] = this.Entity;
            Html.event("dblclick", () => this.DispatchEvent(this.Meta.Events, EventType.DblClick, this, this.Entity))
                .div.className("labels").div.className("labels2");
            if (this.Entity.CategoryId) {
                Html.div.className("label").style("background-color:" + this.Entity.CategoryId).attr("title", this.Entity.CategoryIdText).end.render();
            }
            if (this.Entity.PriorityLevelId) {
                Html.div.className("label").style("background-color:" + this.Entity.PriorityLevelId).attr("title", this.Entity.PriorityLevelIdText).end.render();
            }
            Html.end.render();
            if (this.Entity.AvatarReceiver) {
                Html.div.className("user-avatar label2").img.src(this.Entity.AvatarReceiver).end.a.className("full-name").text(this.Entity.FullNameReceiver).end.end.render();
            }
            Html.end.render();
            if (this.Entity.Code) {
                Html.div.className("bold").text(this.Entity.Code).end.render();
            }
            Html.div.iText(this.Entity.JobName).end.render();
            if (this.Entity.Tags) {
                Html.div.className("tag text-xs").text(this.Entity.Tags).end.render();
            }
            Html.div.className("user-avatar")
                .img.src(this.Entity.Avatar).end
                .a.className("full-name").text(this.Entity.FullName).end
                .span.className("created-date").text(this.dayjs(this.Entity.InsertedDate).format("DD/MM/YY HH:mm"));
        }
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

    UpdateView() {
        Html.take(this.Element);
        Html.clear();
        Html.div.className("labels").div.className("labels2");
        if (this.Entity.CategoryId) {
            Html.div.className("label").style("background-color:" + this.Entity.CategoryId).attr("title", this.Entity.CategoryIdText).end.render();
        }
        if (this.Entity.PriorityLevelId) {
            Html.div.className("label").style("background-color:" + this.Entity.PriorityLevelId).attr("title", this.Entity.PriorityLevelIdText).end.render();
        }
        Html.end.render();
        if (this.Entity.AvatarReceiver) {
            Html.div.className("user-avatar label2").img.src(this.Entity.AvatarReceiver).end.a.className("full-name").text(this.Entity.FullNameReceiver).end.end.render();
        }
        Html.end.render();
        if (this.Entity.Code) {
            Html.div.className("bold").text(this.Entity.Code).end.render();
        }
        Html.div.iText(this.Entity.JobName).end.render();
        if (this.Entity.Tags) {
            Html.div.className("tag text-xs").text(this.Entity.Tags).end.render();
        }
        Html.div.className("user-avatar").img.src(this.Entity.Avatar).end.a.className("full-name").text(this.Entity.FullName).end.span.className("created-date").text(this.dayjs(this.Entity.InsertedDate).format("DD/MM/YY HH:MM")).end.end
        Html.end.render();
    }
}
