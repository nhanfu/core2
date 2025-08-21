import { EditableComponent } from "./editableComponent.js";
import { Component } from "./models/component.js";
import { Html } from "./utils/html.js";
import { ComponentFactory } from "./utils/componentFactory.js";

/**
 * Represents a button component that can be rendered and managed on a web page.
 */
export class ActionButton extends EditableComponent {
    IsButton = true;
    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui);
        /** @type {Component} */
        this.Meta = ui;
        this.ButtonEle = ele;
        this._textEle = null;
    }

    /**
     * Renders the button component into the DOM.
     */
    Render() {
        if (!this.ButtonEle) {
            Html.Take(this.ParentElement).ClassName("btn-group-view").Render();
            this.Element = this.ButtonEle = Html.Context;
        } else {
            this.Element = this.ButtonEle;
        }
        var childs = JSON.parse(this.Meta.FormatData) || [];
        for (let i = 0; i < childs.length; i++) {
            const child = childs[i];
            var newChid = Object.assign({}, this.Meta, child);
            const childCom = ComponentFactory.GetComponent(newChid, this.EditForm);
            if (childCom === null) return;
            childCom.ParentElement = this.Element;
            this.AddChild(childCom);
        }
    }
}
