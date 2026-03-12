import { Button } from "./button";
import { Component } from "./models/";
import { Client } from "./clients/index.js";
import { Spinner } from "./spinner.js";

export class ButtonEmail extends Button {
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
    dispatchClick() {
        if (this.meta.precision == 7) {
            this.dispatchEvent(this.meta.events, "click", this, this.entity).then(() => {
                this.disabled = false;
                Spinner.hide();
            });
        }
        else {
            setTimeout(() => this.dispatchClickAsync(), 0);
        }
    }
    /**
    @type {hTMLIFrameElement}
    */
    iFrameElement
    /**
     * asynchronously handles the click dispatch.
     */
    async dispatchClickAsync() {
        this.sendMail();
    }
    /**
     * closes the preview.
     */
    closePreview() {
        this.preview.remove();
    }

    printPdf() {
        this.iFrameElement.contentWindow.print();
    }

    exportPdf() {
        Spinner.appendTo();
        Client.instance.postAsync({ html: this.entity["pdfTemplate"], fileName: this.entity.formatChat || this.entity.code || this.entity.id }, "/api/genPdf").then(response => {
            Spinner.hide();
            Client.download(response);
        });
    }

    async sendMail() {
        var planEmail = await Client.instance.getService("get planEmail");
        var partner = await Client.instance.getService("get partner");
        var com1 = planEmail[0][0];
        com1.componentType = "dropdown";
        com1.showLabel = true;
        com1.fieldName = "pdfPlanEmailId";
        com1.Label = "template mail";
        com1.template = `[
            {
                "fieldName": "name",
                "Label": "name",
                "componentType": "input"
            }
        ]`;
        com1.column = 6;
        com1.events = `{"change":"updateEmailTemplate"}`;
        var com2 = partner[0][0];
        com2.componentType = "dropdown";
        com2.showLabel = true;
        com2.column = 6;
        com2.Label = "partner";
        com2.fieldName = "pdfPartnerId";
        com2.events = `{"change":"updateEmailTo"}`;
        com2.template = `[
                {
                    "fieldName": "name",
                    "Label": "name",
                    "componentType": "input",
                    "maxWidth": "300px",
                    "minWidth": "300px",
                    "width": "300px"
                },
                {
                    "fieldName": "taxCode",
                    "Label": "taxCode",
                    "componentType": "input"
                },
                {
                    "fieldName": "email",
                    "Label": "email",
                    "componentType": "input"
                }
            ]`;
        this.editForm.openConfig("choose mail template!", async () => {
            await this.createEMLFromFileUrl();
        }, () => { }, true, [com2, com1, { fieldName: "pdfToEmail", Label: "send to", componentType: "input", column: 6 }, { fieldName: "pdfToName", Label: "to name", componentType: "input", column: 6, events: `{"change":"updateEmailTemplate2"}` }, { fieldName: "pdfSubjectMail", Label: "subject", componentType: "input", column: 12 }, { fieldName: "pdfTemplate", Label: "template", componentType: "word", precision: 400 }], null, null, "824px");
    }

    inlineAllStyles(html) {
        return new Promise((resolve) => {
            const iframe = document.createElement("iframe");
            iframe.style.display = "none";
            document.body.appendChild(iframe);

            const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;

            // Gán trực tiếp vào <html> thay vì dùng .write()
            iframe.onload = () => {
                iframeDoc.documentElement.innerHTML = html;

                const styles = [];

                for (const sheet of iframeDoc.styleSheets) {
                    try {
                        for (const rule of sheet.cssRules) {
                            styles.push(rule);
                        }
                    } catch (e) {
                        console.warn("Không thể truy cập stylesheet:", e);
                    }
                }

                styles.forEach(rule => {
                    if (!rule.selectorText || !rule.style) return;

                    const elements = iframeDoc.querySelectorAll(rule.selectorText);
                    elements.forEach(el => {
                        for (const prop of rule.style) {
                            const value = rule.style.getPropertyValue(prop);
                            const priority = rule.style.getPropertyPriority(prop);
                            el.style.setProperty(prop, value, priority);
                        }
                    });
                });

                const allElements = iframeDoc.querySelectorAll('*');
                allElements.forEach(el => el.removeAttribute('class'));

                const resultHtml = iframeDoc.documentElement.outerHTML;
                document.body.removeChild(iframe);
                resolve(resultHtml);
            };

            // Gán srcdoc để trigger iframe.onload
            iframe.srcdoc = html;
        });
    }

