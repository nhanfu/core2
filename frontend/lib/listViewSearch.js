import {
    EventType, httpMethod, logicOperation, operatorEnum
} from './models/';
import { Section } from './section.js';
import { EditableComponent } from './editableComponent.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { Client } from "./clients/client.js";
import { Uuid7 } from './structs/uuidv7.js';
import { AdvancedSearch } from './advancedSearch.js';
import { Toast } from './toast.js';
import { ContextMenu } from './contextMenu.js';
import { ComponentExt } from './utils/componentExt.js';
import { Textbox } from './textbox.js';
import { Datepicker } from './datepicker.js';
import { SearchEntry } from './searchEntry.js';
import { searchMethodEnum, Where } from './models/enum.js';
import { Checkbox } from './checkbox.js';
import { Select } from "./select.js";
import { Spinner } from './spinner.js';
/**
 * @typedef {import('./models/component.js').Component} Component
 * @typedef {import('./listView.js').listView} ListView
 * @typedef {import('./gridView.js').gridView} GridView
 * @typedef {import('./tabEditor.js').tabEditor} TabEditor
 * @typedef {import('./datepicker.js').Datepicker} Datepicker
 * @typedef {import('./searchEntry.js').searchEntry} SearchEntry
 */

/**
 * @class
 */
// @ts-ignore
export class ListViewSearchVM {
    constructor() {
        this.Id = Uuid7.Id25();
        this.searchTerm = '';
        this.fullTextSearch = '';
        this.scanTerm = '';
        this.startDate = null;
        this.dateTimeField = '';
        this.endDate = null;
    }
}

/**
 * @class
 * @extends EditableComponent
 */
export class ListViewSearch extends EditableComponent {
    /** @type {ListView} */
    // @ts-ignore
    Parent;

    /** @type {GridView} */
    parentGridView;
    /**
     * @type {hTMLInputElement}
     * @private
     */
    _uploader;

    /**
     * @type {hTMLInputElement}
     * @private
     */
    _fullTextSearch;

    /**
     * @type {ListViewSearchVM}
     */
    get entityVM() {
        return this.entity;
    }

    /**
     * @type {string}
     */
    get dateTimeField() {
        return this._dateTimeField;
    }

    /**
     * @param {string} value
     */
    set dateTimeField(value) {
        this._dateTimeField = value;
    }

    /**
     * @type {Component[]}
     */
    basicSearch;

    /**
     * @type {boolean}
     * @private
     */
    _hasRender = false;

    /**
     * @param {Component} ui
     */
    constructor(ui) {
        super(ui, null);
        this.populateDirty = false;
        this.alwaysValid = true;
        this.meta = ui;
        this.dateTimeField = ui.dateTimeField ?? 'insertedDate';
        this.entity = new ListViewSearchVM();
        this.disabled = false;
    }

