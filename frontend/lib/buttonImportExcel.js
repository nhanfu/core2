import { Spinner } from "./spinner.js";
import { EditableComponent } from "./editableComponent.js";
import { Component } from "./models/component.js";
import { Html } from "./utils/html.js";
import { Client } from "./clients/index.js";
import { Toast } from "./toast.js";

/**
 * Represents a button component that can be rendered and managed on a web page.
 */
export class ButtonImportExcel extends EditableComponent {
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
            if (!this.ParentElement) throw new Error("ParentElement is required");
            Html.Take(this.ParentElement).Button.Render();
            this.Element = this.ButtonEle = Html.Context;
        } else {
            this.Element = this.ButtonEle;
        }

        Html.Take(this.Element)
            .ClassName(this.Meta.ClassName)
            .Event("click", () => this.DispatchClick())
            .Style(this.Meta.Style);

        if (this.Meta.Icon) {
            Html.Icon(this.Meta.Icon).End.Text(" ").Render();
        }

        Html.Span.ClassName("caption").IText(this.Meta.Label || "", this.EditForm.Meta.Label);
        this._textEle = Html.Context;

        this.Element.closest("td")?.addEventListener("keydown", e => this.ListViewItemTab(e));
        this.DOMContentLoaded?.invoke();
    }

    /**
     * Dispatches the click event, handles UI changes for click action.
     */
    DispatchClick() {
        if (this.Meta.OnClick) {
            this.Meta.OnClick.call();
            return;
        }

        if (this.Disabled || this.Element.hidden) {
            return;
        }
        this.Disabled = true;
        try {
            this.ImportExcelTemplate().then(() => {
                this.Disabled = false;
            });
        } finally {
            window.setTimeout(() => {
                this.Disabled = false;
            }, 2000);
        }
    }

    /**
     * @param {Event} e
     */
    async ImportExcelTemplate(e) {
        const fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.accept = '.xlsx, .xls';
        fileInput.addEventListener('change', (event) => {
            if (event.target.files.length > 0) {
                this.ActImportExcelTemplate(event);
            }
        });
        fileInput.click();
    }

    /**
    * @param {Event} e
    */
    async ActImportExcelTemplate(e) {
        const file = e.target.files[0];
        if (!file) {
            alert("No file selected.");
            return;
        }
        Spinner.AppendTo();
        await Client.Instance.PostFilesAsync(file, this.Meta.FormatData);
        Spinner.Hide();
        var grid = this.EditForm.ChildCom.find(c => c.Meta.ComponentType === "GridView");
        Toast.Success("Excel file imported successfully.", 5000);
        await grid.ActionFilter();
    }
}
