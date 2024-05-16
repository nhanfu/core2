import { Client } from "./clients/client.js";
import EditableComponent from "./editableComponent.js";
import { Component } from "./models/component.js";
import { Uuid7 } from "./structs/uuidv7.js";
import { ComponentExt } from "./utils/componentExt.js";
import { Str } from "./utils/ext.js";
import { Html } from "./utils/html.js";
import "./utils/fix.js";
import ObservableArgs from "models/observable.js";
import EventType from "models/eventType.js";

/**
 * Represents a code editor component.
 */
export class CodeEditor extends EditableComponent {
    /**
     * Creates an instance of a CodeEditor.
     * @param {Component} ui - The UI component.
     * @param {HTMLElement} [ele=null] - The HTML element associated with the editor.
     */
    constructor(ui, ele = null) {
        super(ui);
        this.Element = ele || null;
        this.DefaultValue = '';
        this.editor = null;
    }

    /**
     * Renders the code editor.
     */
    Render() {
        if (!this.Element) {
            this.ParentElement.style.textAlign = 'unset';
            Html.Take(this.ParentElement).Div.Id(Uuid7.Id25());
            this.Element = Html.Context;
        }
            this.editor = initCodeEditor(this);
    }

    /**
     * Updates the view of the editor.
     * @param {boolean} [force=false] - Whether to force the update.
     * @param {boolean|null} [dirty=null] - Whether the view is considered dirty.
     * @param {string[]} componentNames - Names of the components to update.
     */
    updateView(force = false, dirty = null, ...componentNames) {
        const handler = this._events?.[this.constructor.name]; 
            const args = new ObservableArgs();
            args.Com = this;
            args.EvType = 'Change';
            handler(args);
    }
}
