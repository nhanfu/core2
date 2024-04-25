import { ComponentType } from "../models/componentType";

export class HTMLInputElement {
    constructor() {
        this.selectionStart = 0;
        this.selectionEnd = 0;
    }
    tagName = ComponentType.Input;
    /** @type { {[key: string] : any} } */
    _attr = {};
    _value = '';
    get value() { return this._value; }
    set value(val) { return this._value = val?.toString(); }
    /**
     * 
     * @param {String} event - Event name
     * @param {Function} handler - Function handle the event
     */
    addEventListener(event, handler) {
        this['on' + event] = this['on' + event] ?? [];
        this['on' + event].push(handler);
    }
    trigger(event) {
        if (this['on' + event]) {
            this['on' + event].forEach(x => x.call(this, { target: this }));
        }
    }
    setAttribute(name, value) {
        this._attr[name] = value;
    }
    getAttribute(name) {
        return this._attr[name];
    }
    removeAttribute(name) {
        delete this._attr[name];
    }
    closest(selector) {

    }
};