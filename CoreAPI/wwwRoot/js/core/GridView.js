import { ListView } from './listView.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { OperatorEnum, KeyCodeEnum, OrderbyDirection} from './models/enum.js';
import { ValidationRule } from "./models/validationRule.js";
import { LangSelect } from "./utils/langSelect.js";
import { Client } from "./clients/client.js";
import EventType from './models/eventType.js';
import { ComponentType } from './models/componentType.js';
import { Str } from './utils/ext.js';
import ObservableArgs from './models/observable.js';
import { Action } from "./models/action.js";
import { Spinner } from './spinner.js';
import { Toast } from 'toast.js';
import { ContextMenu } from 'contextMenu.js';
import { CustomEventType } from 'models/customEventType.js';
import "./utils/fix.js";
import { ConfirmDialog } from 'confirmDialog.js';
import { Uuid7 } from 'structs/uuidv7.js';



export class GridView extends ListView {

    /**
     * @param {import("./models/component.js").Component} ui
     */
    constructor(ui) {
        super(ui);
        this.SummaryClass = "summary";
        this.CellCountNoSticky = 50;
        this._summarys = [];
        this.LastThClick = null;
        this.LastNumClick = null;
        this.AutoFocus = false;
        this.LoadRerender = false;
        this._waitingLoad = false;
        this._renderPrepareCacheAwaiter = 0;
        /** @type {HTMLElement} */
        this.DataTable = null;
        this.LastElementFocus = null;
        this.lastListViewItem = null;
        this._hasFirstLoad = false;
        this._renderIndexAwaiter = 0;
        this._lastScrollTop = 0;
        this.ToolbarColumn = {
            StatusBar: true,
            Label: '',
            Frozen: true
        };

        this.DOMContentLoaded = this.DOMContentLoadedHandler.bind(this);
    }

    static ToolbarColumn = {
        StatusBar: true,
        Label: '',
        Frozen: true
    };

    DOMContentLoadedHandler() {
        if (this.Meta.IsSumary) {
            this.AddSummaries();
        }
        this.PopulateFields();
    }

    PopulateFields() {
        if (this.Meta.PopulateField) {
            let fields = this.Meta.PopulateField.split(",");
            if (fields.length > 0) {
                this.EditForm.UpdateView(true, fields);
            }
        }
    }

    Rerender() {
        this.LoadRerender = true;
        this.Header = this.Header.filter(x => !x.Hidden);
        this.RenderTableHeader(this.Header);
        if (this.Editable) {
            this.AddNewEmptyRow();
        }
        this.RenderContent();
        this.StickyColumn(this);
        if (!this.Editable && this.RowData.Data.Nothing()) {
            this.NoRecordFound();
        } else {
            this.DisposeNoRecord();
        }
        if (this.Editable) {
            this.MainSection.Element.addEventListener('contextmenu', this.BodyContextMenuHandler.bind(this));
            this.EmptySection.Element.addEventListener('contextmenu', this.BodyContextMenuHandler.bind(this));
        }
        this.RenderIndex();
    }

    StickyColumn(rows, top = null) {
        let shouldStickEle = ["th", "td"];
        let frozen = rows.FilterChildren(x => x.Meta != null && x.Meta.Frozen, x => !(x instanceof ListViewSearch));
        frozen.forEach(x => {
            let cell = x.Element;
            let isCell = shouldStickEle.includes(x.Element.tagName.toLowerCase());
            if (!isCell) {
                cell = x.Element.closest("td");
            }
            if (top && top.HasAnyChar()) {
                Html.Take(cell).Sticky({ top });
            } else {
                Html.Take(cell).Sticky({ left: "0" });
            }
        });
    }

    AddSections() {
        if (this.HeaderSection && this.HeaderSection.Element != null) {
            return;
        }
        let html = Html.Take(this.ParentElement);
        let id = "collapse" + this.Meta.Id;
        let idtb = "tb" + this.Meta.Id;
        if (this.Meta.IsCollapsible) {
            html.Div.ClassName("card mb-0")
                .Div.ClassName("card-header")
                .H5.ClassName("mb-0")
                .A.ClassName("btn btn-primary")
                .DataAttr("toggle", "collapse").Href("#" + id)
                .Attr("aria-expanded", "false")
                .Attr("aria-controls", id).Text(this.Meta.Label).EndOf(".card");
        }
        html.Div.Event('keydown', e => this.HotKeyF6Handler(e, e.KeyCodeEnum())).ClassName("grid-wrapper " + (this.Meta.IsCollapsible ? "collapse multi-collapse" : "")).Id(id)
            .ClassName(this.Editable ? "editable" : "");
        this.Element = Html.Context;
        if (this.Meta.CanSearch) {
            Html.Instance.Div.ClassName("grid-toolbar search").End.Render();
        }
        this.ListViewSearch = new ListViewSearch(this.Meta);
        this.ListViewSearch.Entity = new ListViewSearchVM();
        if (this.Meta.DefaultAddStart.HasValue) {
            let pre = this.Meta.DefaultAddStart.Value;
            this.ListViewSearch.EntityVM.StartDate = new Date(Date.now() + pre * 24 * 3600 * 1000);
        }
        let lFrom = window.localStorage.getItem("FromDate" + this.Meta.Id);
        if (lFrom != null) {
            this.ListViewSearch.EntityVM.StartDate = new Date(lFrom);
        }
        if (this.Meta.DefaultAddEnd.HasValue) {
            let pre = this.Meta.DefaultAddEnd.Value;
            this.ListViewSearch.EntityVM.EndDate = new Date(Date.now() + pre * 24 * 3600 * 1000);
        }
        let lTo = window.localStorage.getItem("ToDate" + this.Meta.Id);
        if (lTo != null) {
            this.ListViewSearch.EntityVM.EndDate = new Date(lTo);
        }
        this.AddChild(this.ListViewSearch);
        this.DataTable = Html.Take(this.Element).Div.ClassName("table-wrapper").Table.ClassName("table").Id(idtb).GetContext();
        Html.Instance.Thead.TabIndex(-1).End.TBody.ClassName("empty").End.TBody.End.TFooter.Render();

        this.FooterSection = new ListViewSection(Html.Context);
        this.FooterSection.ParentElement = this.DataTable;
        this.AddChild(this.FooterSection);

        this.MainSection = new ListViewSection(this.FooterSection.Element.previousElementSibling);
        this.MainSection.ParentElement = this.DataTable;
        this.AddChild(this.MainSection);

        this.EmptySection = new ListViewSection(this.MainSection.Element.previousElementSibling);
        this.EmptySection.ParentElement = this.DataTable;
        this.AddChild(this.EmptySection);

        this.HeaderSection = new ListViewSection(this.EmptySection.Element.previousElementSibling);
        this.HeaderSection.ParentElement = this.DataTable;
        this.AddChild(this.HeaderSection);
        Html.Instance.EndOf(".table-wrapper");
        this.RenderPaginator();
    }

    SwapList(oldIndex, newIndex) {
        let item = this.Header[oldIndex];
        this.Header.splice(oldIndex, 1);
        this.Header.splice(newIndex, 0, item);
    }
    SwapHeader(oldIndex, newIndex) {
        const item = this.Header[oldIndex];
        this.Header.splice(oldIndex, 1);
        this.Header.splice(newIndex, 0, item);
    }

    ClickHeader(e, header) {
        let index = this.LastNumClick;
        const table = this.DataTable;
        if (this.LastNumClick !== null) {
            table.querySelectorAll('tr:not(.summary)').forEach(function(row) {
                if(row.hasAttribute('virtualrow') || row.classList.contains('group-row')){
                    return;
                }
                const cells = Array.from(row.querySelectorAll('th, td'));
                cells[index].style.removeProperty("background-color");
                cells[index].style.removeProperty("color");
            });
        }

        const th = e.target.closest("th");
        const tr = Array.from(th.parentElement.querySelectorAll("th"));
        index = tr.findIndex(x => x === th);
        if (index < 0) {
            return;
        }
        this.LastThClick = th;
        this.LastNumClick = index;
        table.querySelectorAll('tr:not(.summary)').forEach(function(row) {
            if(row.hasAttribute('virtualrow') || row.classList.contains('group-row')){
                return;
            }
            const cells = Array.from(row.querySelectorAll('th, td'));
            cells[index].style.backgroundColor = "#cbdcc2";
            cells[index].style.color = "#000";
        });
    }

    FocusOutHeader(e, header) {
        let index = this.LastNumClick;
        const table = this.DataTable;
        if (this.LastNumClick !== null) {
            table.querySelectorAll('tr:not(.summary)').forEach(function(row) {
                if(row.hasAttribute('virtualrow') || row.classList.contains('group-row')){
                    return;
                }
                const cells = Array.from(row.querySelectorAll('th, td'));
                cells[index].style.removeProperty("background-color");
                cells[index].style.removeProperty("color");
            });
        }
    }