    render() {
        if (!this.meta.canSearch) {
            var coms = this.editForm.meta.componentOptions && this.editForm.meta.componentOptions.filter(x => x.componentId == this.meta.Id && x.typeId == 1);
            if (coms && coms.length > 0) {
                Html.take(this.parent.element.firstChild.firstChild).tabIndex(-1).event(EventType.keyPress, this.enterSearch.bind(this));
                this.element = Html.context;
                Html.take(this.element).div.className('searching-block');
                Html.button.className("btn btn-light btn-sm mr-1").event(EventType.Click, (e) => {
                    this.excelOptions(e, coms);
                }).icon('fal fa-file-excel mr-1').end.end.render();
            }
            var coms2 = this.editForm.meta.componentOptions && this.editForm.meta.componentOptions.filter(x => x.componentId == this.meta.Id && x.typeId == 2);
            if (coms2 && coms2.length > 0) {
                Html.button.className("btn btn-light btn-sm").event(EventType.Click, (e) => {
                    this.excelOptions(e, coms2);
                }).icon('fal fal fa-print mr-1').end.end.render();
            }
            return;
        }
        // @ts-ignore
        Html.take(this.parent.element.firstChild.firstChild).tabIndex(-1).event(EventType.keyPress, this.enterSearch.bind(this));
        this.element = Html.context;
        this.renderImportBtn();
        Html.take(this.element).div.render();
        Html.take(this.element).div.className('searching-block')
            .button.className("btn btn-light btn-sm mr-1").event(EventType.Click, () => {
                this.parent.clearSelected();
                this.parent.reloadData().then();
            }).icon('fal fa-search')
            .end.end
            .button.className("btn btn-light btn-sm mr-1").event(EventType.Click, this.refreshListView.bind(this)).icon('fal fa-undo').end.end
            .button.className("btn btn-light btn-sm mr-1").event(EventType.Click, this.exportExcel.bind(this)).icon('fal fa-file-excel').end.end
            .render();
        var coms = this.editForm.meta.componentOptions && this.editForm.meta.componentOptions.filter(x => x.componentId == this.meta.Id && x.typeId == 1);
        if (coms && coms.length > 0) {
            Html.button.className("btn btn-light btn-sm mr-1").event(EventType.Click, (e) => {
                this.excelOptions(e, coms2);
            }).icon('fal fa-file-excel mr-1').end.end.render();
        }
        var coms2 = this.editForm.meta.componentOptions && this.editForm.meta.componentOptions.filter(x => x.componentId == this.meta.Id && x.typeId == 2);
        if (coms2 && coms2.length > 0) {
            Html.button.className("btn btn-light btn-sm").event(EventType.Click, (e) => {
                this.excelOptions(e, coms2);
            }).icon('fal fal fa-print mr-1').end.end.render();
        }
    }

    refreshListView() {
        this.entityVM.searchTerm = '';
        this.entityVM.startDate = null;
        this.entityVM.endDate = null;
        this.updateView();

        if (!(this.parent)) {
            return;
        }
        const listView = this.parent;
        listView.clearSelected();
        listView.cellSelected = [];
        listView.advSearchVM.Conditions = [];
        listView.advSearchVM.advSearchConditions = [];
        listView.Wheres = [];
        let newVM = { ...this.entityVM };
        Object.keys(newVM).forEach(key => {
            newVM[key] = null;
        });
        this.entity = newVM;
        this.parent.searchSection.Children.forEach(x => x.isOrderBy = false);
        this.parent.searchSection.Children.forEach(txtSearch => {
            txtSearch.Entity = this.entity;
            txtSearch.multipleData = null;
            txtSearch.updateView();
        });
        listView.applyFilter();
    }


    exportExcel() {
        const listView = this.parent;
        listView.excelData(false, 0, 100, true).then();
    }

    filterListView() {
        var json = JSON.parse(this.parent.meta.Query);
        if (json.search) {
            /**
             * @type {any[]}
             * 
             */
            var filterComs = json.search;
            var coms = filterComs.map(x => {
                return {
                    componentType: 'Input',
                    Label: x.Label,
                    fieldName: x.fieldName,
                    Query: x.Where
                }
            });
            this.editForm.openConfig("Advanced filter", () => {
                coms.forEach(item => {
                    const existingConditionIndex = this.parent.advSearchVM.advSearchConditions.findIndex(
                        condition => condition.fieldName === item.fieldName
                    );

                    if (existingConditionIndex > -1) {
                        this.parent.advSearchVM.advSearchConditions[existingConditionIndex] = {
                            ...this.parent.advSearchVM.advSearchConditions[existingConditionIndex],
                            Where: item.Query,
                            Value: this.editForm.entity[item.fieldName]
                        };
                    } else {
                        this.parent.advSearchVM.advSearchConditions.push({
                            fieldName: item.fieldName,
                            Where: item.Query,
                            Value: this.editForm.entity[item.fieldName]
                        });
                    }
                });
                this.parent.applyFilter();
            }, () => { }, true, coms);
        }
    }

