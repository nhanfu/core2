import { GridView } from "./gridView.js";
import { Button } from "./button";
import { PdfReport } from "./pdfReport";
import { html } from "./utils/html.js";
import { Component } from "./models/";
import { Client } from "./clients/index.js";
import { Spinner } from "./spinner.js";
import { Toast } from "./toast.js";

export class ButtonPdf extends Button {
    /**
     * create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui, ele);
        this.preview = null;
        this.pdfReport = null;
        this.typeId = "a4";
        this.landscape = false;
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
        const handlerClose = this.closePreview.bind(this);
        const handlerPrint = this.printPdf.bind(this);
        const handlerCopyLink = this.copyLink.bind(this);
        const handlerPdf = this.exportPdf.bind(this);
        const handlerSendMail = this.sendMail.bind(this);
        html.take(this.tabEditor?.element ?? document.body).div.className("backdrop").style("align-items: center;");
        this.preview = html.context;
        html.instance.div.escape(handlerClose).className("popup-content");
        this.popupContent = html.context;
        html.instance.div.className("popup-title").span.iText(this.meta.plainText || "report pDF", this.editForm.meta.Label);
        this.titleElement = html.context;
        html.instance.end.div.className("title-center");
        this.titleCenterElement = html.context;
        html.instance.end.div.className("icon-box d-flex").style("display: flex; gap: 20px; align-items: center;")
            .span.className("fal fa-at").event("click", handlerSendMail).end
            .span.className("fal fa-copy").event("click", handlerCopyLink).end
            .span.className("fal fa-file-pdf").event("click", handlerPdf).end
            .span.className("fal fa-print").event("click", handlerPrint).end
            .span.className("fa fa-times").event("click", handlerClose).end.end.end.div.className("popup-body scroll-content").style("padding-bottom: 1rem;max-height:calc(100vh - 10rem) !important;display: flex; align-items: center;background-color:#525659");
        var width = "794px";
        switch (this.meta.reportTypeId) {
            case 1: // a4 portrait
                width = "794px";
                break;
            case 2: // a4 landscape
                width = "1123px";
                this.landscape = true;
                break;
            case 3: // a5 portrait
                width = "559px";
                this.typeId = "a5";
                break;
            case 4: // a5 landscape
                width = "794px";
                this.landscape = true;
                this.typeId = "a5";
                break;
            default:
                width = "794px"; // fallback
        }
        html.instance.iFrame.className("container-rpt").style("margin:auto;background:#fff;overflow: auto;min-height:calc(-13rem + 100vh);").width(width);
        this.iFrameElement = html.context;
        var css = document.createElement('style');
        css.textContent = `body {
                                    font-family: 'montserrat';
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
                                    
                                td,
                                td>span,
                                td>p,
                                td>div,
                                td>strong {
                                    padding-left: 2px;
                                    vertical-align: top;
                                    white-space: pre-wrap;
                                    word-break: break-word;
                                }

                                .logo {
                                    width: 100%;
                                    height: 100%;
                                }

                                .dashed tbody tr:not(:last-child) td {
                                    border-bottom: 0.01px dashed rgb(126, 140, 141) !important;
                                }
                                    
                                .a4 {
                                    display: flex;
                                    justify-content: center;
                                    width:206mm;
                                }
                                .header, .footer {
                                    width: 100%;
                                    background: white;
                                    text-align: center;
                                }`;
        var link = document.createElement('link');
        link.rel = "stylesheet";
        link.href = "https://fonts.googleapis.com/css2?family=montserrat:wght@400;700&display=swap";
        this.pdfReport = new PdfReport(this.meta);
        if (this.iFrameElement.onload) {
            this.iFrameElement.onload = () => {
                this.iFrameElement.contentWindow.document.head.appendChild(link);
                this.iFrameElement.contentWindow.document.head.appendChild(css);
                const iframeDoc = this.iFrameElement.contentWindow.document;
                this.pdfReport.parentElement = iframeDoc.body;
                if (this.parent) {
                    this.parent.addChild(this.pdfReport);
                } else {
                    this.addChild(this.pdfReport);
                }
            };
        }
        else {
            this.iFrameElement.contentWindow.document.head.appendChild(link);
            this.iFrameElement.contentWindow.document.head.appendChild(css);
            this.pdfReport = new PdfReport(this.meta);
            this.pdfReport.parentElement = this.iFrameElement.contentWindow.document.body;
            if (this.parent) {
                this.parent.addChild(this.pdfReport);
            } else {
                this.addChild(this.pdfReport);
            }
        }
    }
    /**
     * closes the preview.
     */
    closePreview() {
        this.preview.remove();
    }

