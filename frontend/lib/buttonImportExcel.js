import { Spinner } from "./spinner.js";
import { EditableComponent } from "./editableComponent.js";
import { Component } from "./models/component.js";
import { html } from "./utils/html.js";
import { Client } from "./clients/index.js";
import { Toast } from "./toast.js";

/**
 * represents a button component that can be rendered and managed on a web page.
 */
export class ButtonImportExcel extends EditableComponent {
    isButton = true;
    /**
     * create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui);
        /** @type {Component} */
        this.meta = ui;
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

        html.span.className("caption").iText(this.meta.Label || "", this.editForm.meta.Label);
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
            this.importExcelTemplate().then(() => {
                this.disabled = false;
            });
        } finally {
            window.setTimeout(() => {
                this.disabled = false;
            }, 2000);
        }
    }

    /**
     * @param {event} e
     */
    async importExcelTemplate(e) {
        const fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.accept = '.xlsx, .xls';
        fileInput.addEventListener('change', (event) => {
            if (event.target.files.length > 0) {
                this.actImportExcelTemplate(event);
            }
        });
        fileInput.click();
    }

    /**
    * @param {event} e
    */
    async actImportExcelTemplate(e) {
        const file = e.target.files[0];
        if (!file) {
            alert("no file selected.");
            return;
        }
        Spinner.appendTo();
        try {
            var rs = await Client.instance.postFilesAsync(file, this.meta.formatData);
            Spinner.hide();
            if (this.isBlob(rs)) {
                const ext = this.inferExtByType(rs.type);
                const fileName =
                    (this.meta && this.meta.fileName ? this.meta.fileName : "download") +
                    (ext || "");
                this.downloadBlob(rs, fileName);
            }
            else {
                var grid = this.editForm.childCom.find(c => c.meta.componentType === "GridView");
                Toast.success("excel file imported successfully.", 5000);
                await grid.actionFilter();
            }
        } catch (error) {
            Spinner.hide();
            this.editForm.openConfig(error.detail, () => {
            }, () => { }, false, [], true)
        }

    }

    downloadBlob(blob, fileName) {
        const url = uRL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName || "download";
        document.body.appendChild(a);
        a.click();
        a.remove();
        uRL.revokeObjectURL(url);
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
