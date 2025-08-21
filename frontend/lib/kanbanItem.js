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
        Html.Take(this.ParentElement);
        if (this.Top) {
            var itemTop = Html.Div.ClassName("kanban-item").Context;
            this.Element = Html.Context;
            Html.Context["Entity"] = this.Entity;
            Html.Event("dblclick", () => this.DispatchEvent(this.Meta.Events, EventType.DblClick, this, this.Entity))
                .Div.ClassName("labels").Div.ClassName("labels2");
            if (this.Entity.CategoryId) {
                Html.Div.ClassName("label").Style("background-color:" + this.Entity.CategoryId).Attr("title", this.Entity.CategoryIdText).End.Render();
            }
            if (this.Entity.PriorityLevelId) {
                Html.Div.ClassName("label").Style("background-color:" + this.Entity.PriorityLevelId).Attr("title", this.Entity.PriorityLevelIdText).End.Render();
            }
            Html.End.Render();
            if (this.Entity.AvatarReceiver) {
                Html.Div.ClassName("user-avatar label2").Img.Src(this.Entity.AvatarReceiver).End.A.ClassName("full-name").Text(this.Entity.FullNameReceiver).End.End.Render();
            }
            Html.End.Render();
            if (this.Entity.Code) {
                Html.Div.ClassName("bold").Text(this.Entity.Code).End.Render();
            }
            Html.Div.Text(this.Entity.JobName).End.Render();
            if (this.Entity.Tags) {
                Html.Div.ClassName("tag text-xs").Text(this.Entity.Tags).End.Render();
            }
            Html.Div.ClassName("user-avatar")
                .Img.Src(this.Entity.Avatar).End
                .A.ClassName("full-name").Text(this.Entity.FullName).End
                .Span.ClassName("created-date").Text(this.dayjs(this.Entity.InsertedDate).format("DD/MM/YY HH:MM"));
            this.ParentElement.prepend(itemTop);
        }
        else {
            Html.Div.ClassName("kanban-item");
            this.Element = Html.Context;
            Html.Context["Entity"] = this.Entity;
            Html.Event("dblclick", () => this.DispatchEvent(this.Meta.Events, EventType.DblClick, this, this.Entity))
                .Div.ClassName("labels").Div.ClassName("labels2");
            if (this.Entity.CategoryId) {
                Html.Div.ClassName("label").Style("background-color:" + this.Entity.CategoryId).Attr("title", this.Entity.CategoryIdText).End.Render();
            }
            if (this.Entity.PriorityLevelId) {
                Html.Div.ClassName("label").Style("background-color:" + this.Entity.PriorityLevelId).Attr("title", this.Entity.PriorityLevelIdText).End.Render();
            }
            Html.End.Render();
            if (this.Entity.AvatarReceiver) {
                Html.Div.ClassName("user-avatar label2").Img.Src(this.Entity.AvatarReceiver).End.A.ClassName("full-name").Text(this.Entity.FullNameReceiver).End.End.Render();
            }
            Html.End.Render();
            if (this.Entity.Code) {
                Html.Div.ClassName("bold").Text(this.Entity.Code).End.Render();
            }
            Html.Div.IText(this.Entity.JobName).End.Render();
            if (this.Entity.Tags) {
                Html.Div.ClassName("tag text-xs").Text(this.Entity.Tags).End.Render();
            }
            Html.Div.ClassName("user-avatar")
                .Img.Src(this.Entity.Avatar).End
                .A.ClassName("full-name").Text(this.Entity.FullName).End
                .Span.ClassName("created-date").Text(this.dayjs(this.Entity.InsertedDate).format("DD/MM/YY HH:mm"));
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
        Html.Take(this.Element);
        Html.Clear();
        Html.Div.ClassName("labels").Div.ClassName("labels2");
        if (this.Entity.CategoryId) {
            Html.Div.ClassName("label").Style("background-color:" + this.Entity.CategoryId).Attr("title", this.Entity.CategoryIdText).End.Render();
        }
        if (this.Entity.PriorityLevelId) {
            Html.Div.ClassName("label").Style("background-color:" + this.Entity.PriorityLevelId).Attr("title", this.Entity.PriorityLevelIdText).End.Render();
        }
        Html.End.Render();
        if (this.Entity.AvatarReceiver) {
            Html.Div.ClassName("user-avatar label2").Img.Src(this.Entity.AvatarReceiver).End.A.ClassName("full-name").Text(this.Entity.FullNameReceiver).End.End.Render();
        }
        Html.End.Render();
        if (this.Entity.Code) {
            Html.Div.ClassName("bold").Text(this.Entity.Code).End.Render();
        }
        Html.Div.IText(this.Entity.JobName).End.Render();
        if (this.Entity.Tags) {
            Html.Div.ClassName("tag text-xs").Text(this.Entity.Tags).End.Render();
        }
        Html.Div.ClassName("user-avatar").Img.Src(this.Entity.Avatar).End.A.ClassName("full-name").Text(this.Entity.FullName).End.Span.ClassName("created-date").Text(this.dayjs(this.Entity.InsertedDate).format("DD/MM/YY HH:MM")).End.End
        Html.End.Render();
    }
}
