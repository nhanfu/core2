import { ListView } from './listView.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { ValidationRule } from "./models/validationRule.js";
import { LangSelect } from "./utils/langSelect.js";
import { Client } from "./clients/client.js";
import EventType from './models/eventType.js';
import { ComponentType } from './models/componentType.js';
import { Str } from './utils/ext.js';
import ObservableArgs from './models/observable.js';
import { Action } from "./models/action.js";
import "./utils/fix.js";
import { Spinner } from './spinner.js';


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
        this.DataTable = null;

        GridView.ToolbarColumn = {
            StatusBar: true,
            Label: '',
            Frozen: true
        };

        this.DOMContentLoaded = this.DOMContentLoadedHandler.bind(this);
    }

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
                        where = this.buildNumberCondition(cell, hl, isNull);
                        break;
                    case 'Checkbox':
                        where = this.buildCheckboxCondition(cell, hl, isNull);
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


}
