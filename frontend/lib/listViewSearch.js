import {
    EventType, HttpMethod, LogicOperation, OperatorEnum
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
import { SearchMethodEnum, Where } from './models/enum.js';
import { Checkbox } from './checkbox.js';
import { Select } from "./select.js";
import { Spinner } from './spinner.js';
/**
 * @typedef {import('./models/component.js').Component} Component
 * @typedef {import('./listView.js').ListView} ListView
 * @typedef {import('./gridView.js').GridView} GridView
 * @typedef {import('./tabEditor.js').TabEditor} TabEditor
 * @typedef {import('./datepicker.js').Datepicker} Datepicker
 * @typedef {import('./searchEntry.js').SearchEntry} SearchEntry
 */

/**
 * @class
 */
// @ts-ignore
export class ListViewSearchVM {
    constructor() {
        this.Id = Uuid7.Id25();
        this.SearchTerm = '';
        this.FullTextSearch = '';
        this.ScanTerm = '';
        this.StartDate = null;
        this.DateTimeField = '';
        this.EndDate = null;
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
    ParentGridView;
    /**
     * @type {HTMLInputElement}
     * @private
     */
    _uploader;

    /**
     * @type {HTMLInputElement}
     * @private
     */
    _fullTextSearch;

    /**
     * @type {ListViewSearchVM}
     */
    get EntityVM() {
        return this.Entity;
    }

    /**
     * @type {string}
     */
    get DateTimeField() {
        return this._dateTimeField;
    }

    /**
     * @param {string} value
     */
    set DateTimeField(value) {
        this._dateTimeField = value;
    }

    /**
     * @type {Component[]}
     */
    BasicSearch;

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
        this.PopulateDirty = false;
        this.AlwaysValid = true;
        this.Meta = ui;
        this.DateTimeField = ui.DateTimeField ?? 'InsertedDate';
        this.Entity = new ListViewSearchVM();
        this.Disabled = false;
    }

    Render() {
        if (!this.Meta.CanSearch) {
            var coms = this.EditForm.Meta.ComponentOptions && this.EditForm.Meta.ComponentOptions.filter(x => x.ComponentId == this.Meta.Id);
            if (coms && coms.length > 0) {
                Html.Take(this.Parent.Element.firstChild.firstChild).TabIndex(-1).Event(EventType.KeyPress, this.EnterSearch.bind(this));
                this.Element = Html.Context;
                Html.Take(this.Element).Div.ClassName('searching-block')
                    .Button.ClassName("btn btn-light btn-sm mr-1").Event(EventType.Click, (e) => {
                        this.ExcelOptions(e, 1);
                    }).Icon('fal fa-file-excel mr-1').End.End
                    .Button.ClassName("btn btn-light btn-sm").Event(EventType.Click, (e) => {
                        this.ExcelOptions(e, 2);
                    }).Icon('fal fal fa-print mr-1').End.End.Render();
            }
            return;
        }
        // @ts-ignore
        Html.Take(this.Parent.Element.firstChild.firstChild).TabIndex(-1).Event(EventType.KeyPress, this.EnterSearch.bind(this));
        this.Element = Html.Context;
        this.RenderImportBtn();
        Html.Take(this.Element).Div.Render();
        var txtSearch = new Textbox({
            FieldName: 'SearchTerm',
            Label: 'Search',
            PlainText: 'Search',
            ShowLabel: false,
        });
        txtSearch.ParentElement = Html.Context;
        txtSearch.UserInput = null;
        this.AddChild(txtSearch);
        Html.End.Render();
        var startDate = new Datepicker({
            FieldName: 'StartDate',
            Label: 'From date',
            PlainText: 'From date',
            ShowLabel: false,
        });
        startDate.ParentElement = this.Element;
        startDate.UserInput = null;
        this.AddChild(startDate);
        var endDate = new Datepicker({
            FieldName: 'EndDate',
            Label: 'To date',
            PlainText: 'To date',
            ShowLabel: false,
        });
        endDate.ParentElement = this.Element;
        endDate.UserInput = null;
        this.AddChild(endDate);
        if (this.Parent.Meta.ShowDatetimeField) {
            // @ts-ignore
            var dateType = new SearchEntry({
                FieldName: 'DateTimeField',
                PlainText: 'DateTime field',
                FormatData: '{ShortDesc}',
                ShowLabel: false,
                RefName: 'Component',
            });
            dateType.ParentElement = this.Element;
            dateType.UserInput = null;
            this.AddChild(dateType);
        }
        Html.Take(this.Element).Div.ClassName('searching-block')
            .Button.ClassName("btn btn-light btn-sm mr-1").Event(EventType.Click, () => {
                this.Parent.ClearSelected();
                this.Parent.ReloadData().then();
            }).Icon('fal fa-search')
            .End.End
            .Button.ClassName("btn btn-light btn-sm mr-1").Event(EventType.Click, this.RefreshListView.bind(this)).Icon('fal fa-undo').End.End
            .Render();
        var coms = this.EditForm.Meta.ComponentOptions && this.EditForm.Meta.ComponentOptions.filter(x => x.ComponentId == this.Meta.Id);
        if (coms && coms.length > 0) {
            Html.Button.ClassName("btn btn-light btn-sm mr-1").Event(EventType.Click, (e) => {
                this.ExcelOptions(e, 1);
            }).Icon('fal fa-file-excel mr-1').End.End
                .Button.ClassName("btn btn-light btn-sm").Event(EventType.Click, (e) => {
                    this.ExcelOptions(e, 2);
                }).Icon('fal fal fa-print mr-1').End.End.Render();
        }
    }

    RefreshListView() {
        this.EntityVM.SearchTerm = '';
        this.EntityVM.StartDate = null;
        this.EntityVM.EndDate = null;
        this.UpdateView();

        if (!(this.Parent)) {
            return;
        }
        const listView = this.Parent;
        listView.ClearSelected();
        listView.CellSelected = [];
        listView.AdvSearchVM.Conditions = [];
        listView.AdvSearchVM.AdvSearchConditions = [];
        listView.Wheres = [];
        let newVM = { ...this.EntityVM };
        Object.keys(newVM).forEach(key => {
            newVM[key] = null;
        });
        this.Entity = newVM;
        this.Parent.SearchSection.Children.forEach(x => x.IsOrderBy = false);
        this.Parent.SearchSection.Children.forEach(txtSearch => {
            txtSearch.Entity = this.Entity;
            switch (txtSearch.Meta.ComponentType) {
                case "Dropdown":
                case "Input":
                    txtSearch.SearchIcon = "fal fa-search";
                    txtSearch.SearchMethod = SearchMethodEnum.Contain;
                    txtSearch.OrderMethod = "asc";
                    txtSearch.IsOrderBy = false;
                    txtSearch.Entity = this.ListViewSearch.EntityVM;
                    break;
                case "Datepicker":
                    txtSearch.SearchMethod = SearchMethodEnum.Range;
                    txtSearch.OrderMethod = "asc";
                    txtSearch.SearchIcon = "fal fa-arrows-alt-h";
                    txtSearch.Entity = this.ListViewSearch.EntityVM;
                    break;
                case "Checkbox":
                    txtSearch.SearchMethod = SearchMethodEnum.Contain;
                    break;
                default:
                    txtSearch.SearchMethod = SearchMethodEnum.Contain;
                    break;
            }
            if (txtSearch.SearchIconElement && txtSearch.SearchIcon) {
                txtSearch.SearchIconElement.className = txtSearch.SearchIcon;
            }
            txtSearch.UpdateView();
        });
        listView.ApplyFilter();
    }

    FilterListView() {
        var json = JSON.parse(this.Parent.Meta.Query);
        if (json.search) {
            /**
             * @type {any[]}
             * 
             */
            var filterComs = json.search;
            var coms = filterComs.map(x => {
                return {
                    ComponentType: 'Input',
                    Label: x.Label,
                    FieldName: x.FieldName,
                    Query: x.Where
                }
            });
            this.EditForm.OpenConfig("Advanced filter", () => {
                coms.forEach(item => {
                    const existingConditionIndex = this.Parent.AdvSearchVM.AdvSearchConditions.findIndex(
                        condition => condition.FieldName === item.FieldName
                    );

                    if (existingConditionIndex > -1) {
                        this.Parent.AdvSearchVM.AdvSearchConditions[existingConditionIndex] = {
                            ...this.Parent.AdvSearchVM.AdvSearchConditions[existingConditionIndex],
                            Where: item.Query,
                            Value: this.EditForm.Entity[item.FieldName]
                        };
                    } else {
                        this.Parent.AdvSearchVM.AdvSearchConditions.push({
                            FieldName: item.FieldName,
                            Where: item.Query,
                            Value: this.EditForm.Entity[item.FieldName]
                        });
                    }
                });
                this.Parent.ApplyFilter();
            }, () => { }, true, coms);
        }
    }

    FullScreen() {
        var elem = this.Parent.Element;
        if (elem.requestFullscreen) {
            elem.requestFullscreen();
        }
    }

    /**
     * @param {Event} e
     */
    EnterSearch(e) {
        if (e.KeyCode() !== 13) {
            return;
        }

        this.Parent.ApplyFilter().Done();
    }

    /**
     * @param {Event} e
     */
    UploadCsv(e) {
        /** @type {File[]} */
        var files = e.target['files'];
        if (!files || files.length === 0) {
            return;
        }

        /** @type {HTMLFormElement} */
        // @ts-ignore
        var uploadForm = this._uploader.parentElement;
        var formData = new FormData(uploadForm);
        var meta = this.Parent.Meta;
        // @ts-ignore
        Client.Instance.SubmitAsync({
            FormData: formData,
            Url: `/user/importCsv?table=${meta.RefName}&comId=${meta.Id}&connKey=${meta.MetaConn}`,
            Method: HttpMethod.POST,
            ResponseMimeType: Utils.GetMimeType('csv')
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
    ExcelOptions(e, type) {
        /** @type {HTMLElement} */
        const ele = e.target;
        var buttonRect = ele.getBoundingClientRect();
        var ctxMenu = ContextMenu.Instance;
        ctxMenu.Top = buttonRect.bottom;
        ctxMenu.Left = buttonRect.left;
        ctxMenu.EditForm = this.EditForm;
        var coms = this.EditForm.Meta.ComponentOptions.filter(x => x.ComponentId == this.Meta.Id && x.TypeId == type);
        if (coms) {
            ctxMenu.MenuItems = coms.map(x => ({
                Icon: 'fa fa-download mr-1',
                Text: x.Title || 'Dowload',
                Click: this.DispatchClickAsync.bind(this, x)
            }));
        }
        ctxMenu.Render();
    }
    MetaData;
    DispatchClickAsync(meta) {
        this.MetaData = meta;
        Spinner.AppendTo();
        this.LoadData(meta).then(response => {
            Spinner.Hide();
            if (Utils.IsPath(response)) {
                const pdfUrl = response;
                const a = document.createElement('a');
                a.style.display = 'none';
                a.href = pdfUrl;
                a.download = this.Meta.PlainText || 'output.xlsx';
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
            }
            else {
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
                var width = "794px";
                switch (this.Meta.ReportTypeId) {
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
                Html.Instance.Iframe.ClassName("container-rpt").Style("margin:auto;background:#fff;overflow: auto;min-height:calc(-13rem + 100vh);").Width(width);
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
                if (this.IFrameElement.onload) {
                    this.IFrameElement.onload = () => {
                        this.IFrameElement.contentWindow.document.head.appendChild(link);
                        this.IFrameElement.contentWindow.document.head.appendChild(css);
                        const iframeDoc = this.IFrameElement.contentWindow.document;
                        iframeDoc.body.innerHTML = response;
                    };
                }
                else {
                    this.IFrameElement.contentWindow.document.head.appendChild(link);
                    this.IFrameElement.contentWindow.document.head.appendChild(css);
                    this.IFrameElement.contentWindow.document.body.innerHTML = response;
                }
            }

        });
    }

    /**
    @type {HTMLIFrameElement}
    */
    IFrameElement
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
        Client.Instance.PostAsync({ Html: this.IFrameElement.contentWindow.document.documentElement.outerHTML, FileName: this.MetaData.FileName }, "/api/GenPdf").then(response => {
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
        com1.FieldName = "PdfPlanEmailId";
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
        com2.FieldName = "PdfPartnerId";
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
        }, () => { }, true, [com2, com1, { FieldName: "PdfToEmail", Label: "Send To", ComponentType: "Input", Column: 6 }, { FieldName: "PdfToName", Label: "To Name", ComponentType: "Input", Column: 6 }, { FieldName: "PdfSubjectMail", Label: "Subject", ComponentType: "Input", Column: 12 }, { FieldName: "PdfTemplate", Label: "Template", ComponentType: "Word", Precision: 400 }], null, null, "824px");
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
        Spinner.AppendTo();
        Client.Instance.PostAsync({ Html: this.IFrameElement.contentWindow.document.documentElement.outerHTML, FileName: this.Entity.FormatChat || this.Entity.Code || this.Entity.Id }, "/api/GenPdf").then(async (response2) => {
            Spinner.Hide();
            const removePath = Client.RemoveGuid(response2);
            const fileUrl = response2;
            const fileName = removePath;
            const subject = this.EditForm.Entity.PdfSubjectMail || '';
            const htmlBody = this.EditForm.Entity.PdfTemplate || '';
            const toEmail = this.EditForm.Entity.PdfToEmail || '';
            const toName = this.EditForm.Entity.PdfToName || this.EditForm.Entity.PdfPartnerIdText;
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
                    .a4 { display: flex; justify-content: center; width:206mm;}
                    .header, .footer { width: 100%; background: white; text-align: center; }
                    .header { top: 0; left: 0; }
                    .footer { position: fixed; bottom: 0; left: 0; }
                </style>
            </head>
            <body>${htmlBody}</body>
            </html>`;
            if (!htmlBody) {
                styledHtml = this.IFrameElement.contentWindow.document.documentElement.outerHTML;
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
            const reader = new FileReader();
            reader.onloadend = () => {
                const base64 = reader.result.split(',')[1];
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    }

    LoadData(meta) {
        let submitEntity = Utils.IsFunction(this.Meta.PreQuery, true, this);
        var params = submitEntity ? JSON.stringify(submitEntity) : null;
        let promise = new Promise((resolve, reject) => {
            Client.Instance.PostAsync({ ComId: this.Meta.Id, PathTemplate: meta.TypeId == 1 ? meta.ExcelUrl : meta.Template, FileName: meta.FileName, Params: params, Report: true }, meta.TypeId == 1 ? "/api/CreateExcel" : "/api/CreateHtml").then(res => {
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
    AdvancedOptions(e) {
        /** @type {HTMLElement} */
        // @ts-ignore
        const ele = e.target;
        var buttonRect = ele.getBoundingClientRect();
        var show = localStorage.getItem(`Show${this.Meta.Id}`) ?? false;
        var ctxMenu = ContextMenu.Instance;
        ctxMenu.Top = buttonRect.bottom;
        ctxMenu.Left = buttonRect.left;
        if (this.Meta.CanExport) {
            ctxMenu.MenuItems = [
                { Icon: 'fa fa-download mr-1', Text: 'Export excel', Click: this.ExportAllData.bind(this) },
            ];
        }
        ctxMenu.Render();
    }

    RenderImportBtn() {
        Html.Take(this.Element).Form.Attr('method', 'POST').Attr('enctype', 'multipart/form-data')
            .Display(false).Input.Type('file').Id(`id_${Uuid7.Id25()}`).Attr('name', 'files').Attr('accept', '.csv');
        // @ts-ignore
        this._uploader = Html.Context;
        this._uploader.addEventListener(EventType.Change, (/** @type {Event} */ ev) => this.UploadCsv(ev));
    }

    /**
     * @param {object} arg
     */
    FilterSelected(arg) {
        var selectedIds = this.Parent.SelectedIds;
        if (!selectedIds || selectedIds.length === 0) {
            Toast.Warning('Select rows to filter');
            return;
        }
        if (this.Parent.CellSelected.some(x => x.FieldName === this.IdField)) {
            this.Parent.CellSelected.find(x => x.FieldName === this.IdField).Value = selectedIds.join();
            this.Parent.CellSelected.find(x => x.FieldName === this.IdField).ValueText = selectedIds.join();
        } else {
            // @ts-ignore
            this.Parent.CellSelected.push({
                FieldName: this.IdField,
                FieldText: 'Mã',
                ComponentType: 'Input',
                Value: selectedIds.join(),
                ValueText: selectedIds.join(),
                Operator: OperatorEnum.In,
                OperatorText: 'Chứa',
                Logic: LogicOperation.And,
            });
            this.ParentGridView._summarys.push(new HTMLElement());
        }
        this.Parent.ActionFilter();
    }

    /**
     * @param {object} arg
     */
    ExportCustomData(arg) {
        this.TabEditor?.OpenPopup('Export CustomData', () => this.Exporter()).Done();
    }

    /**
     * @typedef {import('./exportCustomData.js').ExportCustomData} ExportCustomData
     * @returns {Promise<ExportCustomData>}
     */
    async Exporter() {
        const { ExportCustomData } = await import('./exportCustomData.js');
        if (!this._export) {
            this._export = new ExportCustomData(this.Parent);
            this._export.ParentElement = this.TabEditor?.Element;
            this._export.Disposed.add(() => this._export = null);
        }
        return this._export;
    }

    /**
     * @param {object} arg
     */
    async ExportAllData(arg) {
        const exporter = await this.Exporter();
        exporter.Export();
    }

    /**
     * @param {object} arg
     */
    async ExportSelectedData(arg) {
        if (!this.Parent.SelectedIds || this.Parent.SelectedIds.length === 0) {
            Toast.Warning('Select at least 1 one to export excel');
            return;
        }
        const exporter = await this.Exporter();
        exporter.Export(this.Parent.SelectedIds);
    }

    /**
     * @param {object} arg
     */
    OpenExcelFileDialog(arg) {
        this._uploader.click();
    }

    /**
     * Calculates the filter query based on the search terms and date range.
     * @returns {string} The final filter query.
     */
    CalcFilterQuery() {
        if (this.EntityVM.DateTimeField) {
            this.DateTimeField = this.Parent.Header.find(x => x.Id === this.EntityVM.DateTimeField).FieldName;
        }
        var headers = this.Parent.Header.filter(x => ["Dropdown", "Textarea", "Input", "Datepicker", "Checkbox"].includes(x.ComponentType));
        const searchTerm = this.EntityVM.SearchTerm ? this.EntityVM.SearchTerm.trim() : '';
        var operators = headers.map(x => {
            /**
             * @type {Textbox}
             */
            var mapCom = this.Parent.SearchSection.Children.find(y => y.Meta.FieldName == x.FieldName);
            var textFilter = ComponentExt.MapToFilterOperator(x, searchTerm, mapCom);
            var val = null;
            var operator = " OR ";
            if (mapCom && !Utils.isNullOrWhiteSpace(mapCom.GetValueText() ? mapCom.GetValueText().trim() : '')) {
                if (mapCom instanceof Datepicker) {
                    const fromDate = new Date(mapCom.Entity[mapCom.Meta.FieldName]);
                    fromDate.setHours(0, 0, 0, 0);
                    const toDate = new Date(mapCom.Entity[mapCom.Meta.FieldName + "To"]);
                    toDate.setHours(23, 59, 59, 999);
                    textFilter = `(ds.[${mapCom.Meta.FieldName}] >= '${this.dayjs(fromDate).format("YYYY-MM-DD HH:mm")}' and ds.[${(mapCom.Meta.FieldName)}] <= '${this.dayjs(toDate).format("YYYY-MM-DD HH:mm")}')`;
                }
                else if (mapCom instanceof Select) {
                    textFilter = ComponentExt.MapToFilterOperator(x, mapCom.GetValue() || "", mapCom);
                    val = mapCom.GetValue();
                }
                else {
                    textFilter = ComponentExt.MapToFilterOperator(x, (mapCom.GetValueText() ? mapCom.GetValueText().trim() : ''), mapCom);
                    val = mapCom.GetValue();
                }
                operator = " AND ";
                return {
                    Where: textFilter,
                    Value: val,
                    FieldName: `@${x.FieldName.toLocaleLowerCase()}search`,
                    Operator: operator
                };
            }
            else {
                if (mapCom instanceof Datepicker) {
                    textFilter = null;
                }
                else if (mapCom instanceof Select) {
                    textFilter = null;
                }
            }
            return {
                Where: textFilter,
                Value: searchTerm,
                FieldName: `@${x.FieldName.toLocaleLowerCase()}search`,
                Operator: operator
            };

        }).filter(x => !Utils.isNullOrWhiteSpace(x.Where));
        if (this.EntityVM.StartDate) {
            const fromDate = new Date(this.EntityVM.StartDate);
            fromDate.setHours(0, 0, 0, 0);
            operators.push({ Where: `ds.[${this.DateTimeField}] >= '${fromDate}'` });
        }
        if (this.EntityVM.EndDate) {
            const toDate = new Date(mapCom.Entity[mapCom.Meta.FieldName + "To"]);
            toDate.setHours(23, 59, 59, 999);
            operators.push({ Where: `ds.[${this.DateTimeField}] <= '${toDate}'` });
        }
        return operators;
    }

    /**
     * Gets or sets whether the component is disabled.
     * Always returns false indicating that it cannot be disabled.
     */
    get Disabled() {
        return false;
    }

    set Disabled(value) {
        // Components are never disabled, ignore the input.
    }

    AdvancedSearch(arg) {
        ComponentExt.OpenPopup(this.TabEditor, "AdvancedSearch", () => {
            // @ts-ignore
            var editor = new AdvancedSearch(this.ParentListView);
            editor.Parent = this.Parent,
                editor.ParentElement = this.TabEditor.Element
            return editor;
        }).Done();
    }
}

