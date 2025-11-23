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

    Render() {
        this.SetDefaultVal();
        const cellData = this.Entity[this.Meta.FieldName];
        let cellText = '';
        if (!this.Element) {
            this.RenderNewEle(cellText, cellData);
        }
        if (this.Meta && this.Meta.ComponentType == "Checkbox") {
            Html.Take(this.Element).SmallCheckbox(cellData, true);
            this.OriginalText = cellData ? "✅" : "☐";
            return;
        }
        var textCalc = this.Meta && this.Meta.FormatData && (this.Meta.FormatData.includes(".") || this.Meta.FormatData.includes("return")) ? Utils.IsFunction(this.Meta.FormatData || '', false, this) : "";
        if (textCalc) {
            cellText = textCalc;
            if (this.Meta.ComponentType == "Input") {
                this.Element.textContent = this.getTextContent(cellText);
            }
            else {
                this.Element.innerHTML = cellText;
            }
            this.Element.title = cellText;
        }
        else {
            this.CalcCellText(cellData);
        }
        this.SetOldTextAndVal();
    }

    RenderNewEle(cellText, cellData) {
        if (this.Meta.ComponentType == "Number") {
            Html.Instance.Style("justify-content: end;");
        }
        if (!this.Meta.IsMultiple) {
            if (!cellText.includes("<div") && this.Meta.ComponentType != "Checkbox" && ((this.Meta.FormatData && !this.Meta.FormatData.includes("<div")) || !this.Meta.FormatData)) {
                Html.Instance.Span.ClassName("cell-text").Render();
            }
            if (this.Meta.ComponentType == "Input") {
                Html.Instance.Title(cellText).Text(this.getTextContent(cellText));
            }
            else {
                Html.Instance.Title(cellText).InnerHTML(cellText);
            }
        }
        this.Element = Html.Context;
        Html.Instance.End.Render();
    }

    CalcCellText(cellData) {
        if (this.Meta && this.Meta.ComponentType == "Checkbox") {
            Html.Take(this.Element).Clear();
            Html.Take(this.Element).SmallCheckbox(cellData, true);
            this.OriginalText = cellData ? "✅" : "☐";
            return;
        }
        if (this.Meta.Query && this.Meta.ComponentType == "Label") {
            this.RunQuerys().then((data) => {
                if (data[0]) {
                    var cellText = Utils.GetCellText(this.Meta, cellData, data[0][0], false, this.EmptyRow, this.EditForm?.Entity);
                    if (!cellText || cellText == "null") {
                        cellText = "";
                    }
                    this.Element.innerHTML = cellText;
                }
            });
        }
        else {
            var cellText = Utils.GetCellText(this.Meta, cellData, this.Entity, false, this.EmptyRow, this.EditForm?.Entity);
            if (!cellText || cellText == "null") {
                cellText = "";
            }
            if (this.Meta.ComponentType == "Input") {
                this.Element.textContent = this.getTextContent(cellText);
            }
            else {
                this.Element.innerHTML = cellText;
            }
            this.Element.title = cellText;
        }
    }

    getTextContent(element) {
        const doc = new DOMParser().parseFromString(element, 'text/html');
        return doc.body.textContent || '';
    }

    LabelClickHandler(e) {
        this.DispatchEvent(this.Meta.Events, "click", this, this.Entity).then();
    }

    /**
     * 
     * @param {Component} header 
     * @param {any} cellData 
     * @returns {string}
     */
    CalcTextAlign(header, cellData) {
        const textAlign = header.TextAlignEnum;
        if (textAlign) {
            return textAlign;
        }
        if (header.ComponentType == "Dropdown" || header.ComponentType == "Select2" || cellData === null || typeof cellData === "string") {
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

    UpdateView(force = false, dirty = null, componentNames) {
        this.PrepareUpdateView(force, dirty);
        const cellData = this.Entity[this.Meta.FieldName];
        var cellText = "";
        var textCalc = this.Meta && this.Meta.FormatData && (this.Meta.FormatData.includes(".") || this.Meta.FormatData.includes("return")) ? Utils.IsFunction(this.Meta.FormatData || '', false, this) : "";
        if (textCalc) {
            cellText = textCalc;
            if (this.Meta.ComponentType == "Input") {
                this.Element.textContent = this.getTextContent(cellText);
            }
            else {
                this.Element.innerHTML = cellText;
            }
            this.Element.title = cellText;
        }
        else {
            this.CalcCellText(cellData);
            this.Element.title = "";
        }
        if (!this.Dirty) {
            this.OriginalText = this.getTextContent(cellText);
            this.DOMContentLoaded?.Invoke();
            this.OldValue = this.getTextContent(cellText);
        }
    }

    GetValueText() {
        return this.Element.textContent;
    }
}