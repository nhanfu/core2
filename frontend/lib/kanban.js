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
        this.meta = ui;
        this.Columns = [];
        this._textEle = null;
    }
    /**
     * Renders the button component into the DOM.
     */
    render() {
        var template = this.meta.Template;
        var kanbanColumn = JSON.parse(template || "{}");
        if (!this.buttonEle) {
            if (!this.parentElement) throw new Error("parentElement is required");
            Html.take(this.parentElement).div.className("kanban-wrapper").div.className("kanban").render();
            this.element = Html.context;
        } else {
            this.element = this.buttonEle;
        }
        for (const element of kanbanColumn) {
            var column = new KanbanColumn(this.meta, element);
            column.parentElement = this.element;
            column.editForm = this.editForm;
            column.render();
            this.Columns.push(column);
        }
    }
}
