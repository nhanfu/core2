import { GridView } from "./gridView.js";
import { Button } from "./button";
import { PdfReport } from "./pdfReport";
import { Html } from "./utils/html.js";
import { Component } from "./models/";
import { Client } from "./clients/index.js";
import { Spinner } from "./spinner.js";

export class ButtonPdf extends Button {
    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui, ele);
        this.Preview = null;
        this.PdfReport = null;
    }

    /**
     * Dispatches the click event.
     */
    // @ts-ignore
    DispatchClick() {
        if (this.Meta.Precision == 7) {
            this.DispatchEvent(this.Meta.Events, "click", this, this.Entity).then(() => {
                this.Disabled = false;
                Spinner.Hide();
            });
        }
        else {
            setTimeout(() => this.DispatchClickAsync(), 0);
        }
    }
    /**
    @type {HTMLIFrameElement}
    */
    IFrameElement
    /**
     * Asynchronously handles the click dispatch.
     */
    async DispatchClickAsync() {
        const handlerClose = this.ClosePreview.bind(this);
        const handlerPrint = this.PrintPdf.bind(this);
        const handlerPdf = this.ExportPdf.bind(this);
        const handlerSendMail = this.SendMail.bind(this);
        Html.Take(this.TabEditor?.Element ?? document.body).Div.ClassName("backdrop").Style("align-items: center;");
        this.Preview = Html.Context;
        Html.Instance.Div.Escape(handlerClose).ClassName("popup-content");
        this.PopupContent = Html.Context;
        Html.Instance.Div.ClassName("popup-title").Span.IText(this.Meta.PlainText || "Report PDF", this.EditForm.Meta.Label);
        this.TitleElement = Html.Context;
        Html.Instance.End.Div.ClassName("title-center");
        this.TitleCenterElement = Html.Context;
        Html.Instance.End.Div.ClassName("icon-box d-flex").Style("display: flex; gap: 20px; align-items: center;")
            .Span.ClassName("fal fa-at").Event("click", handlerSendMail).End
            .Span.ClassName("fal fa-file-pdf").Event("click", handlerPdf).End
            .Span.ClassName("fal fa-print").Event("click", handlerPrint).End
            .Span.ClassName("fa fa-times").Event("click", handlerClose).End.End.End.Div.ClassName("popup-body scroll-content").Style("padding-bottom: 1rem;max-height:calc(100vh - 10rem) !important;display: flex; align-items: center;background-color:#525659");
        Html.Instance.Iframe.ClassName("container-rpt").Style("margin:auto;background:#fff;overflow: auto;min-height:calc(20rem + 100vh);").Width((this.Meta.ReportTypeId == 2 || this.Meta.ReportTypeId == 4) ? "1123px" : "796px");
        this.IFrameElement = Html.Context;
        var css = document.createElement('style');
        css.textContent = `body {
                                    font-family: 'Montserrat';
                                    font-size: 10pt;
                                }

                                * {
                                    margin: 0;
                                    padding: 0;
                                    box-sizing: border-box;
                                }

                                table {
                                    font-size: unset;
                                }

                                table > tr > td {
                                    vertical-align: top;
                                }

                                td>span,
                                td>p,
                                td>div,
                                td>strong {
                                    padding-left: 2px;
                                    vertical-align: top;
                                    white-space: pre-wrap;
                                }

                                .logo {
                                    width: 100%;
                                    height: 100%;
                                }

                                .dashed tbody td {
                                    border-bottom: 0.01px dashed rgb(126, 140, 141) !important;
                                }
                                    
                                .a4 {
                                    display: flex;
                                    justify-content: center;
                                    width:210mm;
                                }
                                .header, .footer {
                                    width: 100%;
                                    background: white;
                                    text-align: center;
                                }
                                .header { top: 0; left: 0; }
                                .footer { position: fixed;bottom: 0; left: 0; }`;
        var link = document.createElement('link');
        link.rel = "stylesheet";
        link.href = "https://fonts.googleapis.com/css2?family=Montserrat:wght@400;700&display=swap";
        this.IFrameElement.contentWindow.document.head.appendChild(link);
        this.IFrameElement.contentWindow.document.head.appendChild(css);
        this.PdfReport = new PdfReport(this.Meta);
        this.PdfReport.ParentElement = this.IFrameElement.contentWindow.document.body;
        if (this.Parent) {
            this.Parent.AddChild(this.PdfReport);
        } else {
            this.AddChild(this.PdfReport);
        }
    }
    /**
     * Closes the preview.
     */
    ClosePreview() {
        this.Preview.remove();
    }

    PrintPdf() {
        this.IFrameElement.contentWindow.print();
    }

    ExportPdf() {
        Spinner.AppendTo();
        Client.Instance.PostAsync({ Html: this.IFrameElement.contentWindow.document.documentElement.outerHTML, FileName: this.Entity.FormatChat || this.Entity.Code || this.Entity.Id }, "/api/GenPdf").then(response => {
            Spinner.Hide();
            Client.Download(response);
        });
    }

    async SendMail() {
        var planEmail = await Client.Instance.GetService("Get PlanEmail");
        var partner = await Client.Instance.GetService("Get Partner");
        var com1 = planEmail[0][0];
        com1.ComponentType = "Dropdown";
        com1.ShowLabel = true;
        com1.FieldName = "ButtonPdfPlanEmailId";
        com1.Label = "Template mail";
        com1.Template = `[
            {
                "FieldName": "Name",
                "Label": "Name",
                "ComponentType": "Input"
            }
        ]`;
        com1.Column = 6;
        com1.Events = `{"change":"UpdateEmailTemplate"}`;
        var com2 = partner[0][0];
        com2.ComponentType = "Dropdown";
        com2.ShowLabel = true;
        com2.Column = 6;
        com2.Label = "Partner";
        com2.FieldName = "ButtonPdfPartnerId";
        com2.Events = `{"change":"UpdateEmailTo"}`;
        com2.Template = `[
                {
                    "FieldName": "Name",
                    "Label": "Name",
                    "ComponentType": "Input",
                    "MaxWidth": "300px",
                    "MinWidth": "300px",
                    "Width": "300px"
                },
                {
                    "FieldName": "TaxCode",
                    "Label": "TaxCode",
                    "ComponentType": "Input"
                },
                {
                    "FieldName": "Email",
                    "Label": "Email",
                    "ComponentType": "Input"
                }
            ]`;
        this.EditForm.OpenConfig("Choose mail template!", async () => {
            await this.createEMLFromFileUrl();
        }, () => { }, true, [com1, com2, { FieldName: "ButtonPdfToEmail", Label: "Send To", ComponentType: "Input", Column: 6 }, { FieldName: "ButtonPdfToName", Label: "To Name", ComponentType: "Input", Column: 6 }, { FieldName: "ButtonPdfSubjectMail", Label: "Subject", ComponentType: "Input", Column: 12 }, { FieldName: "ButtonPdfTemplate", Label: "Template", ComponentType: "Word", Precision: 400 }], null, null, "824px");
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

                const resultHtml = iframeDoc.body.outerHTML;
                document.body.removeChild(iframe);
                resolve(resultHtml);
            };

            // Gán srcdoc để trigger iframe.onload
            iframe.srcdoc = html;
        });
    }

    async createEMLFromFileUrl() {
        Spinner.AppendTo();
        Client.Instance.PostAsync({ Html: this.IFrameElement.contentWindow.document.documentElement.outerHTML, FileName: this.Entity.FormatChat || this.Entity.Code || this.Entity.Id }, "/api/GenPdf").then(async (response2) => {
            Spinner.Hide();
            const removePath = Client.RemoveGuid(response2);
            const fileUrl = response2;
            const fileName = removePath;
            const subject = this.EditForm.Entity.ButtonPdfSubjectMail || '';
            const htmlBody = this.EditForm.Entity.ButtonPdfTemplate || '';
            const toEmail = this.EditForm.Entity.ButtonPdfToEmail || '';
            const toName = this.EditForm.Entity.ButtonPdfToName || this.EditForm.Entity.ButtonPdfPartnerIdText;
            const styledHtml = `
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
                .dashed tbody td { border-bottom: 0.01px dashed rgb(126, 140, 141) !important; }
                .a4 { display: flex; justify-content: center; width:210mm; }
                .header, .footer { width: 100%; background: white; text-align: center; }
                .header { top: 0; left: 0; }
                .footer { position: fixed; bottom: 0; left: 0; }
            </style>
        </head>
        <body>${htmlBody}</body>
        </html>`;
            const htmlWithInline = await this.inlineAllStyles(styledHtml);
            try {
                const response = await fetch(fileUrl);
                const blob = await response.blob();
                const base64Content = await this.blobToBase64(blob);
                const base64Split = base64Content.match(/.{1,76}/g).join("\r\n");
                const eml = `To: ${toName} <${toEmail}>
Subject: ${subject}
X-Unsent: 1
Content-Type: multipart/mixed; boundary=--boundary_text_string

----boundary_text_string
Content-Type: text/html; charset=UTF-8

${htmlWithInline}

----boundary_text_string
Content-Type: application/octet-stream; name=${fileName}
Content-Transfer-Encoding: base64
Content-Disposition: attachment

${base64Split}

----boundary_text_string--`;

                const emlBlob = new Blob([eml], { type: "message/rfc822" });
                const a = document.createElement("a");
                a.href = URL.createObjectURL(emlBlob);
                a.download = removePath.replaceAll("pdf", "eml");
                a.click();
            } catch (err) {
                console.error("Lỗi khi tạo EML:", err);
            }
        });
    }

    blobToBase64(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = () => {
                const base64 = reader.result.split(',')[1];
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    }
}
