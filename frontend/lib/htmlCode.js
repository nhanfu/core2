import { Component } from "./models/component.js";
import { EditableComponent } from "./editableComponent.js";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { Client } from "./clients/client.js";
export class HtmlCode extends EditableComponent {
    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui, ele);
    }

    render() {
        this.element = Html.take(this.parentElement).div.getContext();
        const submitEntity = Utils.isFunction(this.meta.preQuery, false, this);
        const entity = {
            params: submitEntity,
            comId: this.meta.id,
        };
        Client.instance.submitAsync({
            url: "/api/feature/report",
            isRawString: true,
            jsonData: JSON.stringify(entity, this.getCircularReplacer(), 2),
            method: "POST"
        }).then(data => {
            this.element.innerHTML = Utils.getHtmlCode(this.meta.template, data.updatedItem);
        })
    }

    updateView(force = false, dirty = null, componentNames) {
        this.render();
    }
}