    async createEMLFromFileUrl() {
        Spinner.appendTo();
        Client.instance.postAsync({ html: this.entity["pdfTemplate"], fileName: this.entity.formatChat || this.entity.code || this.entity.id }, "/api/genPdf").then(async (response2) => {
            Spinner.hide();
            const removePath = Client.removeGuid(response2);
            const fileUrl = response2;
            const fileName = removePath;
            const subject = this.editForm.entity.pdfSubjectMail || '';
            const htmlBody = this.editForm.entity.pdfTemplate || '';
            const toEmail = this.editForm.entity.pdfToEmail || '';
            const toName = this.editForm.entity.pdfToName || this.editForm.entity.pdfPartnerIdText;
            var styledHtml = `
        <html>
        <head>
            <meta charset="utf-8" />
            <style>
                body { font-size: 10pt; padding: 0 5px; max-width: 816px; }
                * { margin: 0; padding: 0; box-sizing: border-box; }
                table { border-collapse: collapse; }
                table > tr > td { vertical-align: top; }
                td>span, td>p, td>div, td>strong {
                    padding-left: 2px;
                    vertical-align: top;
                    white-space: pre-wrap;
                }
                .logo { width: 100%; height: 100%; }
                .dashed tbody tr:not(:last-child) td {
                                    border-bottom: 0.01px dashed rgb(126, 140, 141) !important;
                                }
                .a4 { display: flex; justify-content: center; width:206mm; }
                .header, .footer { width: 100%; background: white; text-align: center; }
                .header { top: 0; left: 0; }
                .footer { position: fixed; bottom: 0; left: 0; }
            </style>
        </head>
        <body>${htmlBody}</body>
        </html>`;
            if (!htmlBody) {
                styledHtml = this.iFrameElement.contentWindow.document.documentElement.outerHTML;
            }
            const htmlWithInline = await this.inlineAllStyles(styledHtml);
            try {
                if (htmlBody) {
                    const response = await fetch(fileUrl);
                    const blob = await response.blob();
                    const eml = `to: ${toName} <${toEmail}>
subject: ${subject}
x-unsent: 1
content-type: multipart/mixed; boundary=--boundary_text_string

----boundary_text_string
content-type: text/html; charset=uTF-8

${htmlWithInline}

----boundary_text_string`;

                    const emlBlob = new blob([eml], { type: "message/rfc822" });
                    const a = document.createElement("a");
                    a.href = uRL.createObjectURL(emlBlob);
                    a.download = removePath.replaceAll("pdf", "eml");
                    a.click();
                }
                else {
                    const eml = `to: ${toName} <${toEmail}>
subject: ${subject}
x-unsent: 1
content-type: multipart/mixed; boundary=--boundary_text_string

----boundary_text_string
content-type: text/html; charset=uTF-8

${htmlWithInline}

----boundary_text_string--`;

                    const emlBlob = new blob([eml], { type: "message/rfc822" });
                    const a = document.createElement("a");
                    a.href = uRL.createObjectURL(emlBlob);
                    a.download = removePath.replaceAll("pdf", "eml");
                    a.click();
                }

            } catch (err) {
                console.error("Lỗi khi tạo eML:", err);
            }
        });
    }

    blobToBase64(blob) {
        return new Promise((resolve, reject) => {
            const reader = new fileReader();
            reader.onloadend = () => {
                const base64 = reader.result.split(',')[1];
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    }
}
