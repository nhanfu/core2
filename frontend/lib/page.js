import { Html } from "./utils/html";
import { Utils } from "./utils/utils";
import { EditForm } from "./editForm";
import React from "react";
import { createRoot } from 'react-dom/client';
import { Feature } from "./models";
import { flushSync } from "react-dom";

export class Page {
    /**
     * @type {Feature}
     */
    Meta
    /**
     * @type {object}
     */
    Entity
    /**
     * @type {HTMLElement}
     */
    Element
    /**
     * @type {EditForm}
     */
    EditForm
    /**
     * @type {HTMLElement}
     */
    parentElement

    constructor(meta) {
        this.meta = meta || {};
    }

    async render() {
        Html.take(this.parentElement ?? this.meta.parentElement ?? document.body);
        Html.instance.clear();
        Html.instance.div.render();
        this.element = Html.context;
        let root = createRoot(this.element);
        let reactElement = React.createElement(this.meta.layout);
        flushSync(() => root.render(reactElement))
    }
}