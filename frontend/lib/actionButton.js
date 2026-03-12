import { EditableComponent } from "./editableComponent.js";
import { Component } from "./models/component.js";
import { html } from "./utils/html.js";
import { ComponentFactory } from "./utils/componentFactory.js";

/**
 * represents a button component that can be rendered and managed on a web page.
 */
export class ActionButton extends EditableComponent {
    isButton = true;
    /**
     * create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui);
        /** @type {Component} */
        this.meta = ui;
        this.buttonEle = ele;
        this.isAction = true;
        this._textEle = null;
    }

    /**
     * renders the button component into the dOM.
     */
    render() {
        if (!this.buttonEle) {
            html.take(this.parentElement).className("btn-group-view").render();
            this.element = this.buttonEle = html.context;
        } else {
            this.element = this.buttonEle;
        }
        var childs = JSON.parse(this.meta.formatData) || [];
        for (let i = 0; i < childs.length; i++) {
            const child = childs[i];
            var newChid = object.assign({}, this.meta, child);
            const childCom = ComponentFactory.getComponent(newChid, this.editForm);
            if (childCom === null) return;
            childCom.parentElement = this.element;
            childCom.isAction = true;
            this.addChild(childCom);
        }
    }
}
