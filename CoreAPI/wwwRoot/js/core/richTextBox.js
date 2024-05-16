import EditableComponent from "./editableComponent";
import { Html } from "./utils/html.js";
import EventType from './models/eventType.js';
import ObservableArgs from './models/observable.js';
import { Uuid7 } from "./structs/uuidv7.js";
import { Component } from "models/component.js";

import "./utils/fix.js";


export class RichTextBox extends EditableComponent {
     /**
     * @param {Component} ui
     * @param {HTMLElement} [ele=null] 
     */
    constructor(ui, ele = null) {
        super(ui,ele);
        this.defaultValue = "";
        if (this.Meta.Row <= 0) {
                this.Meta.Row = 1;
        }
        if (ele != null)
            {
                this.ParentElement = ele ; 
                this.BindingWebComponent();
            }
            else
            {
                this.ParentElement = this.ParentElement ?? Html.Context;
                this.BindingWebComponent();
            }
        
            this.ParentElement.appendChild(this.Element);
    }

    BindingWebComponent() {
        this.Element = Html.Take(this.ParentElement).Div.Id(Uuid7.Id25()).GetContext();
    }

    Render() {
        initCkEditor(this);
    }

    UpdateView(force = false, dirty = null, ...componentNames) {
        const handler = this._events["UpdateView"];
        if (handler) {
            const args = new ObservableArgs();
            args.Com = this;
            args.EvType = 'Change';
            handler(args);
        }
    }
}
