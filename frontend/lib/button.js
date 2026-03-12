import { Spinner } from "./spinner.js";
import { EditableComponent } from "./editableComponent.js";
import { Component } from "./models/component.js";
import { html } from "./utils/html.js";

/**
 * represents a button component that can be rendered and managed on a web page.
 */
export class Button extends EditableComponent {
    isButton = true;
    /**
     * create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui);
        /** @type {Component} */
        this.buttonEle = ele;
        this._textEle = null;
    }

    /**
     * renders the button component into the dOM.
     */
    render() {
        if (!this.buttonEle) {
            if (!this.parentElement) throw new error("parentElement is required");
            html.take(this.parentElement).button.render();
            this.element = this.buttonEle = html.context;
        } else {
            this.element = this.buttonEle;
        }

        html.take(this.element)
            .className(this.meta.className)
            .event("click", () => this.dispatchClick())
            .style(this.meta.style);

        if (this.meta.icon) {
            html.icon(this.meta.icon).end.text(" ").render();
        }

        html.span.className("caption").iText(this.meta.label || "", this.editForm.meta.label);
        this._textEle = html.context;

        this.element.closest("td")?.addEventListener("keydown", e => this.listViewItemTab(e));
        this.dOMContentLoaded?.invoke();
    }

    /**
     * dispatches the click event, handles uI changes for click action.
     */
    dispatchClick() {
        if (this.meta.onClick) {
            this.meta.onClick.call();
            return;
        }

        if (this.disabled || this.element.hidden) {
            return;
        }
        this.disabled = true;
        try {
            this.dispatchEvent(this.meta.events, "click", this, this.entity).then(() => {
            });
        } finally {
            window.setTimeout(() => {
                this.disabled = false;
            }, 500);
        }
    }

    /**
     * gets the value text from the button component.
     * @returns {string} the text value of the component.
     */
    getValueText() {
        if (!this.entity || !this.name) {
            return this._textEle.textContent;
        }
        return this.fieldVal?.toString();
    }
}