    ThHotKeyHandler(e, header) {
        if (this.Meta.Focus) {
            return;
        }
        const keyCode = e.keyCode; // Assuming keyCode is directly accessible
        if (keyCode === 39) { // Right arrow
            e.stopPropagation();
            const th = e.target.closest("th");
            const tr = Array.from(th.parentElement.querySelectorAll("th"));
            const index = tr.findIndex(x => x === th);

            th.parentElement.parentElement.parentElement.querySelectorAll('tr').forEach(function(row) {
                if(row.hasAttribute('virtualrow') || row.classList.contains('group-row')){
                    return;
                }
                const cells = Array.from(row.querySelectorAll('th, td'));
                if(cells[0].classList.contains('summary-header')){
                    return;
                }
                var draggingColumnIndex = index;
                var endColumnIndex = index + 1;
                if(draggingColumnIndex > endColumnIndex) {
                    cells[endColumnIndex].parentNode.insertBefore(cells[draggingColumnIndex], cells[endColumnIndex]);
                } else {
                    cells[endColumnIndex].parentNode.insertBefore(cells[draggingColumnIndex], cells[endColumnIndex].nextSibling);
                }
                cells[draggingColumnIndex].style.backgroundColor = "#cbdcc2";
            });
            this.SwapList(index - 1, index);
            this.SwapHeader(index, index + 1);
            this.ResetOrder();
            this.UpdateHeaders(this.GetHeaderSettings(), ['Order', 'FieldName']);
            th.focus();
        }
        else if (keyCode === 37) { // Left arrow
            e.stopPropagation();
            const th1 = e.target.closest("th");
            const tr1 = Array.from(th1.parentElement.querySelectorAll("th"));
            const index1 = tr1.findIndex(x => x === th1);

            th1.parentElement.parentElement.parentElement.querySelectorAll('tr').forEach(function(row) {
                if(row.hasAttribute('virtualrow') || row.classList.contains('group-row')){
                    return;
                }
                const cells = Array.from(row.querySelectorAll('th, td'));
                if(cells[0].classList.contains('summary-header')){
                    return;
                }
                var draggingColumnIndex = index1;
                var endColumnIndex = index1 - 1;
                if(draggingColumnIndex > endColumnIndex) {
                    cells[endColumnIndex].parentNode.insertBefore(cells[draggingColumnIndex], cells[endColumnIndex]);
                } else {
                    cells[endColumnIndex].parentNode.insertBefore(cells[draggingColumnIndex], cells[endColumnIndex].nextSibling);
                }
                cells[draggingColumnIndex].style.backgroundColor = "#cbdcc2";
            });
            this.SwapList(index1 - 1, index1 - 2);
            this.SwapHeader(index1, index1 - 1);
            this.ResetOrder();
            this.UpdateHeaders(this.GetHeaderSettings(), ['Order', 'FieldName']);
            th1.focus();
        }
    }

    async FilterInSelected(e) {
        const hotKeyModel = this.AsHotKeyModel(e);
        if (this._waitingLoad) {
            clearTimeout(this._renderPrepareCacheAwaiter);
        }

        if (!hotKeyModel.Operator) {
            return;
        }

        const header = this.Header.find(x => x.FieldName === hotKeyModel.FieldName);
        if (!header) return;

        const lastFilterKey = `LastSearch${this.Meta.Id}${header.Id}`;
        let subFilter = window.localStorage.getItem(lastFilterKey) || '';

        const inputType = header.ComponentType === 'Datepicker' ? 'date' :
                          header.ComponentType === 'Number' ? 'number' : 'text';

        const result = await Swal.fire({
            title: `Enter ${header.Label} to search ${hotKeyModel.OperatorText}`,
            input: inputType,
            inputValue: subFilter,
            inputAttributes: {
                autocapitalize: 'off',
                autocorrect: 'off'
            },
            showCancelButton: true,
            preConfirm: (value) => {
                if (!value) {
                    Swal.showValidationMessage(`You need to write something!`);
                }
                return value;
            }
        });

        if (result.value) {
            const value = result.value.trim();
            const valueText = value; 
            window.localStorage.setItem(lastFilterKey, value);

            // Update or add to CellSelected
            const cellIndex = this.CellSelected.findIndex(x => x.FieldName === hotKeyModel.FieldName && x.Operator === OperatorEnum.In);
            if (cellIndex !== -1 && !hotKeyModel.Shift) {
                this.CellSelected[cellIndex].Value = value;
                this.CellSelected[cellIndex].ValueText = valueText;
            } else {
                this.CellSelected.push({
                    FieldName: hotKeyModel.FieldName,
                    FieldText: header.Label,
                    ComponentType: header.ComponentType,
                    Shift: hotKeyModel.Shift,
                    Value: value,
                    ValueText: valueText,
                    Operator: hotKeyModel.Operator,
                    OperatorText: hotKeyModel.OperatorText,
                });
            }
            this.ActionFilter();
        }
    }

    AsHotKeyModel(e) {
        return {
            FieldName: e.FieldName,
            OperatorText: e.operatorText,
            Operator: e.operator,
            Shift: e.shiftKey,
            ActValue: e.actValue,
            Value: e.value,
            ValueText: e.valueText,
        };
    }

    async ActionFilter() {
        if (this.CellSelected.length === 0) {
            this.NoCellSelected();
            return;
        }
        Spinner.AppendTo(this.DataTable);
        const dropdowns = this.CellSelected.filter(x => (x.value || x.valueText) && (x.componentType === 'SearchEntry' || x.FieldName.includes(".")));
        const data = await this.FilterDropdownIds(dropdowns);
        let lisToast = [];
        this.CellSelected.forEach((cell, index) => {
            index = this.buildCondition(cell, data, index, lisToast);
        });
        Spinner.Hide();
        if (this.Meta.ComponentType === 'VirtualGrid' && this.Meta.CanSearch) {
            this.HeaderSection.Element.focus();
        }
        if (this.Meta.ComponentType === 'SearchEntry') {
            const search = this.Parent;
            if (search && search.input) {
                search.input.focus();
            }
        }
        Toast.success(lisToast.join("</br>"));
        this.ApplyFilter();
    }

    async FilterDropdownIds(dropdowns) {
        let tasks = dropdowns.map(x => {
            let header = this.Header.find(y => y.FieldName === x.FieldName);
            if (!x.isSearch) {
                let filterOperation = 'Like';
                switch (x.operator) {
                    case OperatorEnum.Lr:
                        filterOperation = 'StartWith';
                        break;
                    case OperatorEnum.Rl:
                        filterOperation = 'EndWidth';
                        break;
                    case OperatorEnum.NotIn:
                        filterOperation = 'NotLike';
                        break;
                }
                let sqlFilter = `${filterOperation} ds.[${header.formatData}] ${x.value}`;
                return Client.GetInstance().getIds({
                    comId: header.Id,
                    where: sqlFilter,
                    metaConn: this.metaConn,
                    dataConn: this.dataConn
                });
            } else {
                return Promise.resolve([x.value]);
            }
        });
        return Promise.all(tasks);
    }

    BuildCondition(cell, data, index, lisToast) {
        let where = '';
        const hl = this.Header.find(y => y.FieldName === cell.FieldName);
        let ids = null;
        const isNull = !cell.Value || cell.Value.trim() === '';
        let advo = cell.Operator === OperatorEnum.NotIn ? AdvSearchOperation.NotIn : AdvSearchOperation.In;

        if (hl.FieldName === this.IdField) {
            where = cell.Operator === OperatorEnum.NotIn ? `[ds].${cell.FieldName} not in (${cell.Value})` : `[ds].${cell.FieldName} in (${cell.Value})`;
            lisToast.push(`${cell.FieldText} <span class='text-danger'>${cell.OperatorText}</span> ${cell.ValueText}`);
        } else {
            if (hl.ComponentType === 'SearchEntry' && hl.FormatData.trim() !== '') {
                if (isNull) {
                    advo = cell.Operator === OperatorEnum.NotIn ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                    where = cell.Operator === OperatorEnum.NotIn ? `[ds].${cell.FieldName} is not null` : `[ds].${cell.FieldName} is null`;
                } else {
                    const idArr = data[index];
                    if (idArr && idArr.length > 0) {
                        ids = idArr.join();
                        where = cell.Operator === OperatorEnum.NotIn ? `[ds].${cell.FieldName} not in (${ids})` : `[ds].${cell.FieldName} in (${ids})`;
                    } else {
                        where = cell.Operator === OperatorEnum.NotIn ? `[ds].${cell.FieldName} != ${cell.Value}` : `[ds].${cell.FieldName} = ${cell.Value}`;
                    }
                    index++;
                }
                lisToast.push(`${hl.Label} <span class='text-danger'>${cell.OperatorText}</span> ${cell.ValueText}`);
            } else {
                switch (hl.ComponentType) {
                    case 'Number':
                    case 'Label':
                        where = this.BuildNumberCondition(cell, hl, isNull);
                        break;
                    case 'Checkbox':
                        where = this.BuildCheckboxCondition(cell, hl, isNull);
                        break;
                    case 'Datepicker':
                        where = this.BuildDateCondition(cell, hl, isNull);
                        break;
                    default:
                        where = this.BuildDefaultCondition(cell, hl, isNull);
                        break;
                }
                lisToast.push(`${hl.Label} <span class='text-danger'>${cell.OperatorText}</span> ${cell.ValueText}`);
            }
        }
        const value = ids || cell.Value;
        this.ProcessConditions(cell, advo, where, value, hl, lisToast);
        return index;
    }