    fullScreen() {
        var elem = this.parent.element;
        if (elem.requestFullscreen) {
            elem.requestFullscreen();
        }
    }

    /**
     * @param {Event} e
     */
    enterSearch(e) {
        if (e.keyCode() !== 13) {
            return;
        }

        this.parent.applyFilter().Done();
    }

    /**
     * @param {Event} e
     */
    uploadCsv(e) {
        /** @type {File[]} */
        var files = e.target['files'];
        if (!files || files.length === 0) {
            return;
        }

        /** @type {hTMLFormElement} */
        // @ts-ignore
        var uploadForm = this._uploader.parentElement;
        var formData = new FormData(uploadForm);
        var meta = this.parent.meta;
        // @ts-ignore
        Client.instance.submitAsync({
            formData: formData,
            Url: `/user/importCsv?table=${meta.refName}&comId=${meta.Id}&connKey=${meta.metaConn}`,
            Method: httpMethod.POST,
            responseMimeType: Utils.getMimeType('csv')
        }).Done(() => {
            Toast.Success('Import excel success');
            this._uploader.value = '';
        }).catch(error => {
            Toast.Warning(error.Message);
            this._uploader.value = '';
        });
    }

    /**
     * @param {Event} e
     */
    excelOptions(e, coms) {
        /** @type {HTMLElement} */
        const ele = e.target;
        var buttonRect = ele.getBoundingClientRect();
        var ctxMenu = ContextMenu.Instance;
        ctxMenu.Top = buttonRect.bottom;
        ctxMenu.Left = buttonRect.left;
        ctxMenu.editForm = this.editForm;
        ctxMenu.menuItems = coms.map(x => ({
            Icon: 'fa fa-download mr-1',
            Text: x.Title || 'Dowload',
            Click: this.dispatchClickAsync.bind(this, x)
        }));
        ctxMenu.render();
    }
    metaData;
    dispatchClickAsync(meta) {
        this.metaData = meta;
        Spinner.appendTo();
        this.loadData(meta).then(response => {
            Spinner.Hide();
            if (Utils.isPath(response)) {
                const pdfUrl = response;
                const a = document.createElement('a');
                a.style.display = 'none';
                a.href = pdfUrl;
                a.download = this.meta.plainText || 'output.xlsx';
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
            }
            else {
                const handlerClose = this.closePreview.bind(this);
                const handlerPrint = this.printPdf.bind(this);
                const handlerPdf = this.exportPdf.bind(this);
                const handlerSendMail = this.sendMail.bind(this);
                Html.take(this.tabEditor?.element ?? document.body).div.className("backdrop").style("align-items: center;");
                this.Preview = Html.context;
                Html.instance.div.escape(handlerClose).className("popup-content");
                this.popupContent = Html.context;
                Html.instance.div.className("popup-title").span.iText(this.meta.plainText || "Report PDF", this.editForm.meta.label);
                this.titleElement = Html.context;
                Html.instance.end.div.className("title-center");
                this.titleCenterElement = Html.context;
                Html.instance.end.div.className("icon-box d-flex").style("display: flex; gap: 20px; align-items: center;")
                    .span.className("fal fa-at").event("click", handlerSendMail).end
                    .span.className("fal fa-file-pdf").event("click", handlerPdf).end
                    .span.className("fal fa-print").event("click", handlerPrint).end
                    .span.className("fa fa-times").event("click", handlerClose).end.end.end.div.className("popup-body scroll-content").style("padding-bottom: 1rem;max-height:calc(100vh - 10rem) !important;display: flex; align-items: center;background-color:#525659");
                var width = "794px";
                switch (this.meta.reportTypeId) {
                    case 1: // A4 Portrait
                        width = "794px";
                        break;
                    case 2: // A4 Landscape
                        width = "1123px";
                        break;
                    case 3: // A5 Portrait
                        width = "559px";
                        break;
                    case 4: // A5 Landscape
                        width = "794px";
                        break;
                    default:
                        width = "794px"; // fallback
                }
                Html.instance.iFrame.className("container-rpt").style("margin:auto;background:#fff;overflow: auto;min-height:calc(-13rem + 100vh);").width(width);
                this.iFrameElement = Html.context;
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
                                    }
                                    .header { top: 0; left: 0; }
                                    .footer { position: fixed;bottom: 0; left: 0; }`;
                var link = document.createElement('link');
                link.rel = "stylesheet";
                link.href = "https://fonts.googleapis.com/css2?family=Montserrat:wght@400;700&display=swap";
                if (this.iFrameElement.onload) {
                    this.iFrameElement.onload = () => {
                        this.iFrameElement.contentWindow.document.head.appendChild(link);
                        this.iFrameElement.contentWindow.document.head.appendChild(css);
                        const iframeDoc = this.iFrameElement.contentWindow.document;
                        iframeDoc.body.innerHTML = response;
                    };
                }
                else {
                    this.iFrameElement.contentWindow.document.head.appendChild(link);
                    this.iFrameElement.contentWindow.document.head.appendChild(css);
                    this.iFrameElement.contentWindow.document.body.innerHTML = response;
                }
            }

        });
    }

    /**
    @type {hTMLIFrameElement}
    */
    iFrameElement
    /**
     * Closes the preview.
     */
    closePreview() {
        this.Preview.remove();
    }

    printPdf() {
        this.iFrameElement.contentWindow.print();
    }

    exportPdf() {
        Spinner.appendTo();
        Client.instance.postAsync({ Html: this.iFrameElement.contentWindow.document.documentElement.outerHTML, fileName: this.metaData.fileName }, "/api/genPdf").then(response => {
            Spinner.Hide();
            Client.download(response);
        });
    }

    async sendMail() {
        var planEmail = await Client.instance.getService("Get planEmail");
        var partner = await Client.instance.getService("Get Partner");
        var com1 = planEmail[0][0];
        com1.componentType = "Dropdown";
        com1.showLabel = true;
        com1.fieldName = "pdfPlanEmailId";
        com1.Label = "Template mail";
        com1.Template = `[
                {
                    "fieldName": "Name",
                    "Label": "Name",
                    "componentType": "Input"
                }
            ]`;
        com1.Column = 6;
        com1.Events = `{"change":"updateEmailTemplate"}`;
        var com2 = partner[0][0];
        com2.componentType = "Dropdown";
        com2.showLabel = true;
        com2.Column = 6;
        com2.Label = "Partner";
        com2.fieldName = "pdfPartnerId";
        com2.Events = `{"change":"updateEmailTo"}`;
        com2.Template = `[
                    {
                        "fieldName": "Name",
                        "Label": "Name",
                        "componentType": "Input",
                        "maxWidth": "300px",
                        "minWidth": "300px",
                        "Width": "300px"
                    },
                    {
                        "fieldName": "taxCode",
                        "Label": "taxCode",
                        "componentType": "Input"
                    },
                    {
                        "fieldName": "Email",
                        "Label": "Email",
                        "componentType": "Input"
                    }
                ]`;
        this.editForm.openConfig("Choose mail template!", async () => {
            await this.createEMLFromFileUrl();
        }, () => { }, true, [com2, com1, { fieldName: "pdfToEmail", Label: "Send To", componentType: "Input", Column: 6 }, { fieldName: "pdfToName", Label: "To Name", componentType: "Input", Column: 6 }, { fieldName: "pdfSubjectMail", Label: "Subject", componentType: "Input", Column: 12 }, { fieldName: "pdfTemplate", Label: "Template", componentType: "Word", Precision: 400 }], null, null, "824px");
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
        Client.instance.postAsync({ Html: this.iFrameElement.contentWindow.document.documentElement.outerHTML, fileName: this.entity.formatChat || this.entity.Code || this.entity.Id }, "/api/genPdf").then(async (response2) => {
            Spinner.Hide();
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
                    .a4 { display: flex; justify-content: center; width:206mm;}
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
                }
                else {
                    const eml = `To: ${toName} <${toEmail}>
    Subject: ${subject}
    X-Unsent: 1
    Content-Type: multipart/mixed; boundary=--boundary_text_string
    
    ----boundary_text_string
    Content-Type: text/html; charset=UTF-8
    
    ${htmlWithInline}
    
    ----boundary_text_string--`;

                    const emlBlob = new Blob([eml], { type: "message/rfc822" });
                    const a = document.createElement("a");
                    a.href = URL.createObjectURL(emlBlob);
                    a.download = removePath.replaceAll("pdf", "eml");
                    a.click();
                }

            } catch (err) {
                console.error("Lỗi khi tạo EML:", err);
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

    loadData(meta) {
        let submitEntity = Utils.isFunction(this.meta.preQuery, true, this);
        var params = submitEntity ? JSON.stringify(submitEntity) : null;
        let promise = new Promise((resolve, reject) => {
            Client.instance.postAsync({ comId: this.meta.Id, pathTemplate: meta.typeId == 1 ? meta.excelUrl : meta.Template, fileName: meta.fileName, params: params, Report: true }, meta.typeId == 1 ? "/api/createExcel" : "/api/createHtml").then(res => {
                resolve(res);
            }).catch(e => {
                Spinner.Hide();
                Toast.Warning(e.Message);
            });
        });
        return promise;
    }

    /**
     * @param {Event} e
     */
    advancedOptions(e) {
        /** @type {HTMLElement} */
        // @ts-ignore
        const ele = e.target;
        var buttonRect = ele.getBoundingClientRect();
        var show = localStorage.getItem(`Show${this.meta.Id}`) ?? false;
        var ctxMenu = ContextMenu.Instance;
        ctxMenu.Top = buttonRect.bottom;
        ctxMenu.Left = buttonRect.left;
        if (this.meta.canExport) {
            ctxMenu.menuItems = [
                { Icon: 'fa fa-download mr-1', Text: 'Export excel', Click: this.exportAllData.bind(this) },
            ];
        }
        ctxMenu.render();
    }

    renderImportBtn() {
        Html.take(this.element).form.attr('method', 'POST').attr('enctype', 'multipart/form-data')
            .display(false).input.type('file').id(`id_${Uuid7.Id25()}`).attr('name', 'files').attr('accept', '.csv');
        // @ts-ignore
        this._uploader = Html.context;
        this._uploader.addEventListener(EventType.Change, (/** @type {Event} */ ev) => this.uploadCsv(ev));
    }

    /**
     * @param {object} arg
     */
    filterSelected(arg) {
        var selectedIds = this.parent.selectedIds;
        if (!selectedIds || selectedIds.length === 0) {
            Toast.Warning('Select rows to filter');
            return;
        }
        if (this.parent.cellSelected.some(x => x.fieldName === this.idField)) {
            this.parent.cellSelected.find(x => x.fieldName === this.idField).Value = selectedIds.join();
            this.parent.cellSelected.find(x => x.fieldName === this.idField).valueText = selectedIds.join();
        } else {
            // @ts-ignore
            this.parent.cellSelected.push({
                fieldName: this.idField,
                fieldText: 'Mã',
                componentType: 'Input',
                Value: selectedIds.join(),
                valueText: selectedIds.join(),
                Operator: operatorEnum.In,
                operatorText: 'Chứa',
                Logic: logicOperation.And,
            });
            this.parentGridView._summarys.push(new HTMLElement());
        }
        this.parent.actionFilter();
    }

    /**
     * @param {object} arg
     */
    ExportCustomData(arg) {
        this.tabEditor?.openPopup('Export customData', () => this.Exporter()).Done();
    }

    /**
     * @typedef {import('./exportCustomData.js').exportCustomData} ExportCustomData
     * @returns {Promise<ExportCustomData>}
     */
    async Exporter() {
        const { ExportCustomData } = await import('./exportCustomData.js');
        if (!this._export) {
            this._export = new ExportCustomData(this.parent);
            this._export.parentElement = this.tabEditor?.element;
            this._export.Disposed.add(() => this._export = null);
        }
        return this._export;
    }

    /**
     * @param {object} arg
     */
    async exportAllData(arg) {
        const exporter = await this.Exporter();
        exporter.Export();
    }

    /**
     * @param {object} arg
     */
    async exportSelectedData(arg) {
        if (!this.parent.selectedIds || this.parent.selectedIds.length === 0) {
            Toast.Warning('Select at least 1 one to export excel');
            return;
        }
        const exporter = await this.Exporter();
        exporter.Export(this.parent.selectedIds);
    }

    /**
     * @param {object} arg
     */
    openExcelFileDialog(arg) {
        this._uploader.click();
    }

    /**
     * Calculates the filter query based on the search terms and date range.
     * @returns {string} The final filter query.
     */
    calcFilterQuery() {
        if (this.entityVM.dateTimeField) {
            this.dateTimeField = this.parent.Header.find(x => x.Id === this.entityVM.dateTimeField).fieldName;
        }
        var headers = this.parent.Header.filter(x => ["Dropdown", "Textarea", "Input", "Datepicker", "Checkbox", "Number"].includes(x.componentType));
        const searchTerm = this.entityVM.searchTerm ? this.entityVM.searchTerm.trim() : '';
        var operators = headers.map(x => {
            /**
             * @type {Textbox}
             */
            var mapCom = this.parent.searchSection.Children.find(y => y.Meta && y.Meta.Id && y.Meta.Id == x.Id);
            var textFilter = ComponentExt.mapToFilterOperator(x, searchTerm, mapCom);
            var val = null;
            var operator = " OR ";
            if (this.parent.componentType != "Dropdown") {
                operator = " AND ";
            }
            if (mapCom && !Utils.isNullOrWhiteSpace(mapCom.getValueText() ? mapCom.getValueText().trim() : '')) {
                if (mapCom instanceof Datepicker) {
                    if (mapCom.searchMethod == searchMethodEnum.filled) {
                        textFilter = mapCom.meta.searchFieldName ? `${(mapCom.meta.searchFieldName)} is not null` : `ds.[${(mapCom.meta.fieldName)}] is not null`;
                    }
                    else if (mapCom.searchMethod == searchMethodEnum.empty) {
                        textFilter = mapCom.meta.searchFieldName ? `${(mapCom.meta.searchFieldName)} is not null` : `ds.[${(mapCom.meta.fieldName)}] is null`;
                    }
                    else {
                        var fromDate = new Date(mapCom.entity[mapCom.meta.fieldName]);
                        fromDate.setHours(0, 0, 0, 0);
                        var toDate = new Date(mapCom.entity[mapCom.meta.fieldName + "To"]);
                        if (!mapCom.entity[mapCom.meta.fieldName + "To"]) {
                            toDate = new Date(mapCom.entity[mapCom.meta.fieldName]);
                            toDate.setHours(23, 59, 59, 999);
                        }
                        textFilter =
                            textFilter = mapCom.meta.searchFieldName
                                ? `(${(mapCom.meta.searchFieldName)} >= '${this.dayjs(fromDate).format("YYYY-MM-DD HH:mm")}' and ${((mapCom.meta.searchFieldName))} <= '${this.dayjs(toDate).format("YYYY-MM-DD HH:mm")}')`
                                :
                                `(ds.[${(mapCom.meta.fieldName)}] >= '${this.dayjs(fromDate).format("YYYY-MM-DD HH:mm")}' and ds.[${((mapCom.meta.fieldName))}] <= '${this.dayjs(toDate).format("YYYY-MM-DD HH:mm")}')`;
                    }
                }
                else if (mapCom instanceof Select) {
                    textFilter = ComponentExt.mapToFilterOperator(x, mapCom.getValue() || "", mapCom);
                    val = mapCom.getValue();
                }
                else {
                    textFilter = ComponentExt.mapToFilterOperator(x, (mapCom.getValueText() ? mapCom.getValueText().trim() : ''), mapCom);
                    val = mapCom.getValue();
                }
                operator = " AND ";
                return {
                    Where: textFilter,
                    Value: val,
                    fieldName: x.searchFieldName ? `@${x.searchFieldName.replaceAll(".", "").toLocaleLowerCase()}search` : `@${x.fieldName.toLocaleLowerCase()}search`,
                    Operator: operator
                };
            }
            else {
                if (mapCom && mapCom.multipleData) {
                    const esc = s => s.replace(/'/g, "''");
                    const values = (mapCom.multipleData.toString())
                        .split(/\r?\n/)
                        .map(s => s.trim())
                        .filter(Boolean);

                    if (values.length === 0) {
                    } else if (x.componentType !== "Dropdown") {
                        const inList = values.map(v => `N'${esc(v)}'`).join(", ");
                        textFilter = x.searchFieldName ? `${x.fieldName} IN (${inList})` : `ds.[${x.fieldName}] IN (${inList})`;
                    } else {
                        const refName = (x.refName || '').trim();
                        if (refName) {
                            const cols = ComponentExt.extractStrings(x.formatData) || [];
                            const inList = values.map(v => `N'${esc(v)}'`).join(", ");
                            const matchCols = (cols.length > 0 ? cols : ["Name"]).map(c => `ds2.[${c}] IN (${inList})`);
                            const fieldName = x.searchFieldName ? `${x.searchFieldName}` : `ds.[${x.fieldName}]`;
                            textFilter = `EXISTS (SELECT 1 FROM [${refName}] ds2 WHERE ds2.Id = ${fieldName} AND (${matchCols.join(" OR ")}))`;
                        }
                    }
                }
                else {
                    if (mapCom && mapCom.searchMethod == searchMethodEnum.filled) {
                        textFilter = x.searchFieldName ? `${x.fieldName} is not null` : `ds.[${x.fieldName}] is not null`;
                    }
                    else if (mapCom && mapCom.searchMethod == searchMethodEnum.empty) {
                        textFilter = x.searchFieldName ? `${x.fieldName} is null` : `ds.[${x.fieldName}] is null`;
                    }
                }

            }
            return {
                Where: textFilter,
                Value: searchTerm,
                fieldName: x.searchFieldName ? `@${x.searchFieldName.replaceAll(".", "").toLocaleLowerCase()}search` : `@${x.fieldName.toLocaleLowerCase()}search`,
                Operator: operator
            };

        }).filter(x => !Utils.isNullOrWhiteSpace(x.Where));
        if (this.entityVM.startDate) {
            const fromDate = new Date(this.entityVM.startDate);
            fromDate.setHours(0, 0, 0, 0);
            operators.push({ Where: `ds.[${this.dateTimeField}] >= '${fromDate}'` });
        }
        if (this.entityVM.endDate) {
            const toDate = new Date(mapCom.Entity[mapCom.Meta.fieldName + "To"]);
            toDate.setHours(23, 59, 59, 999);
            operators.push({ Where: `ds.[${this.dateTimeField}] <= '${toDate}'` });
        }
        return operators;
    }

    /**
     * Gets or sets whether the component is disabled.
     * Always returns false indicating that it cannot be disabled.
     */
    get disabled() {
        return false;
    }

    set disabled(value) {
        // Components are never disabled, ignore the input.
    }

    AdvancedSearch(arg) {
        ComponentExt.openPopup(this.tabEditor, "AdvancedSearch", () => {
            // @ts-ignore
            var editor = new AdvancedSearch(this.parentListView);
            editor.Parent = this.parent,
                editor.parentElement = this.tabEditor.element
            return editor;
        }).Done();
    }
}

