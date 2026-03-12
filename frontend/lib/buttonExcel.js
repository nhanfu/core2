import { GridView } from "./gridView.js";
import { Button } from "./button.js";
import { PdfReport } from "./pdfReport.js";
import { html } from "./utils/html.js";
import { Component } from "./models/index.js";
import { Client } from "./clients/index.js";
import { Spinner } from "./spinner.js";

export class ButtonExcel extends Button {
    /**
     * create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui, ele);
        this.preview = null;
        this.pdfReport = null;
    }

    /**
     * dispatches the click event.
     */
    // @ts-ignore
    dispatchClick() {
        setTimeout(() => this.dispatchClickAsync(), 0);
    }
    /**
     * asynchronously handles the click dispatch.
     */
    async dispatchClickAsync() {
        Spinner.appendTo();
        var response = await this.loadData();
        Spinner.hide();
        const pdfUrl = response;
        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = pdfUrl;
        a.download = this.meta.plainText || 'output.xlsx';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }

    loadData() {
        let promise = new Promise((resolve, reject) => {
            Client.instance.postAsync({ comId: this.meta.id, data: this.entity }, "/api/createExcel").then(res => {
                resolve(res);
            }).catch(e => {
                Spinner.hide();
                Toast.warning(e.Message);
            });
        });
        return promise;
    }
}