    BuildDateCondition(cell, hl, isNull) {
        let where = '';
        if (!isNull) {
            const dateValue = new Date(cell.Value).toISOString().split('T')[0]; 
            switch (cell.Operator) {
                case OperatorEnum.NotIn:
                    where = `[ds].${hl.FieldName} != '${dateValue}'`;
                    break;
                case OperatorEnum.In:
                    where = `[ds].${hl.FieldName} = '${dateValue}'`;
                    break;
                case OperatorEnum.Gt:
                    where = `[ds].${hl.FieldName} > '${dateValue}'`;
                    break;
                case OperatorEnum.Lt:
                    where = `[ds].${hl.FieldName} < '${dateValue}'`;
                    break;
                case OperatorEnum.Ge:
                    where = `[ds].${hl.FieldName} >= '${dateValue}'`;
                    break;
                case OperatorEnum.Le:
                    where = `[ds].${hl.FieldName} <= '${dateValue}'`;
                    break;
                default:
                    where = `[ds].${hl.FieldName} = '${dateValue}'`;
            }
        } else {
            where = cell.Operator === OperatorEnum.NotIn ? `[ds].${hl.FieldName} is not null` : `[ds].${hl.FieldName} is null`;
        }
        return where;
    }

    BuildDefaultCondition(cell, hl, isNull) {
        let where = '';
        if (isNull) {
            where = cell.Operator === OperatorEnum.NotIn ? `[ds].${hl.FieldName} is not null` : `[ds].${hl.FieldName} is null`;
        } else {
            where = cell.Operator === OperatorEnum.NotIn ? `[ds].${hl.FieldName} != '${cell.Value}'` : `[ds].${hl.FieldName} = '${cell.Value}'`;
        }
        return where;
    }

    ProcessConditions(cell, advo, where, value, hl, lisToast) {
        console.log(`Processing condition for field: ${hl.FieldName}, Condition: ${where}`);
        this.AdvSearchVM.Conditions.push({
            field: hl.FieldName,
            operation: advo,
            condition: where,
            value: value
        });
        lisToast.push(`Applied condition ${where} on field ${hl.FieldName}`);
    }

    NoCellSelected() {
        this.MainSection.DisposeChildren();
        this.ApplyFilter();
        if (this.Meta.ComponentType === 'VirtualGrid' && this.Meta.CanSearch) {
            this.HeaderSection.Element.focus();
        }
        if (this.Meta.ComponentType === 'SearchEntry') {
            const search = this.Parent;
            if (search && search._input) {
                search._input.focus();
            }
        }
    }

    ApplyLocal() {
        const tb = this.DataTable;
        let rows;
        if (this.Meta.TopEmpty) {
            rows = tb.tBodies[tb.tBodies.length - 1].children;
        } else {
            rows = tb.tBodies[0].children;
        }
        if (!this.CellSelected.length) {
            Array.from(rows).forEach(row => row.classList.remove("d-none"));
            return;
        }
        const listNone = [];
        const header = this.Header.findIndex(y => y.FieldName === this.CellSelected[0].FieldName);

        this.CellSelected.forEach(cell => {
            Array.from(rows).forEach(row => {
                const cells = row.children;
                if (!cells[header]) return;
                const cellText = cells[header].textContent || '';
                if (cell.Operator === OperatorEnum.In) {
                    if (!cellText.toLowerCase().includes(cell.ValueText.toLowerCase())) {
                        if (!listNone.includes(row)) {
                            listNone.push(row);
                        }
                    }
                } else {
                    if (cellText.toLowerCase().indexOf(cell.ValueText.toLowerCase()) > -1) {
                        if (!listNone.includes(row)) {
                            listNone.push(row);
                        }
                    }
                }
            });
        });

        Array.from(rows).forEach(row => {
            if (listNone.includes(row)) {
                row.classList.add("d-none");
            } else {
                row.classList.remove("d-none");
                if (!this.LastElementFocus) {
                    this.LastElementFocus = row.children[header];
                }
            }
        });

        if (this.LastElementFocus) {
            this.LastElementFocus.focus();
            this.LastElementFocus = null;
        }
    }

    FilterSelected(hotKeyModel) {
        if (!hotKeyModel.Operator) {
            return;
        }
        if (!this.cellSelected.some(x => x.FieldName === hotKeyModel.FieldName && x.Value === hotKeyModel.Value && x.ValueText === hotKeyModel.ValueText && x.Operator === hotKeyModel.Operator)) {
            const header = this.header.find(x => x.FieldName === hotKeyModel.FieldName);
            this.cellSelected.push({
                FieldName: hotKeyModel.FieldName,
                FieldText: header ? header.Label : '',
                ComponentType: header ? header.ComponentType : '',
                Value: hotKeyModel.Value,
                ValueText: hotKeyModel.ValueText,
                Operator: hotKeyModel.Operator,
                OperatorText: hotKeyModel.OperatorText,
                IsSearch: hotKeyModel.ActValue,
            });
            this._summarys.push(document.createElement('HTMLElement'));
        }
        this.ActionFilter();
    }

    DisposeSumary() {
        if (this._summarys.length > 0) {
            const lastSummary = this._summarys[this._summarys.length - 1];
            lastSummary.remove(); 
            this._summarys.pop();
        }
        if (this.lastListViewItem && this.LastElementFocus) {
            this.lastListViewItem.Focused(true);
            this.LastElementFocus.focus();
        }
    }

    HiddenSumary() {
        const lastSummary = this._summarys[this._summarys.length - 1];
        if (lastSummary) {
            lastSummary.style.display = 'none'; 
        }
    }

    SearchDisplayRows() {
        const table = this.DataTable;
        const rows = table.tBodies[table.tBodies.length - 1].children;
        for (let i = 0; i < rows.length; i++) {
            if (rows[i].classList.contains("virtual-row")) {
                continue;
            }
            const cells = rows[i].childNodes;
            let found = false;
            for (let j = 0; j < cells.length; j++) {
                const htmlElement = cells[j];
                const input = htmlElement.querySelector("input:first-child");
                let cellText;
                if (input !== null) {
                    cellText = input.value;
                } else {
                    cellText = cells[j].textContent || "";
                }
                if (Utils.DecodeSpecialChar(cellText).toLowerCase().indexOf(Utils.DecodeSpecialChar(this.ListViewSearch.EntityVM.FullTextSearch.toLowerCase())) > -1) {
                    found = true;
                    break;
                }
            }
            if (found) {
                rows[i].classList.remove("d-none");
            } else {
                rows[i].classList.add("d-none");
            }
        }
    }

    FocusCell(e, header) {
        const td = e.target.closest("td");

        // Clearing focus on other cells
        const table = e.target.closest('table');
        table.querySelectorAll("tbody tr").forEach(tr => tr.classList.remove("focus"));
        table.querySelectorAll("tbody td").forEach(td => td.classList.remove("cell-selected"));

        // Adding focus class to the current row and cell
        td.closest("tr").classList.add("focus");
        td.classList.add("cell-selected");
    }

    ActionKeyHandler(e, header, focusedRow, com, el, keyCode) {
        let fieldName = "";
        let text = "";
        let value = "";

        if ([KeyCodeEnum.F4, KeyCodeEnum.F8, KeyCodeEnum.F9, KeyCodeEnum.F10, KeyCodeEnum.F11, KeyCodeEnum.F2, KeyCodeEnum.UpArrow, KeyCodeEnum.DownArrow, KeyCodeEnum.Home, KeyCodeEnum.End, KeyCodeEnum.Insert].includes(keyCode) || (e.ctrlKey || e.metaKey) && keyCode === KeyCodeEnum.D) {
            e.preventDefault();
            e.stopPropagation();
            if (!com) {
                return;
            }
            fieldName = com.FieldName;
            switch (com.Meta.ComponentType) {
                case "SearchEntry":
                    value = focusedRow.Entity.GetPropValue(header.FieldName).toString().EncodeSpecialChar();
                    break;
                case "Number":
                    value = focusedRow.Entity.GetPropValue(header.FieldName).toString().replace(",", "");
                    break;
                case "Checkbox":
                    value = com.GetValue().toString().toLowerCase();
                    break;
                default:
                    value = com.GetValue().toString().EncodeSpecialChar();
                    break;
            }
            if (value === null) {
                text = null;
            } else {
                if (!com.Meta.Editable) {
                    text = com.GetValueTextAct().toString().DecodeSpecialChar();
                } else {
                    text = com.GetValueText().toString().DecodeSpecialChar();
                }
            }
        }

        switch (keyCode) {
            case KeyCodeEnum.F2:
                this.FilterSelected({ Operator: 2, OperatorText: "Exclude", Value: value, FieldName: fieldName, ValueText: text, ActValue: true });
                break;
            case KeyCodeEnum.F4:
                this.ProcessFilterDetail(e, com, el, fieldName, text, value);
                break;
            case KeyCodeEnum.F8:
                this.ProcessHardDelete();
                break;
            case KeyCodeEnum.F9:
                this.FilterSelected({ Operator: 1, OperatorText: "Contains", Value: value, FieldName: fieldName, ValueText: text, ActValue: true });
                com.Focus();
                break;
            case KeyCodeEnum.F11:
                this.ProcessSort(e, com);
                break;
            case KeyCodeEnum.UpArrow:
                this.MoveFocusUp(e, com, fieldName);
                break;
            case KeyCodeEnum.DownArrow:
                this.MoveFocusDown(e, com, fieldName);
                break;
            case KeyCodeEnum.LeftArrow:
                this.MoveFocusLeft(e, com);
                break;
            case KeyCodeEnum.RightArrow:
                this.MoveFocusRight(e, com);
                break;
            case KeyCodeEnum.Home:
                this.MoveFocusHome();
                break;
            case KeyCodeEnum.End:
                this.MoveFocusEnd();
                break;
            case KeyCodeEnum.Insert:
                this.ToggleItemSelection();
                break;
            default:
                break;
        }
    }
    
