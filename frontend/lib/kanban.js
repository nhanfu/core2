import { EditableComponent } from "./editableComponent.js";
import { Component } from "./models/component.js";
import { Html } from "./utils/html.js";
import { KanbanColumn } from "./kanbanColumn.js";

/**
 * Represents a button component that can be rendered and managed on a web page.
 */
export class Kanban extends EditableComponent {
    Columns;
    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui);
        /** @type {Component} */
        this.Meta = ui;
        this.Columns = [];
        this._textEle = null;
    }
    /**
     * Renders the button component into the DOM.
     */
    Render() {
        var template = this.Meta.Template;
        var kanbanColumn = JSON.parse(template || "{}");
        if (!this.ButtonEle) {
            if (!this.ParentElement) throw new Error("ParentElement is required");
            Html.Take(this.ParentElement).Div.ClassName("kanban-wrapper").Div.ClassName("kanban").Render();
            this.Element = Html.Context;
        } else {
            this.Element = this.ButtonEle;
        }
        for (const element of kanbanColumn) {
            var column = new KanbanColumn(this.Meta, element);
            column.ParentElement = this.Element;
            column.EditForm = this.EditForm;
            column.Render();
            this.Columns.push(column);
        }
    }
}
