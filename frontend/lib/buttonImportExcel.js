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
            Html.take(this.ParentElement).button.render();
            this.Element = this.ButtonEle = Html.Context;
        } else {
            this.Element = this.ButtonEle;
        }

        Html.take(this.Element)
            .className(this.Meta.ClassName)
            .event("click", () => this.DispatchClick())
            .style(this.Meta.Style);

        if (this.Meta.Icon) {
            Html.icon(this.Meta.Icon).end.text(" ").render();
        }

        Html.span.className("caption").iText(this.Meta.Label || "", this.EditForm.Meta.Label);
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
        try {
            var rs = await Client.instance.postFilesAsync(file, this.Meta.FormatData);
            Spinner.Hide();
            if (this.isBlob(rs)) {
                const ext = this.inferExtByType(rs.type);
                const fileName =
                    (this.Meta && this.Meta.FileName ? this.Meta.FileName : "download") +
                    (ext || "");
                this.downloadBlob(rs, fileName);
            }
            else {
                var grid = this.EditForm.ChildCom.find(c => c.Meta.ComponentType === "GridView");
                Toast.Success("Excel file imported successfully.", 5000);
                await grid.ActionFilter();
            }
        } catch (error) {
            Spinner.Hide();
            this.EditForm.OpenConfig(error.detail, () => {
            }, () => { }, false, [], true)
        }

    }

    downloadBlob(blob, fileName) {
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName || "download";
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
    }

    isBlob(x) {
        return x && typeof x === "object" && typeof x.arrayBuffer === "function" && typeof x.type === "string";
    }

    inferExtByType(type) {
        if (/spreadsheetml/i.test(type)) return ".xlsx";
        if (/pdf/i.test(type)) return ".pdf";
        if (/msword/i.test(type)) return ".doc";
        if (/wordprocessingml/i.test(type)) return ".docx";
        if (/zip/i.test(type)) return ".zip";
        if (/json/i.test(type)) return ".json";
        return ""; // fallback
    }
}