    MoveFocusUp(e, com, fieldName) {
        let currentItemUp = this.GetItemFocus();
        if (currentItemUp.RowNo === 0 && !(e.ctrlKey || e.metaKey)) {
            return;
        }
        let upItemUp = this.AllListViewItem.filter(x => !x.GroupRow).find(x => x.RowNo === currentItemUp.RowNo - 1);
        if (!upItemUp) {
            if (this.Meta.CanAdd) {
                upItemUp = this.EmptySection.FirstChild;
            } else {
                return;
            }
        }
        this.CopyValue(e, com, fieldName, currentItemUp, upItemUp);
    }

    MoveFocusDown(e, com, fieldName) {
        let currentItemDown = this.GetItemFocus();
        if (!currentItemDown && !(e.ctrlKey || e.metaKey)) {
            return;
        }
        let downItemDown = this.AllListViewItem.filter(x => !x.GroupRow).find(x => x.RowNo === currentItemDown.RowNo + 1);
        if (!downItemDown) {
            if (this.Meta.CanAdd) {
                downItemDown = this.EmptySection.FirstChild;
            } else {
                return;
            }
        }
        this.CopyValue(e, com, fieldName, currentItemDown, downItemDown);
    }

    MoveFocusLeft(e, com) {
        if (!this.Meta.IsRealtime && !(e.ctrlKey || e.metaKey)) {
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        let currentItemLeft = this.LastListViewItem;
        let leftItem = currentItemLeft.Children.find(x => x.Element.closest('td') === com.Element.closest('td').previousElementSibling);
        if (!leftItem || currentItemLeft.Children.length === 0) {
            return;
        }
        leftItem.ParentElement?.focus();
        leftItem.Focus();
        if (leftItem.Meta.Editable && !leftItem.Disabled) {
            if (leftItem.Element instanceof HTMLInputElement) {
                leftItem.Element.selectionStart = 0;
                leftItem.Element.selectionEnd = leftItem.GetValueText().length;
            }
        }
    }

    MoveFocusRight(e, com) {
        if (!this.Meta.IsRealtime && !(e.ctrlKey || e.metaKey)) {
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        let currentItemRight = this.LastListViewItem;
        let rightItem = currentItemRight.Children.find(x => x.Element.closest('td') === com.element.closest('td').nextElementSibling);
        if (!rightItem) {
            return;
        }
        rightItem.ParentElement?.focus();
        rightItem.Focus();
        if (rightItem.Meta.Editable && !rightItem.Disabled) {
            if (rightItem.Element instanceof HTMLInputElement) {
                rightItem.Element.selectionStart = 0;
                rightItem.Element.selectionEnd = rightItem.GetValueText().length;
            }
        }
    }

    MoveFocusHome() {
        let firstItem = this.AllListViewItem[0];
        firstItem?.Focus();
        this.DataTable.parentElement.scrollTop = 0;
    }

    MoveFocusEnd() {
        let lastItem = this.AllListViewItem[this.AllListViewItem.length - 1];
        lastItem?.Focus();
        this.DataTable.parentElement.scrollTop = this.DataTable.parentElement.scrollHeight;
    }

    ToggleItemSelection() {
        let currentItem = this.GetItemFocus();
        currentItem.Selected = !currentItem.Selected;
    }

    ProcessHardDelete() {
        if (this.Disabled) {
            return;
        }
        const selectedRows = this.GetSelectedRows();
        if (selectedRows.length === 0) {
            Toast.Warning("Vui lòng chọn dòng cần xóa");
            return;
        }
        const isOwner = selectedRows.every(x => Utils.IsOwner(x, false));
        const canDelete = this.CanDo(x => x.CanDelete && isOwner || x.CanDeleteAll);
        if (canDelete) {
            this.HardDeleteSelected();
        }
    }

    ProcessFilterDetail(e, com, el, fieldName, text, value) {
        const menu = ContextMenu.Instance;
        menu.PElement = this.MainSection.Element;
        menu.Top = el.getBoundingClientRect().top;
        menu.Left = el.getBoundingClientRect().left;
        menu.MenuItems = [
            {
                Icon: "fal fa-angle-double-right",
                Text: "Chứa", Click: this.FilterInSelected,
                Parameter: { Operator: OperatorEnum.In, OperatorText: "Chứa", Value: value, FieldName: fieldName, ValueText: text, Shift: e.shiftKey },
                Ele: undefined,
                Style: '',
                Disabled: false,
                MenuItems: []
            },
            {
                Icon: "fal fa-not-equal", Text: "Không chứa",
                Click: this.FilterInSelected,
                Parameter: { Operator: OperatorEnum.NotIn, OperatorText: "Không chứa", Value: value, FieldName: fieldName, ValueText: text, Shift: e.shiftKey },
                Ele: undefined,
                Style: '',
                Disabled: false,
                MenuItems: []
            },
            {
                Icon: "fal fa-hourglass-start", Text: "Trái phải", Click: this.FilterInSelected,
                Parameter: { Operator: OperatorEnum.Lr, OperatorText: "Trái phải", Value: value, FieldName: fieldName, ValueText: text, Shift: e.shiftKey },
                Ele: undefined,
                Style: '',
                Disabled: false,
                MenuItems: []
            },
            {
                Icon: "fal fa-hourglass-end", Text: "Phải trái", Click: this.FilterInSelected,
                Parameter: { Operator: OperatorEnum.Rl, OperatorText: "Phải trái", Value: value, FieldName: fieldName, ValueText: text, Shift: e.shiftKey },
                Ele: undefined,
                Style: '',
                Disabled: false,
                MenuItems: []
            }
        ];

        if (com.Meta.ComponentType === "Number" || com.Meta.ComponentType === "Datepicker") {
            menu.MenuItems.push(
                {
                    Icon: "fal fa-greater-than", Text: "Lớn hơn", Click: this.FilterInSelected,
                    Parameter: { Operator: OperatorEnum.Gt, OperatorText: "Lớn hơn", Value: value, FieldName: fieldName, ValueText: text, Shift: e.shiftKey },
                    Ele: undefined,
                    Style: '',
                    Disabled: false,
                    MenuItems: []
                },
                { Icon: "fal fa-less-than", Text: "Nhỏ hơn", Click: this.FilterInSelected,
                    Parameter: { Operator: OperatorEnum.Lt, OperatorText: "Nhỏ hơn", Value: value, FieldName: fieldName, ValueText: text, Shift: e.shiftKey },
                    Ele: undefined,
                    Style: '',
                    Disabled: false,
                    MenuItems: []},
                { Icon: "fal fa-greater-than-equal", Text: "Lớn hơn bằng", Click: this.FilterInSelected,
                    Parameter: { Operator: OperatorEnum.Ge, OperatorText: "Lớn hơn bằng", Value: value, FieldName: fieldName, ValueText: text, Shift: e.shiftKey },
                    Ele: undefined,
                    Style: '',
                    Disabled: false,
                    MenuItems: []},
                { Icon: "fal fa-less-than-equal", Text: "Nhỏ hơn bằng", Click: this.FilterInSelected,
                    Parameter: { Operator: OperatorEnum.Le, OperatorText: "Nhỏ hơn bằng", Value: value, FieldName: fieldName, ValueText: text, Shift: e.shiftKey },
                    Ele: undefined,
                    Style: '',
                    Disabled: false,
                    MenuItems: []}
            );
        }
        menu.Render();
    }

    ProcessSort(e, com) {
        if (com.Meta.ComponentType === "Button") {
            return;
        }
        let th = this.HeaderSection.Children.find(x => x.Meta.Id === com.Meta.Id);
        th.Element.classList.remove("desc", "asc");
        const fieldName = com.ComponentType === "SearchEntry" ? com.Meta.FieldText : com.FieldName;
        const sort = {
            FieldName: fieldName,
            OrderbyDirectionId: OrderbyDirection.ASC,
            ComId: com.Meta.Id,
        };
        if (!this.AdvSearchVM.OrderBy.length) {
            this.AdvSearchVM.OrderBy = [sort];
            th.Element.classList.add("desc");
        } else {
            const existSort = this.AdvSearchVM.OrderBy.find(x => x.FieldName === fieldName);
            if (existSort) {
                this.AlterExistSort(th, existSort);
            } else {
                const shiftKey = e.shiftKey;
                this.RemoveOtherSorts(shiftKey);
                th.Element.classList.add("desc");
                this.AdvSearchVM.OrderBy.push(sort);
            }
        }
        localStorage.setItem("OrderBy" + this.Meta.Id, JSON.stringify(this.AdvSearchVM.OrderBy));
        this.ReloadData();
    }

    AlterExistSort(th, existSort) {
        if (existSort.OrderbyDirectionId === OrderbyDirection.ASC) {
            existSort.OrderbyDirectionId = OrderbyDirection.DESC;
            th.Element.classList.replace("asc", "desc");
        } else {
            const index = this.AdvSearchVM.OrderBy.indexOf(existSort);
            if (index !== -1) {
                this.AdvSearchVM.OrderBy.splice(index, 1);
            }
        }
    }

    RemoveOtherSorts(shiftKey) {
        if (shiftKey) return;
        this.HeaderSection.Children.forEach(x => {
            x.Element.classList.remove("desc");
            x.Element.classList.remove("asc");
        });
        this.AdvSearchVM.OrderBy.length = 0; 
    }

    CopyValue(e, com, fieldName, currentItem, upItem) {
        this.LastListViewItem = upItem;
        currentItem.Focused(false);
        upItem.Focused(true);
        if (!fieldName || fieldName.trim() === '') {
            return;
        }
        let nextcom = upItem.FilterChildren(x => x.Meta.Id === com.Meta.Id)[0];
        if (nextcom) {
            this.LastComponentFocus = nextcom.Meta;
            nextcom.ParentElement?.focus();
            nextcom.Focus();
            if (nextcom.Meta.Editable && !nextcom.Disabled) {
                if (nextcom.Element instanceof HTMLInputElement) {
                    nextcom.Element.selectionStart = 0;
                    nextcom.Element.selectionEnd = nextcom.GetValueText().length;
                }
            }
            this.LastElementFocus = nextcom.Element;
            if (e.shiftKey) {
                upItem.Entity.SetComplexPropValue(fieldName, com.GetValue());
                let updated = upItem.FilterChildren(x => x.FieldName === nextcom.FieldName)[0];
                if (updated && (!updated.Disabled || updated.Meta.Editable)) {
                    updated.Dirty = true;
                    (async () => {
                        if (updated.Meta.ComponentType === "SearchEntry") {
                            updated.UpdateView();
                            let dropdown = com; 
                            updated.PopulateFields(dropdown.Matched);
                            await updated.DispatchEvent(updated.Meta.Events, "change", upItem.Entity, dropdown.Matched);
                        } else {
                            updated.UpdateView();
                            updated.PopulateFields();
                            await updated.DispatchEvent(updated.Meta.Events, "change", upItem.Entity);
                        }
                        await upItem.ListViewSection.ListView.DispatchEvent(upItem.ListViewSection.ListView.Meta.Events, "change", upItem.Entity);
                        if (this.Meta.IsRealtime) {
                            await upItem.PatchUpdateOrCreate();
                        }
                    })();
                }
            }
        }
    }

    RenderViewPort(count = true, firstLoad = false, skip = null) {
        return;
    }

    HotKeyF6Handler(e, keyCode) {
        switch (keyCode) {
            case KeyCodeEnum.F6:
                e.preventDefault();
                e.stopPropagation();
                if (this._summarys.length) {
                    let lastElement = this._summarys[this._summarys.length - 1];
                    if (this.Meta.FilterLocal) {
                        if (lastElement.innerHTML === "") {
                            this.CellSelected.pop();
                            this.ActionFilter();
                            this._summarys.pop();
                        } else {
                            if (lastElement.style.display === "none") {
                                this.CellSelected.pop();
                                this.ActionFilter();
                                lastElement.style.display = "";
                            } else {
                                this._summarys.pop();
                                lastElement.remove();
                            }
                        }
                        return;
                    }
                    if (lastElement.innerHTML === "") {
                        this.CellSelected.pop();
                        this.Wheres.pop();
                        let last = this.AdvSearchVM.Conditions[this.AdvSearchVM.Conditions.length - 1];
                        if (last && last.Field.ComponentType === "Input" && !last.Value.trim()) {
                            this.AdvSearchVM.Conditions.pop();
                            this.AdvSearchVM.Conditions.pop();
                        } else {
                            this.AdvSearchVM.Conditions.pop();
                        }
                        this.ActionFilter();
                        this._summarys.pop();
                    } else {
                        if (this._waitingLoad) {
                            clearTimeout(this._renderPrepareCacheAwaiter);
                        }
                        if (lastElement.style.display === "none") {
                            this.CellSelected.pop();
                            this.Wheres.pop();
                            let last = this.AdvSearchVM.Conditions[this.AdvSearchVM.Conditions.length - 1];
                            if (last && last.Field.ComponentType === "Input" && !last.Value.trim()) {
                                this.AdvSearchVM.Conditions.pop();
                                this.AdvSearchVM.Conditions.pop();
                            } else {
                                this.AdvSearchVM.Conditions.pop();
                            }
                            this.ActionFilter();
                            lastElement.style.display = "";
                        } else {
                            this._summarys.pop();
                            lastElement.remove();
                        }
                    }
                }
                break;
            case KeyCodeEnum.F3:
                e.preventDefault();
                e.stopPropagation();
                this.GetRealTimeSelectedRows().then(selected => {
                    if (selected.length === 0) {
                        selected = this.RowData.Data;
                    }
                    let numbers = this.Header.filter(x => x.ComponentType === "Number");
                    if (numbers.length === 0) {
                        Toast.Warning("Vui lòng cấu hình");
                        return;
                    }
                    let listString = numbers.map(x => {
                        let val = selected.map(k => k[x.FieldName]).filter(k => k != null).reduce((a, b) => a + parseFloat(b), 0);
                        return x.Label + " : " + (val % 2 > 0 ? val.toFixed(2) : Math.round(val).toString());
                    });
                    Toast.Success(listString.join("</br>"), 6000);
                });
                break;
            case KeyCodeEnum.F1:
                e.preventDefault();
                e.stopPropagation();
                this.ToggleAll();
                break;
            case KeyCodeEnum.U:
                if (e.ctrlKey || e.metaKey) {
                    if (this.Disabled || !this.Meta.CanAdd) {
                        return;
                    }
                    e.preventDefault();
                    e.stopPropagation();
                    this.DuplicateSelected(e, true);
                }
                break;
            default:
                break;
        }
        if (!this.LastListViewItem || !this.LastListViewItem.Children) {
            return;
        }
        let com = this.LastListViewItem.Children.find(x => x.Meta.Id === this.LastComponentFocus?.Id);
        if (!com) {
            return;
        }
        this.ActionKeyHandler(e, this.LastComponentFocus, this.LastListViewItem, com, com.Element.closest('td'), keyCode);
    }

    async AddRow(rowData, index = 0, singleAdd = true) {
        let rowSection = await super.AddRow(rowData, index, singleAdd);
        this.StickyColumn(rowSection);
        this.RenderIndex();
        return rowSection;
    }

    AddNewEmptyRow() {
        if (this.Disabled || !this.Editable || (this.EmptySection && this.EmptySection.Children.length > 0)) {
            return;
        }

        let emptyRowData = {};
        if (this.Meta.DefaultVal && Utils.IsFunction(this.Meta.DefaultVal)) {
            let dfObj = this.Meta.DefaultVal.call(this, this);
            Object.keys(dfObj).forEach(key => {
                emptyRowData[key] = dfObj[key];
            });
        }

        emptyRowData[this.IdField] = null;
        let rowSection = this.RenderRowData(this.Header, emptyRowData, this.EmptySection, null, true);

        Object.keys(emptyRowData).forEach(field => {
            rowSection.PatchModel.push({
                Field: field,
                Value: emptyRowData[field]?.toString()
            });
        });

        this.StickyColumn(rowSection);

        if (!this.Meta.TopEmpty) {
            this.DataTable.insertBefore(rowSection.Element, this.MainSection.Element);
        } else {
            this.DataTable.insertBefore(rowSection.Element, this.EmptySection.Element);
        }

        this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterEmptyRowCreated, emptyRowData);
    }

    FilterColumns(components) {
        if (!components || components.length === 0) return components;

        const permission = this.EditForm.GetGridPolicies(components.map(x => x.Id), Utils.ComponentId);
        let headers = components.filter(x => !x.Hidden && x.Id !== this.Meta.Id)
            .filter(header => !header.IsPrivate || permission.filter(p => p.RecordId === header.Id).some(policy => policy.CanRead))
            .map(header => this.CalcTextAlign(header))
            .sort((a, b) => b.Frozen - a.Frozen || (b.ComponentType === "Button") - (a.ComponentType === "Button") || a.Order - b.Order);

        this.OrderHeaderGroup(headers);
        this.Header = [this.ToolbarColumn, ...headers].filter(x => x !== null);
        return this.Header;
    }

    async ApplyFilter() {
        this.DataTable.parentElement.scrollTop = 0;
        await this.ReloadData(this.cacheHeader = true);
    }

    ColumnResizeHandler() {
        const createResizableTable = (table) => {
            if (table === null) return;
            const cols = table.querySelectorAll('th');
            cols.forEach((col) => {
                // Add a resizer element to the column
                const resizer = document.createElement('div');
                resizer.classList.add('resizer');

                col.appendChild(resizer);

                createResizableColumn(col, resizer);
            });
        };

        const createResizableColumn = (col, resizer) => {
            let x = 0;
            let w = 0;

            const mouseDownHandler = (e) => {
                e.preventDefault();
                x = e.clientX;

                const styles = window.getComputedStyle(col);
                w = parseInt(styles.width, 10);

                document.addEventListener('mousemove', mouseMoveHandler);
                document.addEventListener('mouseup', mouseUpHandler);

                resizer.classList.add('resizing');
            };

            const mouseMoveHandler = (e) => {
                e.preventDefault();
                const dx = e.clientX - x;
                col.style.width = `${w + dx}px`;
                col.style.minWidth = `${w + dx}px`;
                col.style.maxWidth = `${w + dx}px`;
            };

            const mouseUpHandler = () => {
                this.UpdateHeader();
                resizer.classList.remove('resizing');
                document.removeEventListener('mousemove', mouseMoveHandler);
                document.removeEventListener('mouseup', mouseUpHandler);
            };

            resizer.addEventListener('mousedown', mouseDownHandler);
        };

        createResizableTable(this.DataTable);
    }

    RenderContent() {
        if (!this.LoadRerender) {
            this.Rerender();
        }
        this.AddSections();
        if (!this._hasFirstLoad && this.VirtualScroll) {
            this._hasFirstLoad = true;
            return;
        }
        let viewPort = this.GetViewPortItem();
        this.FormattedRowData = this.Meta.LocalRender ? this.Meta.LocalData : this.RowData.Data;
        if (!this.FormattedRowData || this.FormattedRowData.length === 0) {
            this.MainSection.DisposeChildren();
            this.DomLoaded();
            return;
        }
        this.DisposeNoRecord();
        if (this.VirtualScroll && this.FormattedRowData.length > viewPort) {
            this.FormattedRowData = this.FormattedRowData.slice(0, viewPort);
        }
        if (this.MainSection.Children.length > 0) {
            this.UpdateExistRowsWrapper(false, 0, viewPort);
            return;
        }
        this.MainSection.Show = false;
        this.FormattedRowData.forEach(rowData => {
            Html.Take(this.MainSection.Element);
            this.RenderRowData(this.Header, rowData, this.MainSection);
        });
        this.MainSection.Show = true;
        this.ContentRendered();
        this.DomLoaded();
    }

    SetFocusingCom() {
        if (this.AutoFocus) {
            return;
        }
        if (this.EntityFocusId != null && this.LastComponentFocus != null) {
            let element = this.MainSection.Children.flatMap(x => x.Children)
                .find(x => x.Entity[this.IdField].toString() === this.EntityFocusId && x.Meta.Id === this.LastComponentFocus.Id);
            if (element) {
                let lastListView = this.AllListViewItem.find(x => x.Entity[this.IdField].toString() === this.EntityFocusId);
                if (lastListView) {
                    lastListView.Focused(true);
                    element.ParentElement.classList.add("cell-selected");
                    this.LastListViewItem = lastListView;
                    this.LastComponentFocus = element.Meta;
                    this.LastElementFocus = element.Element;
                }
            } else {
                this.HeaderSection.Element.focus();
            }
        } else {
            this.HeaderSection.Element.focus();
        }
    }

    UpdateExistRowsWrapper(dirty, skip, viewPort) {
        if (!this._hasFirstLoad) {
            this._hasFirstLoad = true;
            return;
        }
        this.UpdateExistRows(dirty);
        this.RenderIndex();
        this.DomLoaded();
    }

    UpdateExistRows(dirty) {
        const updatedData = this.FormattedRowData.slice();
        const dataSections = this.AllListViewItem.slice(0, updatedData.length);

        dataSections.forEach((child, index) => {
            child.Entity = updatedData[index];
            this.FlattenChildren(child).forEach(x => {
                x.Entity = updatedData[index];
            });
            child.UpdateView();
        });

        const shouldAddRow = this.AllListViewItem.length <= updatedData.length;
        if (shouldAddRow) {
            updatedData.slice(dataSections.length).forEach(newRow => {
                // @ts-ignore
                const rs = this.RenderRowData(this.Header, newRow, this.MainSection);
                this.StickyColumn(rs);
            });
        } else {
            this.MainSection.Children.slice(updatedData.length).forEach(x => x.Dispose());
        }

        if (dirty !== undefined) {
            this.Dirty = dirty;
        }
        this.RenderIndex();
    }

    FlattenChildren(component) {
        const allChildren = [];
        const stack = [component];
        while (stack.length) {
            const current = stack.pop();
            if (current.Children) {
                allChildren.push(...current.Children);
                current.Children.forEach(child => stack.push(child));
            }
        }
        return allChildren;
    }

    RenderRowData(headers, row, section, index = null, emptyRow = false) {
        const tbody = section.element;
        const rowSection = new GridViewItem('tr', {
            EmptyRow: emptyRow,
            Entity: row,
            ParentElement: tbody,
            PreQueryFn: this._preQueryFn,
            ListView: this,
            Meta: this.Meta
        });
        section.AddChild(rowSection, index);

        const tr = document.createElement('tr');
        tr.tabIndex = -1;

        if (index !== null) {
            if (index >= tbody.children.length || index < 0) {
                index = 0;
            }
            tbody.insertBefore(tr, tbody.children[index]);
        } else {
            tbody.appendChild(tr);
        }

        rowSection.RenderRowData(headers, row, index, emptyRow);

        if (emptyRow) {
            this.Children.forEach(x => x.AlwaysLogHistory = true);
        }

        if (this.Disabled) {
            rowSection.SetDisabled(false, "btnEdit");
        }

        if (this.Meta.ComponentType !== 'FileUploadGrid') {
            if (row[Utils.IdField] != null) {
                tr.classList.remove("new-row");
            } else {
                tr.classList.add("new-row");
            }
        }

        return rowSection;
    }

    AddSummaries() {
        if (this.Header.every(x => !x.Summary || x.Summary.trim() === "")) {
            return;
        }

        const sums = this.Header.filter(x => x.Summary && x.Summary.trim() !== "");
        const summaryElements = this.MainSection.Element.querySelectorAll(`.${SummaryClass}`);
        summaryElements.forEach(x => x.remove());
        const count = new Set(sums.map(x => x.Summary)).size;

        sums.forEach(header => {
            this.RenderSummaryRow(header, this.Header, this.FooterSection.Element, count);
        });
    }

    // @ts-ignore
        DuplicateSelected(ev, addRow = false) {
        const originalRows = this.GetSelectedRows();
        const copiedRows = this.CloneRows(originalRows);
        if (!copiedRows.length || !this.CanWrite) {
            return;
        }

        Toast.Success("Đang Sao chép liệu !");
        this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforePasted, originalRows, copiedRows).then(() => {
            const index = this.GetStartIndex(ev, addRow);
            this.AddRowsNo(copiedRows, index).then(list => {
                this.RowsAdded(list, originalRows, copiedRows);
            });
        });
    }

