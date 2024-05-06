
import { LogicOperation } from './models/enum.js';
import { Section } from './section.js';
import EditableComponent from './editableComponent.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { Client } from "./clients/client.js";
import EventType from './models/eventType.js';
import "./utils/fix.js";
import { Uuid7 } from './structs/uuidv7.js';
/**
 * @typedef {import('./models/component').Component} Component
 * @typedef {import('./listView').ListView} ListView
 * @typedef {import('./gridView').GridView} GridView
 * @typedef {import('./tabEditor').TabEditor} TabEditor
 * @typedef {import('./datepicker').Datepicker} Datepicker
 * @typedef {import('./searchEntry').SearchEntry} SearchEntry
 * @typedef {import('./exportCustomData').ExportCustomData} ExportCustomData
 */

/**
 * @typedef {Object} ListViewSearchVM
 * @property {string} Id
 * @property {string} SearchTerm
 * @property {string} FullTextSearch
 * @property {string} ScanTerm
 * @property {Date|null} StartDate
 * @property {string} DateTimeField
 * @property {Date|null} EndDate
 */

/**
 * @class
 */
class ListViewSearchVM {
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
    Parent;
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
     * @type {ListView}
     * @private
     */
    _parentListView;

    /**
     * @type {GridView}
     * @private
     */
    _parentGridView;

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
        this.DateTimeField = ui.DateTimeField ?? 'Component.InsertedDate';
        this.Entity = new ListViewSearchVM();
    }

    /**
     * @param {Event[][]} basicSearchHeader
     */
    ListView_DataLoaded(basicSearchHeader) {
        if (this._hasRender) return;
        this._hasRender = true;
        this.BasicSearch = this.Parent.Header
            .filter(x => x.Active && !x.Hidden)
            .sort((a, b) => b.Order - a.Order);
        if (this.BasicSearch.length === 0) {
            return;
        }
        Html.Take(this.Element);
        var components = this.BasicSearch.map(header => {
            var com = header;
            var componentType = com.ComponentType;
            com.ShowLabel = false;
            com.PlainText = header.Label;
            com.Visibility = true;
            com.Column = 1;
            var compareOpId = Components.AdvancedSearch.OperatorFactory(componentType ?? ComponentTypeTypeEnum.Textbox)[0].Id.TryParseInt();
            this.Parent.AdvSearchVM.Conditions.push({
                FieldId: com.Id,
                CompareOperatorId: compareOpId,
                LogicOperatorId: LogicOperation.And,
                Field: header,
            });
            return com;
        });
        var sectionInfo = {
            Children: components,
            Responsive: true,
            Column: components.length,
            ClassName: 'wrapper'
        };
        var _basicSearchGroup = Section.RenderSection(this, sectionInfo);
        _basicSearchGroup.Children.forEach(child => {
            child.UserInput += changes => {
                var condition = this.Parent.AdvSearchVM.Conditions.find(x => x.FieldId === child.Meta.Id);
                condition.Value = child.GetValue(true)?.toString();
            };
        });
        while (_basicSearchGroup.Element.Children.length > 0) {
            Element.InsertBefore(_basicSearchGroup.Element.FirstChild, Element.FirstChild);
        }
    }

    Render() {
        this.Parent.DataLoaded += this.ListView_DataLoaded;
        if (!this.Meta.CanSearch) {
            return;
        }
        Html.Take(Parent.Element.FirstElementChild).TabIndex(-1).Event(EventType.KeyPress, this.EnterSearch);
        this.Element = Html.Context;
        this.RenderImportBtn();
        if (this.Meta.ComponentType === 'GridView' || this.Meta.ComponentType === 'TreeView' || !this.Meta.IsRealtime) {
            var txtSearch = new Textbox(new Component({
                FieldName: 'ListViewSearchVM.SearchTerm',
                Visibility: true,
                Label: 'Tìm kiếm',
                PlainText: 'Tìm kiếm',
                ShowLabel: false,
            }));
            txtSearch.ParentElement = this.Element;
            txtSearch.UserInput = null;
            this.AddChild(txtSearch);
        }
        if (this.Meta.ComponentType !== 'ListView' && this.Meta.ComponentType !== 'TreeView') {
            var txtFullTextSearch = new Textbox(new Component({
                FieldName: 'ListViewSearchVM.FullTextSearch',
                Visibility: true,
                Label: 'Inline search',
                PlainText: 'Inline search',
                ShowLabel: false,
            }));
            txtFullTextSearch.ParentElement = this.Element;
            txtFullTextSearch.UserInput = null;
            this.AddChild(txtFullTextSearch);
            this._fullTextSearch = txtFullTextSearch.Input;
            this._fullTextSearch.AddEventListener(EventType.Input, this.ParentGridView.SearchDisplayRows);
        }

        if (this.Meta.UpperCase) {
            var txtScan = new Textbox(new Component({
                FieldName: 'ListViewSearchVM.ScanTerm',
                Visibility: true,
                Label: 'Scan',
                PlainText: 'Scan',
                ShowLabel: false,
                Focus: true,
                Events: "{'input':'ScanGridView'}"
            }));
            txtScan.ParentElement = this.Element;
            txtScan.UserInput = null;
            this.AddChild(txtScan);
        }
        var startDate = new Datepicker(new Component({
            FieldName: 'ListViewSearchVM.StartDate',
            Visibility: true,
            Label: 'From date',
            PlainText: 'From date',
            ShowLabel: false,
        }));
        startDate.ParentElement = this.Element;
        startDate.UserInput = null;
        this.AddChild(startDate);
        var endDate = new Datepicker(new Component({
            FieldName: 'ListViewSearchVM.EndDate',
            Visibility: true,
            Label: 'To date',
            PlainText: 'To date',
            ShowLabel: false,
        }));
        endDate.ParentElement = this.Element;
        endDate.UserInput = null;
        this.AddChild(endDate);
        if (this.Parent.Meta.ShowDatetimeField) {
            var dateType = new SearchEntry(new Component({
                FieldName: 'Component.DateTimeField',
                PlainText: 'Loại ngày',
                FormatData: '{ShortDesc}',
                ShowLabel: false,
                ReferenceId: Utils.GetEntity('Component').Id,
                RefName: 'Component',
            }));
            dateType.ParentElement = this.Element;
            dateType.UserInput = null;
            this.AddChild(dateType);
        }
        Html.Take(this.Element).Div.ClassName('searching-block')
            .Icon('btn fa fa-search')
            .Event(EventType.Click, () => {
                this.Parent.ClearSelected();
                this.Parent.ReloadData().Done();
            }).End
            .Icon('btn fa fa-cog')
            .Title('Advance')
            .Event(EventType.Click, this.AdvancedOptions).End
            .Icon('btn fa fa-undo')
            .Title('Refresh')
            .Event(EventType.Click, this.RefershListView).End
            .Render();
        if (this.Meta.ShowHotKey && this.ParentGridView != null) {
            Html.Take(this.Element).Div.ClassName('hotkey-block')
                .Button2('F1', { className: 'btn btn-light btn-sm' }).Event(EventType.Click, this.ParentGridView.ToggleAll)
                .Attr('title', 'Uncheck all').End
                .Button2('F2', { className: 'btn btn-light btn-sm' }).Event(EventType.Click, (e) => {
                    var com = this.Parent.LastListViewItem.Children.find(x => x.Meta.Id === this.ParentGridView.LastComponentFocus.Id);
                    this.ParentGridView.ActionKeyHandler(e, this.ParentGridView.LastComponentFocus, this.ParentGridView.LastListViewItem, com, com.Element.Closest(MVVM.ElementType.td.toString()), KeyCodeEnum.F2);
                })
                .Attr('title', 'Filter except').End
                .Button2('F3', { className: 'btn btn-light btn-sm' }).Event(EventType.Click, (e) => {
                    var com = this.Parent.LastListViewItem.Children.find(x => x.Meta.Id === this.ParentGridView.LastComponentFocus.Id);
                    this.ParentGridView.ActionKeyHandler(e, this.ParentGridView.LastComponentFocus, this.ParentGridView.LastListViewItem, com, com.Element.Closest(MVVM.ElementType.td.toString()), KeyCodeEnum.F3);
                })
                .Attr('title', 'Summary selected').End
                .Button2('F4', { className: 'btn btn-light btn-sm' }).Event(EventType.Click, (e) => {
                    var com = this.Parent.LastListViewItem.Children.find(x => x.Meta.Id === this.ParentGridView.LastComponentFocus.Id);
                    this.ParentGridView.ActionKeyHandler(e, this.ParentGridView.LastComponentFocus, this.ParentGridView.LastListViewItem, com, com.Element.Closest(MVVM.ElementType.td.toString()), KeyCodeEnum.F4);
                })
                .Attr('title', 'Lọc tiếp theo các phép tính (Chứa: Bằng; Lớn hơn; Nhỏ hơn; Lớn hơn hoặc bằng;...)').End
                .Button2('F6', { className: 'btn btn-light btn-sm' }).Event(EventType.Click, (e) => {
                    this.ParentGridView.HotKeyF6Handler(e, KeyCodeEnum.F6);
                })
                .Attr('title', 'Quay lại lần lọc trước').End
                .Button2('F8', { className: 'btn btn-light btn-sm' }).Event(EventType.Click, (e) => {
                    var com = this.Parent.LastListViewItem.Children.find(x => x.Meta.Id === this.ParentGridView.LastComponentFocus.Id);
                    this.ParentGridView.ActionKeyHandler(e, this.ParentGridView.LastComponentFocus, this.ParentGridView.LastListViewItem, com, com.Element.Closest(MVVM.ElementType.td.toString()), KeyCodeEnum.F8);
                })
                .Attr('title', 'Xóa/ Vô hiệu hóa dòng hiện thời hoặc các dòng đánh dấu').End
                .Button2('F9', { className: 'btn btn-light btn-sm' }).Event(EventType.Click, (e) => {
                    var com = this.Parent.LastListViewItem.Children.find(x => x.Meta.Id === this.ParentGridView.LastComponentFocus.Id);
                    this.ParentGridView.ActionKeyHandler(e, this.ParentGridView.LastComponentFocus, this.ParentGridView.LastListViewItem, com, com.Element.Closest(MVVM.ElementType.td.toString()), KeyCodeEnum.F9);
                })
                .Attr('title', 'Lọc tại chỗ theo giá trị ô hiện thời').End
                .Button2('F10', { className: 'btn btn-light btn-sm' }).Event(EventType.Click, (e) => {
                    var com = this.Parent.LastListViewItem.Children.find(x => x.Meta.Id === this.ParentGridView.LastComponentFocus.Id);
                    this.ParentGridView.ActionKeyHandler(e, this.ParentGridView.LastComponentFocus, this.ParentGridView.LastListViewItem, com, com.Element.Closest(MVVM.ElementType.td.toString()), KeyCodeEnum.F10);
                })
                .Attr('title', 'Gộp theo cột hiện thời(thống kê lại số nội dung trong cột)').End
                .Button2('F11', { className: 'btn btn-light btn-sm' }).Event(EventType.Click, (e) => {
                    var com = this.Parent.LastListViewItem.Children.find(x => x.Meta.Id === this.ParentGridView.LastComponentFocus.Id);
                    this.ParentGridView.ActionKeyHandler(e, this.ParentGridView.LastComponentFocus, this.ParentGridView.LastListViewItem, com, com.Element.Closest(MVVM.ElementType.td.toString()), KeyCodeEnum.F11);
                })
                .Attr('title', 'Sắp xếp thứ tự tăng dần, giảm dần. (Shift+F11 để sort nhiều cấp)').End.Render();
        }
    }

    FullScreen() {
        var elem = this.Parent.Element;
        /*@
         if (elem.requestFullscreen) {
                        elem.requestFullscreen();
                    } else if (elem.webkitRequestFullscreen) { 
                                        elem.webkitRequestFullscreen();
                                } else if (elem.msRequestFullscreen) {
                        elem.msRequestFullscreen();
                    }
         */
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
        var files = e.Target['files'];
        if (!files || files.length === 0) {
            return;
        }

        var fileName = files[0].Name;
        var uploadForm = this._uploader.ParentElement;
        var formData = new FormData(uploadForm);
        var meta = this.Parent.Meta;
        Client.Instance.SubmitAsync(new XHRWrapper({
            FormData: formData,
            Url: `/user/importCsv?table=${meta.RefName}&comId=${meta.Id}&connKey=${meta.MetaConn}`,
            Method: HttpMethod.POST,
            ResponseMimeType: Utils.GetMimeType('csv')
        })).Done(success => {
            Toast.Success('Import excel success');
            this._uploader.Value = '';
        }).Catch(error => {
            Toast.Warning(error.Message);
            this._uploader.Value = '';
        });
    }

    /**
     * @param {Event} e
     */
    AdvancedOptions(e) {
        var buttonRect = e.Target.As().GetBoundingClientRect();
        var show = LocalStorage.GetItem(`Show${this.Meta.Id}`) ?? false;
        var ctxMenu = ContextMenu.Instance;
        ctxMenu.Top = buttonRect.Bottom;
        ctxMenu.Left = buttonRect.Left;
        ctxMenu.MenuItems = [
            { Icon: 'fa fa-search-plus mr-1', Text: 'Advanced search', Click: this.AdvancedSearch },
            { Icon: 'fa fa-search mr-1', Text: 'Show selected only', Click: this.FilterSelected },
            { Icon: 'fa fa-download mr-1', Text: 'Import csv', Click: obj => this._uploader.Click() },
            { Icon: 'fa fa-download mr-1', Text: 'Export all', Click: this.ExportAllData },
            { Icon: 'fa fal fa-ballot-check mr-1', Text: 'Export selected', Click: this.ExportSelectedData },
            { Icon: 'fa fa-download mr-1', Text: 'Customize export', Click: this.ExportCustomData },
        ];
        ctxMenu.Render();
    }

    RenderImportBtn() {
        Html.Take(this.Element).Form.Attr('method', 'POST').Attr('enctype', 'multipart/form-data')
            .Display(false).Input.Type('file').Id(`id_${this.GetHashCode()}`).Attr('name', 'files').Attr('accept', '.csv');
        this._uploader = Html.Context;
        this._uploader.AddEventListener(EventType.Change.toString(), ev => this.UploadCsv(ev));
        Html.Instance.End.End.Render();
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
        TabEditor.OpenPopup('Export CustomData', () => this.Exporter).Done();
    }

    /**
     * @returns {ExportCustomData}
     */
    get Exporter() {
        if (!this._export) {
            this._export = new ExportCustomData(this.Parent);
            this._export.ParentElement = TabEditor.Element;
            this._export.Disposed += () => this._export = null;
        }
        return this._export;
    }

    /**
     * @param {object} arg
     */
    ExportAllData(arg) {
        this.Exporter.Export();
    }

    /**
     * @param {object} arg
     */
    ExportSelectedData(arg) {
        if (!this.Parent.SelectedIds || this.Parent.SelectedIds.length === 0) {
            Toast.Warning('Select at least 1 one to export excel');
            return;
        }
        this.Exporter.Export(this.Parent.SelectedIds);
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

        const searchTerm = this.EntityVM.SearchTerm.trim().EncodeSpecialChar() || '';
        const headers = this.Parent.Header.filter(x => x && x.FieldName.HasNonSpaceChar() && !x.ComponentType.includes("Button"));
        const operators = headers.map(x => x.MapToFilterOperator(searchTerm)).filter(x => x.HasAnyChar());
        let finalFilter = operators.join(" or ");

        const basicsAddDate = this.Parent.Header.filter(x => x.AddDate).map(x => x.Id);
        const parentGrid = basicsAddDate.length && basicsAddDate.some(id => this.ParentGridView.AdvSearchVM.Conditions.some(cond => cond.FieldId === id && !cond.Value.IsNullOrWhiteSpace()));

        if (!parentGrid && this.EntityVM.StartDate) {
            const startDateCondition = `ds.[${this.DateTimeField}] >= '${this.EntityVM.StartDate.toISOString().slice(0, 10)}'`;
            const oldStartDate = this.Parent.Wheres.find(x => x.Condition.includes(`ds.[${this.DateTimeField}] >=`));
            if (!oldStartDate) {
                this.Parent.Wheres.push({ Condition: startDateCondition, Group: false });
            } else {
                oldStartDate.Condition = startDateCondition;
            }
            LocalStorage.SetItem("FromDate" + this.Parent.Meta.Id, this.EntityVM.StartDate.toISOString().slice(0, 10));
        } else if (!this.EntityVM.StartDate) {
            const startDateIndex = this.Parent.Wheres.findIndex(x => x.Condition.includes(`ds.[${this.DateTimeField}] >=`));
            if (startDateIndex !== -1) {
                this.Parent.Wheres.splice(startDateIndex, 1);
            }
            LocalStorage.RemoveItem("FromDate" + this.Parent.Meta.Id);
        }

        if (!parentGrid && this.EntityVM.EndDate) {
            if (finalFilter) {
                finalFilter += " and ";
            }
            const endDate = new Date(this.EntityVM.EndDate.getTime() + 86400000);  // Add one day
            const endDateCondition = `ds.[${this.DateTimeField}] < '${endDate.toISOString().slice(0, 10)}'`;
            const oldEndDate = this.Parent.Wheres.find(x => x.Condition.includes(`ds.[${this.DateTimeField}] <`));
            if (!oldEndDate) {
                this.Parent.Wheres.push({ Condition: endDateCondition, Group: false });
            } else {
                oldEndDate.Condition = endDateCondition;
            }
            LocalStorage.SetItem("ToDate" + this.Parent.Meta.Id, endDate.toISOString().slice(0, 10));
        } else if (!this.EntityVM.EndDate) {
            const endDateIndex = this.Parent.Wheres.findIndex(x => x.Condition.includes(`ds.[${this.DateTimeField}] <`));
            if (endDateIndex !== -1) {
                this.Parent.Wheres.splice(endDateIndex, 1);
            }
            LocalStorage.RemoveItem("ToDate" + this.Parent.Meta.Id);
        }

        if ((this.EntityVM.EndDate || this.EntityVM.StartDate) && this.Parent.Meta.ShowNull) {
            finalFilter += ` or ds.${this.DateTimeField} is null`;
        }
        return finalFilter;
    }

    /**
     * Refreshes the list view, clearing all filters and selections.
     */
    RefreshListView() {
        this.EntityVM.SearchTerm = '';
        this.EntityVM.StartDate = null;
        this.EntityVM.EndDate = null;
        this.UpdateView();
        if (!(this.Parent instanceof ListView)) {
            return;
        }
        const listView = this.Parent;
        listView.ClearSelected();
        listView.CellSelected = [];
        listView.AdvSearchVM.Conditions = [];
        listView.Wheres = [];
        listView.ApplyFilter().Done();
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

}