    printPdf() {
        if (!this.meta.showHotKey) {
            this.iFrameElement.contentWindow.print();
            return;
        }
        Spinner.appendTo();
        Client.instance.postAsync(
            {
                html: this.iFrameElement.contentWindow.document.documentElement.outerHTML,
                fileName: this.entity.formatChat || this.entity.code || this.entity.id,
                type: this.typeId,
                landscape: this.landscape
            },
            "/api/genPdf"
        ).then(async (res) => {
            Spinner.hide();

            let url = (typeof res === "string") ? res
                : (res?.url || res?.data || res?.path || res?.path);

            if (!url) throw new error("Không tìm thấy uRL pDF trong phản hồi.");

            if (!/^https?:|^blob:|^data:/i.test(url)) {
                url = location.origin + (url.startsWith("/") ? "" : "/") + url;
            }
            const u = new uRL(url, location.origin);
            const isSameOrigin = u.origin === location.origin || url.startsWith("blob:") || url.startsWith("data:");

            try {
                if (isSameOrigin) {
                    printViaHiddenIframe(url);
                } else {
                    // Thử fetch cORS để tạo blob (tránh cross-origin print bị chặn)
                    const resp = await fetch(url, { mode: "cors", credentials: "omit" });
                    if (!resp.ok) throw new error(`fetch pDF thất bại: ${resp.status}`);
                    const blob = await resp.blob();
                    const blobUrl = uRL.createObjectURL(blob);
                    printViaHiddenIframe(blobUrl, /*revoke*/ true);
                }
            } catch (err) {
                console.error("Không thể in trực tiếp (cORS hoặc policy):", err);
            }
        }).catch(err => {
            Spinner.hide();
            console.error(err);
        });

        function printViaHiddenIframe(src, revokeAfter = false) {
            const iframe = document.createElement("iframe");
            iframe.style.position = "fixed";
            iframe.style.width = "0";
            iframe.style.height = "0";
            iframe.style.border = "0";
            iframe.src = src;

            const cleanup = () => {
                if (revokeAfter && src.startsWith("blob:")) uRL.revokeObjectURL(src);
                iframe.remove();
                window.removeEventListener("afterprint", cleanup);
            };

            iframe.onload = () => {
                try {
                    // 1 số trình duyệt cần delay nhỏ để render pDF viewer
                    setTimeout(() => {
                        iframe.contentWindow?.focus();
                        iframe.contentWindow?.print();
                    }, 50);
                } catch (e) {
                    console.error("Gọi print() bị chặn:", e);
                } finally {
                    window.addEventListener("afterprint", cleanup);
                    setTimeout(cleanup, 1000 * 60 * 5);
                }
            };

            document.body.appendChild(iframe);
        }
    }

    copyLink() {
        Spinner.appendTo();
        Client.instance.postAsync({
            html: this.iFrameElement.contentWindow.document.documentElement.outerHTML,
            fileName: this.entity.formatChat || this.entity.code || this.entity.id,
            type: this.typeId,
            landscape: this.landscape
        }, "/api/genPdf").then(async response => {
            Spinner.hide();
            try {
                await navigator.clipboard.writeText(response);
                Toast.success("link copied to clipboard!")
            } catch (e) {
                Toast.warning('copy failed — please copy manually: ' + response);
            }
        });
    }

    exportPdf() {
        Spinner.appendTo();
        Client.instance.postAsync({
            html: this.iFrameElement.contentWindow.document.documentElement.outerHTML,
            fileName: this.entity.formatChat || this.entity.code || this.entity.id,
            type: this.typeId,
            landscape: this.landscape
        }, "/api/genPdf").then(response => {
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
        }, () => { }, true, [com2, com1, { fieldName: "pdfToEmail", Label: "send to", componentType: "input", column: 6 }, { fieldName: "pdfToName", Label: "to name", componentType: "input", column: 6 }, { fieldName: "pdfSubjectMail", Label: "subject", componentType: "input", column: 12 }, { fieldName: "pdfTemplate", Label: "template", componentType: "word", precision: 400 }], null, null, "824px");
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
        Client.instance.postAsync({
            html: this.iFrameElement.contentWindow.document.documentElement.outerHTML,
            fileName: this.entity.formatChat || this.entity.code || this.entity.id,
            type: this.typeId,
            landscape: this.landscape
        }, "/api/genPdf").then(async (response2) => {
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
                    word-break: break-word;
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
                    const base64Content = await this.blobToBase64(blob);
                    const base64Split = base64Content.match(/.{1,76}/g).join("\r\n");
                    const eml = `to: ${toName} <${toEmail}>
subject: ${subject}
x-unsent: 1
content-type: multipart/mixed; boundary=--boundary_text_string

----boundary_text_string
content-type: text/html; charset=uTF-8

${htmlWithInline}

----boundary_text_string
content-type: application/octet-stream; name=${fileName}
content-transfer-encoding: base64
content-disposition: attachment

${base64Split}

----boundary_text_string--`;

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