    GetStartIndex(ev, addRow) {
        let index = this.AllListViewItem.findIndex(x => x.Selected);
        if (addRow) {
            if (ev.keyCode === KeyCodeEnum.U && (ev.ctrlKey || ev.metaKey)) {
                if (this.Meta.TopEmpty) {
                    index = 0;
                } else {
                    index = this.AllListViewItem[this.AllListViewItem.length - 1].RowNo;
                }
            }
        }
        return index;
    }

    RowsAdded(list, originalRows, copiedRows) {
        const lastChild = list[0] ? list[0].FilterChildren(x => x.Meta.Editable)[0] : null;
        if (lastChild) {
            lastChild.Focus();
        }
        this.RenderIndex();
        if (this.Meta.IsSumary) {
            this.AddSummaries();
        }
        this.ClearSelected();
        list.forEach(item => {
            item.Selected = true;
        });
        this.LastListViewItem = list[0] || null;
        if (this.Meta.IsRealtime) {
            Promise.all(list.map(x => x.PatchUpdateOrCreate())).then(() => {
                Toast.Success("Sao chép dữ liệu thành công!");
                this.Dirty = false;
            });
        } else {
            Toast.Success("Sao chép dữ liệu thành công!");
        }
        this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterPasted, originalRows, copiedRows);
    }

    RenderSummaryRow(sum, headers, footer, count) {
        let tr = this.CreateSummaryTableRow(sum, footer, count);
        if (!tr) {
            return;
        }
    
        const hasSummaryClass = tr.classList.contains("summary");
        const colSpan = sum.SummaryColSpan || 0;
        tr.classList.add("summary");
        if (!hasSummaryClass && headers.includes(sum)) {
            this.ResetSummaryRow(tr, colSpan);
        }
        if (!headers.includes(sum)) {
            this.ClearSummaryContent(tr);
            return;
        }
        this.SetSummaryHeaderText(sum, tr);
        this.CalcSumCol(sum, headers, tr, colSpan);
    }

    SetRowData(listData) {
        if (this.RowData._data === null || typeof this.RowData._data === "string") {
            this.RowData._data = listData;
        } else {
            this.RowData._data.length = 0; // Clear existing data
            if (listData.length > 0) {
                listData.forEach(item => this.RowData._data.push(item)); // Add new data
            }
        }
        this.RenderContent();
        if (this.Entity != null && this.ShouldSetEntity) {
            this.Entity.SetComplexPropValue(this.FieldName, this.RowData.Data);
        }
    }

    SetSummaryHeaderText(sum, tr) {
        if (!sum.Summary || sum.Summary.trim() === '') {
            return;
        }

        let cell = tr.cells[0];
        cell.colSpan = sum.SummaryColSpan;
        cell.textContent = sum.Summary;
        cell.classList.add('summary-header');
    }

    CreateSummaryTableRow(sum, footer, count) {
        if (!footer) {
            return null;
        }

        let summaryText = sum.Summary;
        let summaryRows = Array.from(footer.rows).filter(row => row.classList.contains('summary-header'));
        let existingSummaryRow = Array.from(summaryRows).reverse()
            .find(row => Array.from(row.cells).some(cell => cell.textContent === summaryText));

        if (!existingSummaryRow) {
            existingSummaryRow = summaryRows[summaryRows.length - 1];  // Gets the last summary row
        }

        if (summaryRows.length >= count) {
            return existingSummaryRow;
        }

        if (!this.MainSection.FirstChild) {
            return null;
        }

        let result = this.MainSection.FirstChild.cloneNode(true);  // Cloning the first row
        footer.appendChild(result);
        Array.from(result.children).forEach(child => child.innerHTML = '');  // Clearing cell contents
        return result;
    }

    CalcSumCol(header, headers, tr, colSpan) {
        const index = headers.indexOf(header);
        const cellVal = tr.cells[index - colSpan + 1];
        const format = header.FormatData ? header.FormatData : "{0:n0}";
        const isNumber = this.RowData.Data.some(x => typeof x[header.FieldName] === 'number');
        const sum = this.RowData.Data.reduce((acc, x) => {
            const val = x[header.FieldName];
            return acc + (val ? parseFloat(val) : 0);
        }, 0);
        cellVal.textContent = Utils.FormatEntity(format, isNumber ? sum : this.RowData.Data.length);
    }

    ResetSummaryRow(tr, colSpan) {
        for (let i = 1; i < colSpan; i++) {
            if (tr.cells[0]) {
                tr.cells[0].remove();
            }
        }
        this.ClearSummaryContent(tr);
    }

    ClearSummaryContent(tr) {
        Array.from(tr.cells).forEach(cell => {
            cell.innerHTML = '';
        });
    }

    // @ts-ignore
    async RowChangeHandler(rowData, rowSection, observableArgs, component = null) {
        const com = ['SearchEntry'];
        if (rowSection.EmptyRow && observableArgs.EvType === EventType.Change) {
            await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforeCreated, rowData, this);
            let rs;
            if (this.Meta.IsRealtime && !rowSection.Focused()) {
                var entity = rowData;
                await rowSection.PatchUpdateOrCreate();
            } else {
                rs = rowSection.Entity;
                this.Dirty = true;
            }
            if (this.Meta.ComponentType !== 'VirtualGrid') {
                this.Entity.SetComplexPropValue(this.FieldName, this.RowData.Data);
            }
            if (rowSection.EmptyRow) {
                rowSection.EmptyRow = false;
                this.MoveEmptyRow(rowSection);
                this.EmptySection.Children.Clear();
                this.AddNewEmptyRow();
            }
            if (!com.includes(component?.Meta.ComponentType)) {
                this.ClearSelected();
                rowSection.Selected = true;
                rowSection.Focus();
                this.LastListViewItem = rowSection;
                this.LastElementFocus.Focus();
            }
            await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterCreated, rowData);
        }
        if (component && component.ComponentType === 'GridView') {
            await this.DispatchEvent(component.Meta.Events, observableArgs.EvType, rowData, rowSection);
        }
        await this.DispatchEvent(this.Meta.Events, observableArgs.EvType, rowData, rowSection);
        if (observableArgs.EvType === EventType.Change) {
            this.PopulateFields();
            this.RenderIndex();
            if (this.Meta.IsSumary) {
                this.AddSummaries();
            }
            this.LastListViewItem = rowSection;
            let headers = this.Header.filter(y => y.Editable);
            let currentComponent = headers.find(y => y.FieldName === component?.FieldName);
            if (com.includes(currentComponent?.ComponentType) && rowData[currentComponent.FieldName] != null) {
                let index = headers.indexOf(currentComponent);
                if (headers.length > index + 1) {
                    let nextGrid = headers[index + 1];
                    let nextComponent = rowSection.Children.find(y => y?.FieldName === nextGrid.FieldName);
                    this.ClearSelected();
                    rowSection.Selected = true;
                    rowSection.Focus();
                    this.LastListViewItem = rowSection;
                    nextComponent.Focus();
                }
            }
        }
    }

    MoveEmptyRow(rowSection) {
        if (this.RowData.Data.includes(rowSection.Entity)) {
            return;
        }
        if (this.Meta.TopEmpty) {
            this.RowData.Data.unshift(rowSection.Entity);
            if (!this.MainSection.Children.includes(this.EmptySection.FirstChild)) {
                this.MainSection.Children.unshift(this.EmptySection.FirstChild);
            }
            this.MainSection.Element.prepend(this.EmptySection.Element.firstElementChild);
        } else {
            this.RowData.Data.push(rowSection.Entity);
            this.MainSection.Element.appendChild(this.EmptySection.Element.firstElementChild);
            if (!this.MainSection.Children.includes(this.EmptySection.FirstChild)) {
                this.MainSection.Children.push(this.EmptySection.FirstChild);
            }
        }
        if (this.Meta.IsRealtime) {
            rowSection.Element.classList.remove("new-row");
        }
        rowSection.Parent = this.MainSection;
        rowSection.ListViewSection = this.MainSection;
    }
    ProcessMetaData(ds, rowCount) {
        // const total = ds.length > 1 && ds[1].length > 0 ? ds[1][0]["total"] : null;
        // const headers = ds.length > 2 ? ds[2].map(x => this.CastProp(x)) : null;
        // this.Settings = ds.length > 3 && ds[3].length > 0 ? this.As(ds[3][0], UserSetting) : null;
        // this.FilterColumns(this.MergeComponent(headers, this.Settings));
        // this.RenderTableHeader(this.Header);
        // if (this.Paginator !== null) {
        //     this.Paginator.Options.Total = total ?? rowCount;
        // }
    }

    RenderTableHeader(headers) {
        if (!headers || headers.length === 0) {
            headers = this.Header;
        }
        if (!this.HeaderSection.Element) {
            this.AddSections();
        }
        headers.forEach((x, index) => x.PostOrder = index);
        this.HeaderSection.DisposeChildren();
        const anyGroup = headers.some(x => x.GroupName);
        
        const headerRow = document.createElement('tr');
        headers.forEach((header, index) => {
            if (anyGroup && header.GroupName) {
                if (header !== headers.find(x => x.GroupName === header.GroupName)) {
                    return;
                }
                const th = document.createElement('th');
                th.setAttribute('colspan', headers.filter(x => x.GroupName === header.GroupName).length.toString());
                th.innerHTML = header.GroupName;
                headerRow.appendChild(th);
                return;
            }
            const th = document.createElement('th');
            th.tabIndex = -1;
            th.dataset.field = header.FieldName;
            th.dataset.id = header.Id;
            th.style.width = header.AutoFit ? 'auto' : header.Width;
            th.style.minWidth = header.MinWidth;
            th.style.maxWidth = header.MaxWidth;
            th.style.textAlign = header.TextAlignEnum || 'center';
            th.innerHTML = header.Label;
            th.ondblclick = () => this.EditForm.ComponentProperties(header);
            th.oncontextmenu = e => this.HeaderContextMenu(e, header);
            th.onfocusout = e => this.FocusOutHeader(e, header);
            th.onkeydown = e => this.ThHotKeyHandler(e, header);
            if (header.StatusBar) {
                const icon = document.createElement('i');
                icon.className = 'fa fa-edit';
                icon.onclick = () => this.ToggleAll();
                th.appendChild(icon);
            }
            if (header.Icon) {
                const icon = document.createElement('i');
                icon.className = header.Icon;
                th.appendChild(icon);
            }
            if (header.Description) {
                th.title = header.Description;
            }
            if (this.Client.SystemRole) {
                th.setAttribute('contenteditable', 'true');
                th.oninput = e => this.ChangeHeader(e, header);
            }
            headerRow.appendChild(th);
        });
        this.HeaderSection.Element.appendChild(headerRow);

        if (anyGroup) {
            const groupRow = document.createElement('tr');
            headers.forEach(header => {
                if (header.GroupName) {
                    const th = document.createElement('th');
                    th.dataset.field = header.FieldName;
                    th.style.width = header.Width;
                    th.style.minWidth = header.MinWidth;
                    th.style.maxWidth = header.MaxWidth;
                    th.style.textAlign = header.TextAlignEnum || 'center';
                    th.innerHTML = header.Label;
                    th.oncontextmenu = e => this.HeaderContextMenu(e, header);
                    groupRow.appendChild(th);
                }
            });
            this.HeaderSection.Element.appendChild(groupRow);
        }
        this.HeaderSection.Children.sort((a, b) => a.Meta.PostOrder - b.Meta.PostOrder);
        if (!this.Meta.Focus) {
            this.ColumnResizeHandler();
        }
    }

    ChangeHeader(e, header) {
        clearTimeout(this._imeout);
        this._imeout = setTimeout(() => {
            let html = e.target;
            let patchVM = {
                Table: "Component",
                Changes: [
                    { Field: "Component.Id", Value: header.Id, OldVal: header.Id },
                    { Field: "Component.Label", Value: html.textContent.trim(), OldVal: header.Label }
                ]
            };
            Client.Instance.PatchAsync(patchVM);
        }, 1000);
    }

    async CustomQuery(vm) {
        try {
            const ds = await Client.Instance.ComQuery(vm);
            if (!ds || ds.length === 0) {
                this.SetRowData(null);
                return null;
            }
            let total = ds.length > 1 ? ds[1][0].total : ds[0].length;
            if (ds.length >= 3) {
                this.ProcessMetaData(ds, total);
            }
            let rows = [...ds[0]];
            this.ClearRowData();
            this.SetRowData(rows);
            this.UpdatePagination(total, rows.length);
            return rows;
        } catch (err) {
            console.error(err);
            throw err;
        }
    }

    ToggleAll() {
        let anySelected = this.AllListViewItem.some(x => x.Selected);
        if (anySelected) {
            this.ClearSelected();
            return;
        }
        this.RowAction(x => {
            if (x.EmptyRow) return;
            x.Selected = true;
        });
    }

    HeaderContextMenu(e, header) {
        e.preventDefault();
        e.stopPropagation();
        const editForm = this.FindClosest('EditForm');
        const section = this.FindClosest('Section');
        const menu = ContextMenu.Instance;
        menu.Top = e.clientY; // Adjusted for typical web usage
        menu.Left = e.clientX; // Adjusted for typical web usage

        menu.MenuItems = [
            {
                Icon: "fal fa-eye", Text: "Hiện tiêu đề", Click: () => this.ShowWidth(header, e),
                Ele: undefined,
                Style: '',
                Disabled: false,
                Parameter: undefined,
                MenuItems: []
            },
            {
                Icon: "fal fa-eye-slash", Text: "Ẩn tiêu đề", Click: () => this.HideWidth(header, e),
                Ele: undefined,
                Style: '',
                Disabled: false,
                Parameter: undefined,
                MenuItems: []
            },
            {
                Icon: header.Frozen ? "fal fa-snowflakes" : "fal fa-snowflake", Text: header.Frozen ? "Hủy định cột" : "Cố định cột", Click: () => this.FrozenColumn(header, e),
                Ele: undefined,
                Style: '',
                Disabled: false,
                Parameter: undefined,
                MenuItems: []
            },
        ];

        if (Client.SystemRole) {
            menu.MenuItems.push(
                {
                    Icon: "fal fa-wrench", Text: "Tùy chọn cột dữ liệu", Click: () => editForm.ComponentProperties(header),
                    Ele: undefined,
                    Style: '',
                    Disabled: false,
                    Parameter: undefined,
                    MenuItems: []
                },
                {   Icon: "fal fa-clone", Text: "Clone cột", Click: () => this.CloneHeader(header),
                    Ele: undefined,
                    Style: '',
                    Disabled: false,
                    Parameter: undefined,
                    MenuItems: [] 
                },
                {   Icon: "fal fa-trash-alt", Text: "Xóa cột", Click: () => this.RemoveHeader(header),
                    Ele: undefined,
                    Style: '',
                    Disabled: false,
                    Parameter: undefined,
                    MenuItems: [] 
                 },
                {   Icon: "fal fa-cog", Text: "Tùy chọn bảng dữ liệu", Click: () => editForm.ComponentProperties(this.Meta),
                    Ele: undefined,
                    Style: '',
                    Disabled: false,
                    Parameter: undefined,
                    MenuItems: [] 
                 },
                {   Icon: "fal fa-cogs", Text: "Tùy chọn vùng dữ liệu", Click: () => editForm.SectionProperties(section.Meta),
                    Ele: undefined,
                    Style: '',
                    Disabled: false,
                    Parameter: undefined,
                    MenuItems: [] 
                },
                {   Icon: "fal fa-folder-open", Text: "Thiết lập chung", Click: () => editForm.FeatureProperties(editForm.Feature),
                    Ele: undefined,
                    Style: '',
                    Disabled: false,
                    Parameter: undefined,
                    MenuItems: [] 
                }
            );
        }
        menu.Render();
    }

    HideWidth(header, e) {
        const targetElement = e.target.closest('th');
        if (targetElement) {
            targetElement.style.minWidth = "";
            targetElement.style.maxWidth = "";
            targetElement.style.width = "";
        }
        this.UpdateHeaders(this.GetHeaderSettings());
    }

    GetHeaderSettings() {
        const headerElement = {};
        this.HeaderSection.Children.filter(x => x.Meta?.Id != null).forEach(x => {
            headerElement[x.Meta.Id] = x;
        });

        const ele = Array.from(this.HeaderSection.Element.firstElementChild.children);
        this.HeaderSection.Children.forEach(x => {
            x.Meta.Order = ele.indexOf(x.Element);
        });

        const columns = this.Header.filter(x => x.Id != null).map(x => {
            const match = headerElement[x.Id];
            if (!match) return null;
            x.Width = `${match.Element.offsetWidth}px`;
            x.MaxWidth = `${match.Element.offsetWidth}px`;
            x.MinWidth = `${match.Element.offsetWidth}px`;
            return x;
        }).filter(x => x != null);

        // @ts-ignore
        return columns.sort((a, b) => (a.Frozen - b.Frozen) || (a.Order - b.Order));
    }

    ShowWidth(arg) {
        const entity = arg.header;
        const e = arg.events;
        if (e.target.firstChild && !e.target.firstChild.length) {
            e.target.prepend(document.createTextNode(entity.ShortDesc));
        }
        e.target.style.minWidth = "";
        e.target.style.maxWidth = "";
        e.target.style.width = "";

        this.UpdateHeaders(this.GetHeaderSettings());
    }

    FrozenColumn(arg) {
        const entity = arg.header;
        const header = this.Header.find(x => x.Id === entity.Id);
        if (header) {
            header.Frozen = !header.Frozen;
        }
        this.UpdateHeaders(this.GetHeaderSettings());
    }

    CloneHeader(arg) {
        {
            var entity = arg;
            var confirm = new ConfirmDialog
            confirm.Content = "Bạn có chắc chắn muốn clone cột này không?";
            confirm.Render();
            confirm.YesConfirmed += () =>
            {
                var cloned = entity.Clone();
                cloned.Id = Uuid7.Id25();
                var patch = cloned.MapToPatch();
                Client.Instance.PatchAsync(patch).then(success =>
                {
                    if (success == 0)
                    {
                        Toast.Warning("Clone error");
                        return;
                    }
                    this.Header.push(cloned);
                    // @ts-ignore
                    this.Header = this.Header.sort((a, b) => b.Frozen - a.Frozen || b.ComponentType === "Button" - a.ComponentType === "Button" || a.Order - b.Order);
                    this.Rerender();
                    Toast.Success("Clone success");
                }).catch(e =>
                {
                    Toast.Warning("Clone header NOT success");
                });
            };
        }
    }

    RemoveHeader(arg) {
        var entity = arg;
        var confirm = new ConfirmDialog
        confirm.Content = "Bạn có chắc chắn muốn clone cột này không?";
        confirm.Render();
        confirm.YesConfirmed += () =>
        {
            const ids = [entity.Id];
            Client.Instance.HardDeleteAsync(ids, 'Component', this.MetaConn, this.MetaConn)
            .then(success =>
            {
                if (!success)
                {
                    Toast.Warning("delete error");
                    return;
                }
                Toast.Success("Delete success");
                this.Header.Remove(entity);
                this.Rerender();
            });
        };
    }

    RemoveRowById(id) {
        super.RemoveRowById(id);
        this.RenderIndex();
    }

    RemoveRow(row)
    {
        super.RemoveRow(row);
        this.RenderIndex();
    }

    HardDeleteConfirmed(deleted) {
        return new Promise((resolve, reject) => {
            super.HardDeleteConfirmed(deleted).then(res => {
                this.RenderIndex();
                if (this.Meta.IsSumary) {
                    this.AddSummaries();
                }
                resolve(res);
            }).catch(err => reject(err));
        });
    }

    UpdateView(force = false, dirty = null, componentNames = []) {
        if (!this.Editable && !this.Meta.CanCache) {
            if (force) {
                this.DisposeNoRecord();
                this.ListViewSearch.RefreshListView();
            }
        } else {
            // @ts-ignore
            this.RowAction(row => !row.EmptyRow, row => row.UpdateView(force, dirty, componentNames));
        }
    }

    async RowChangeHandlerGrid(rowData, rowSection, observableArgs, component = null) {
        await new Promise(resolve => setTimeout(resolve, this.CellCountNoSticky));
        if (rowSection.EmptyRow && observableArgs.EvType === EventType.Change) {
            await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforeCreated, rowData);
            rowSection.EmptyRow = false;
            this.MoveEmptyRow(rowSection);
            const headers = this.Header.filter(y => y.Editable);
            const currentComponent = headers.find(y => y.FieldName === component.FieldName);
            const index = headers.indexOf(currentComponent);
            if (headers.length > index + 1) {
                const nextGrid = headers[index + 1];
                const nextComponent = rowSection.Children.find(y => y.FieldName === nextGrid.FieldName);
                if (nextComponent) {
                    nextComponent.Focus();
                }
            }
            this.EmptySection.Children = [];
            this.AddNewEmptyRow();
            this.Entity.SetComplexPropValue(this.FieldName, this.RowData.Data);
            await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterCreated, rowData);
        }
        this.AddSummaries();
        this.PopulateFields();
        this.RenderIndex();
        await this.DispatchEvent(this.Meta.Events, EventType.Change, rowData);
    }

    GetViewPortItem() {
        if (!this.Element || !this.Element.classList.contains('sticky')) {
            return this.RowData.Data.length;
        }
        let mainSectionHeight = this.Element.clientHeight
            - (this.HeaderSection.Element ? this.HeaderSection.Element.clientHeight : 0)
            - this.Paginator.Element.clientHeight
            - this._theadTable;

        this.Header = this.Header.filter(x => x != null);

        if (this.Header.some(x => x.Summary && x.Summary.trim() !== "")) {
            mainSectionHeight -= this._tfooterTable;
        }
        if (this.Meta.CanAdd) {
            mainSectionHeight -= this._rowHeight;
        }
        return this.GetRowCountByHeight(mainSectionHeight);
    }
}
