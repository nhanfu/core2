import { Component } from "./models/component.js";
import { Client } from "./clients/client.js";
import { EditableComponent } from "./editableComponent.js";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { LangSelect } from "./utils/langSelect.js";


export class Label extends EditableComponent {
    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui, ele);
    }

    render() {
        this.setDefaultVal();
        const cellData = this.entity[this.meta.fieldName];
        let cellText = '';
        if (!this.element) {
            this.renderNewEle(cellText, cellData);
        }
        if (this.meta && this.meta.componentType == "Checkbox") {
            Html.take(this.element).smallCheckbox(cellData, true);
            this.originalText = cellData ? "✅" : "☐";
            return;
        }
        var textCalc = this.meta && this.meta.formatData && (this.meta.formatData.includes(".") || this.meta.formatData.includes("return")) ? Utils.isFunction(this.meta.formatData || '', false, this) : "";
        if (textCalc) {
            cellText = textCalc;
            if (this.meta.componentType == "Input") {
                this.element.textContent = this.getTextContent(cellText);
            }
            else {
                this.element.innerHTML = cellText;
            }
            this.element.title = cellText;
        }
        else {
            this.calcCellText(cellData);
        }
        this.setOldTextAndVal();
    }

    renderNewEle(cellText, cellData) {
        if (this.meta.componentType == "Number") {
            Html.instance.style("justify-content: end;");
        }
        if (!this.meta.isMultiple) {
            if (!cellText.includes("<div") && this.meta.componentType != "Checkbox" && ((this.meta.formatData && !this.meta.formatData.includes("<div")) || !this.meta.formatData)) {
                Html.instance.span.className("cell-text").render();
            }
            if (this.meta.componentType == "Input") {
                Html.instance.title(cellText).text(this.getTextContent(cellText));
            }
            else {
                Html.instance.title(cellText).innerHTML(cellText);
            }
        }
        this.element = Html.context;
        Html.instance.end.render();
    }

    calcCellText(cellData) {
        if (this.meta && this.meta.componentType == "Checkbox") {
            Html.take(this.element).clear();
            Html.take(this.element).smallCheckbox(cellData, true);
            this.originalText = cellData ? "✅" : "☐";
            return;
        }
        if (this.meta.Query && this.meta.componentType == "Label") {
            this.runQuerys().then((data) => {
                if (data[0]) {
                    var cellText = Utils.getCellText(this.meta, cellData, data[0][0], false, this.emptyRow, this.editForm?.entity);
                    if (!cellText || cellText == "null") {
                        cellText = "";
                    }
                    this.element.innerHTML = cellText;
                }
            });
        }
        else {
            var cellText = Utils.getCellText(this.meta, cellData, this.entity, false, this.emptyRow, this.editForm?.entity);
            if (!cellText || cellText == "null") {
                cellText = "";
            }
            if (this.meta.componentType == "Input") {
                this.element.textContent = this.getTextContent(cellText);
            }
            else {
                this.element.innerHTML = cellText;
            }
            this.element.title = cellText;
        }
    }

    getTextContent(element) {
        const doc = new dOMParser().parseFromString(element, 'text/html');
        return doc.body.textContent || '';
    }

    labelClickHandler(e) {
        this.dispatchEvent(this.meta.events, "click", this, this.entity).then();
    }

    /**
     * 
     * @param {Component} header 
     * @param {any} cellData 
     * @returns {string}
     */
    calcTextAlign(header, cellData) {
        const textAlign = header.textAlignEnum;
        if (textAlign) {
            return textAlign;
        }
        if (header.componentType == "Dropdown" || header.componentType == "Select2" || cellData === null || typeof cellData === "string") {
            return "left";
        }
        if (typeof cellData === "number") {
            return "right";
        }
        if (typeof cellData === "boolean") {
            return "center";
        }
        return "center";
    }

    updateView(force = false, dirty = null, componentNames) {
        this.prepareUpdateView(force, dirty);
        const cellData = this.entity[this.meta.fieldName];
        var cellText = "";
        var textCalc = this.meta && this.meta.formatData && (this.meta.formatData.includes(".") || this.meta.formatData.includes("return")) ? Utils.isFunction(this.meta.formatData || '', false, this) : "";
        if (textCalc) {
            cellText = textCalc;
            if (this.meta.componentType == "Input") {
                this.element.textContent = this.getTextContent(cellText);
            }
            else {
                this.element.innerHTML = cellText;
            }
            this.element.title = cellText;
        }
        else {
            this.calcCellText(cellData);
            this.element.title = "";
        }
        if (!this.Dirty) {
            this.originalText = this.getTextContent(cellText);
            this.dOMContentLoaded?.invoke();
            this.oldValue = this.getTextContent(cellText);
        }
    }

    getValueText() {
        return this.element.textContent;
    }
}