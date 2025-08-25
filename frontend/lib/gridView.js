import { Toast } from './toast.js';
import { Spinner } from './spinner.js';
import { Utils } from "./utils/utils.js";
import { ListView } from './listView.js';
import { Uuid7 } from './structs/uuidv7.js';
import { ContextMenu } from './contextMenu.js';
import { Client } from "./clients/";
import { SearchEntry } from './searchEntry.js';
import { EditableComponent, LangSelect } from './index.js';
import {
    EventType, ElementType, HotKeyModel, CustomEventType, Component,
    OperatorEnum, KeyCodeEnum, OrderbyDirection, AdvSearchOperation, LogicOperation
} from './models/';
import { SearchMethodEnum } from './models/enum.js';
import { GridViewItem } from './gridViewItem.js';
import { ConfirmDialog } from './confirmDialog.js';
import { Direction, Html } from "./utils/html.js";
import { Section } from './section.js';
import { ListViewSection } from './listViewSection.js';
import { ListViewSearch, ListViewSearchVM } from './listViewSearch.js';
import { ComponentFactory } from './utils/componentFactory.js';
import Decimal from 'decimal.js';
import { GroupViewItem } from './groupViewItem.js';
import { UserSetting } from './models/userSeting.js';
import { Textbox } from './textbox.js';
import { Datepicker } from './datepicker.js';
import { Select } from './select.js';
import Sortable from 'sortablejs';

export class GridView extends ListView {

    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {HTMLElement | null} ele 
     */
    constructor(ui, el) {
        super(ui, el);
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
        this.DOMContentLoaded.add(this.DOMContentLoadedHandler.bind(this));
    }

    static ToolbarColumn = {
        StatusBar: true,
        Label: '',
        Frozen: true
    };
    /** @type {HTMLElement} */
    MenuGridView;
    /** @type {GridViewItem} */
    GridViewItemEmpty;
    DOMContentLoadedHandler() {
        if (this.Meta.IsSumary) {
            this.AddSummaries();
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
        this.UpdateStickyColumns();
    }
    AddSections() {
        if (this.HeaderSection && this.HeaderSection.Element != null) {
            return;
        }
        Html.Take(this.ParentElement);
        Html.Instance.Div.Style(this.Meta.ChildStyle).ClassName("grid-wrapper").ClassName(this.Meta.ClassName).ClassName(this.Editable ? "editable" : "");
        this.Element = Html.Context;
        Html.Instance.Div.ClassName("d-grid").Style("grid-template-columns: repeat(12, 1fr)").Div.Style("grid-area: span 1 / span 10;").ClassName("grid-toolbar search").End.Render();
        Html.Instance.Div.Style("grid-area: span 1 / span 2;").ClassName("button-toolbar").Render();
        this.MenuGridView = Html.Context;
        Html.Instance.End.Render();
        var child = (this.EditForm.Meta.Components || []).filter(x => x.ComponentGroupId == this.Meta.Id && (x.ComponentType == "Button" || x.ComponentType == "ImportExcel"));
        child.forEach(ui => {
            const com = ComponentFactory.GetComponent(ui, this.EditForm);
            if (com == null) return;
            com.ParentElement = this.MenuGridView;
            this.Parent.AddChild(com);
            this.EditForm.ChildCom.push(com);
            com.Disabled = ui.Disabled || this.EditForm.Disabled || com.Disabled;
            if (ui.Focus) {
                com.Focus();
            }
            if (Client.SystemRole) {
                com.Element.addEventListener('contextmenu', e => {
                    e.preventDefault();
                    e.stopPropagation();
                    this.EditForm.SysConfigMenu(e, ui, null, com)
                });
            }
        });
        this.ListViewSearch = new ListViewSearch(this.Meta);
        this.ListViewSearch.Entity = new ListViewSearchVM();
        if (this.Meta.DefaultAddStart) {
            let pre = this.Meta.DefaultAddStart;
            this.ListViewSearch.EntityVM.StartDate = new Date(Date.now() + pre * 24 * 3600 * 1000);
        }
        let lFrom = window.localStorage.getItem("FromDate" + this.Meta.Id);
        if (lFrom != null) {
            this.ListViewSearch.EntityVM.StartDate = new Date(lFrom);
        }
        if (this.Meta.DefaultAddEnd) {
            let pre = this.Meta.DefaultAddEnd;
            this.ListViewSearch.EntityVM.EndDate = new Date(Date.now() + pre * 24 * 3600 * 1000);
        }
        let lTo = window.localStorage.getItem("ToDate" + this.Meta.Id);
        if (lTo != null) {
            this.ListViewSearch.EntityVM.EndDate = new Date(lTo);
        }
        this.AddChild(this.ListViewSearch);
        Html.Take(this.Element).Div.ClassName("table-wrapper").Table.ClassName("table").Event('keydown', e => this.HotKeyF6Handler(e, e.KeyCodeEnum()))
        this.DataTable = Html.Context;
        Html.Instance.Thead.TabIndex(-1).ClassName("tb-header").Render();
        this.HeaderSection = new ListViewSection(null, Html.Context);
        this.HeaderSection.ParentElement = this.DataTable;
        this.AddChild(this.HeaderSection);
        Html.Take(this.DataTable).Thead.ClassName("tb-search").Render();
        this.SearchSection = new ListViewSection(null, Html.Context);
        this.SearchSection.Entity = {
            Id: Uuid7.Guid()
        }
        this.SearchSection.ParentElement = this.DataTable;
        this.AddChild(this.SearchSection);
        Html.Take(this.DataTable).TBody.ClassName("tb-empty").Render();
        this.EmptySection = new ListViewSection(null, Html.Context);
        this.EmptySection.ParentElement = this.DataTable;
        this.AddChild(this.EmptySection);
        Html.Take(this.DataTable).TBody.ClassName("tb-body").Render();
        this.MainSection = new ListViewSection(null, Html.Context);
        this.MainSection.ParentElement = this.DataTable;
        this.AddChild(this.MainSection);
        if (this.Meta.ShowHotKey) {
            var seft = this;
            new Sortable(this.MainSection.Element, {
                animation: 150,
                ghostClass: 'blue-background-class',
                handle: '.status-bar',
                autoScroll: true,
                scrollSensitivity: 30,
                scrollSpeed: 10,
                onEnd: async function (evt) {
                    await seft.RenderIndex2();
                }
            });
        }
        if (!this.AddContentRendered) {
            this.MainSection.Element.addEventListener('contextmenu', this.BodyContextMenuHandler.bind(this));
            this.EmptySection.Element.addEventListener('contextmenu', this.BodyContextMenuHandler.bind(this));
            this.AddContentRendered = true;
        }
        Html.Take(this.DataTable).TFooter.ClassName("tb-footer").Render();
        this.FooterSection = new ListViewSection(null, Html.Context);
        this.FooterSection.ParentElement = this.DataTable;
        this.AddChild(this.FooterSection);
        Html.Instance.EndOf(".table-wrapper");
        Html.Take(this.Element);
        this.RenderPaginator();
    }

    ClickHeader(e, header) {
        e.preventDefault();
        e.stopPropagation();
        if (this.EditForm.DevToolsElement) {
            this.EditForm.UpdateMetaData(header);
        }
        let index = this.LastNumClick;
        const table = this.DataTable;
        if (this.LastNumClick != null) {
            table.querySelectorAll('tr:not(.summary)').forEach(function (row) {
                if (row.hasAttribute('virtualrow') || row.classList.contains('group-row')) {
                    return;
                }
                /** @type {HTMLElement[]} */
                const cells = Array.from(row.querySelectorAll('th, td'));
                if (cells[index]) {
                    cells[index].style.removeProperty("background-color");
                    cells[index].style.removeProperty("color");
                }
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
        table.querySelectorAll('tr:not(.summary)').forEach(function (row) {
            if (row.hasAttribute('virtualrow') || row.classList.contains('group-row')) {
                return;
            }
            /** @type {HTMLElement[]} */
            const cells = Array.from(row.querySelectorAll('th, td'));
            if (cells[index]) {
                cells[index].style.backgroundColor = "#cbdcc2";
                cells[index].style.color = "#000";
            }
        });
    }

    FocusOutHeader(e, header) {
        let index = this.LastNumClick;
        const table = this.DataTable;
        if (this.LastNumClick !== null) {
            table.querySelectorAll('tr:not(.summary)').forEach(function (row) {
                if (row.hasAttribute('virtualrow') || row.classList.contains('group-row')) {
                    return;
                }
                const cells = Array.from(row.querySelectorAll('th, td'));
                // @ts-ignore
                cells[index].style.removeProperty("background-color");
                // @ts-ignore
                cells[index].style.removeProperty("color");
            });
        }
    }

    ThHotKeyHandler(e, header) {
        var hasGroup = this.Header.some(x => !Utils.isNullOrWhiteSpace(x.GroupName));
        if (this.Meta.Focus || hasGroup) {
            return;
        }
        const keyCode = e.keyCode;
        if (keyCode === 39) {
            if (!Utils.isNullOrWhiteSpace(header.GroupName)) {
                return;
            }
            e.stopPropagation();
            const th = e.target.closest("th");
            const tr = Array.from(th.parentElement.querySelectorAll("th"));
            const index = tr.findIndex(x => x === th);
            th.parentElement.parentElement.parentElement.querySelectorAll('tr').forEach(function (row) {
                if (row.hasAttribute('virtualrow') || row.classList.contains('group-row')) {
                    return;
                }
                const cells = Array.from(row.querySelectorAll('th, td'));
                if (cells[0].classList.contains('summary-header')) {
                    return;
                }
                var draggingColumnIndex = index;
                var endColumnIndex = index + 1;
                if (draggingColumnIndex > endColumnIndex) {
                    cells[endColumnIndex].parentNode.insertBefore(cells[draggingColumnIndex], cells[endColumnIndex]);
                } else {
                    cells[endColumnIndex].parentNode.insertBefore(cells[draggingColumnIndex], cells[endColumnIndex].nextSibling);
                }
                cells[draggingColumnIndex].style.backgroundColor = "#cbdcc2";
            });
            this.SwapHeader(index, index + 1);
            this.SwapSectionHeader(index, index + 1);
            this.UpdateHeaders();
            th.focus();
            this.UpdateStickyColumns();
        }
        else if (keyCode === 37) { // Left arrow
            if (!Utils.isNullOrWhiteSpace(header.GroupName)) {
                return;
            }
            e.stopPropagation();
            const th1 = e.target.closest("th");
            const tr1 = Array.from(th1.parentElement.querySelectorAll("th"));
            const index1 = tr1.findIndex(x => x === th1);

            th1.parentElement.parentElement.parentElement.querySelectorAll('tr').forEach(function (row) {
                if (row.hasAttribute('virtualrow') || row.classList.contains('group-row')) {
                    return;
                }
                const cells = Array.from(row.querySelectorAll('th, td'));
                if (cells[0].classList.contains('summary-header')) {
                    return;
                }
                var draggingColumnIndex = index1;
                var endColumnIndex = index1 - 1;
                if (draggingColumnIndex > endColumnIndex) {
                    cells[endColumnIndex].parentNode.insertBefore(cells[draggingColumnIndex], cells[endColumnIndex]);
                } else {
                    cells[endColumnIndex].parentNode.insertBefore(cells[draggingColumnIndex], cells[endColumnIndex].nextSibling);
                }
                cells[draggingColumnIndex].style.backgroundColor = "#cbdcc2";
            });
            this.SwapHeader(index1, index1 - 1);
            this.SwapSectionHeader(index1, index1 - 1);
            this.UpdateHeaders();
            th1.focus();
            this.UpdateStickyColumns();
        }
    }

    SwapHeader(oldIndex, newIndex) {
        const item = this.Header[oldIndex];
        this.Header.splice(oldIndex, 1);
        this.Header.splice(newIndex, 0, item);
    }

    SwapSectionHeader(oldIndex, newIndex) {
        const item = this.HeaderSection.Children[oldIndex];
        this.HeaderSection.Children.splice(oldIndex, 1);
        this.HeaderSection.Children.splice(newIndex, 0, item);
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
    /**
     * 
     * @param {MouseEvent} e 
     * @returns 
     */
    ActionKeyHandler(e, header, focusedRow, com, el, keyCode) {
        let fieldName = "";
        if ([KeyCodeEnum.F8, KeyCodeEnum.UpArrow, KeyCodeEnum.DownArrow, KeyCodeEnum.LeftArrow, KeyCodeEnum.RightArrow].includes(keyCode)) {
            e.preventDefault();
            e.stopPropagation();
            if (!com) {
                return;
            }
            fieldName = header.FieldName;
        }
        switch (keyCode) {
            case KeyCodeEnum.F8:
                this.HardDeleteSelected();
                break;
            case KeyCodeEnum.U:
                if (e.ctrlKey && this.Meta.CanAdd) {
                    e.preventDefault();
                    e.stopPropagation();
                    this.DuplicateSelected();
                }
                break;
            case KeyCodeEnum.R:
                if (e.ctrlKey) {
                    e.preventDefault();
                    e.stopPropagation();
                    this.ActionFilter();
                }
                break;
            case KeyCodeEnum.UpArrow:
                this.MoveFocusUp(fieldName);
                break;
            case KeyCodeEnum.DownArrow:
                this.MoveFocusDown(fieldName);
                break;
            case KeyCodeEnum.LeftArrow:
                this.MoveFocusLeft(com);
                break;
            case KeyCodeEnum.RightArrow:
                this.MoveFocusRight(com);
                break;
            case KeyCodeEnum.C:
                if (e.ctrlKey) {
                    const selected = Array.from(this.Element.querySelectorAll('td.cell-matrix'));
                    if (selected.length === 0) return;

                    const selectedData = selected.map(td => ({
                        row: parseInt(td.dataset.row),
                        col: parseInt(td.dataset.col),
                        value: td.innerText
                    }));

                    const rows = selectedData.map(cell => cell.row);
                    const cols = selectedData.map(cell => cell.col);
                    const minRow = Math.min(...rows);
                    const maxRow = Math.max(...rows);
                    const minCol = Math.min(...cols);
                    const maxCol = Math.max(...cols);

                    let result = '';
                    for (let i = minRow; i <= maxRow; i++) {
                        let row = [];
                        for (let j = minCol; j <= maxCol; j++) {
                            const cell = this.Matrix[i][j];
                            row.push(cell && cell.classList.contains('cell-matrix') ? (cell.querySelector("input") ? cell.querySelector("input").value : cell.innerText) : '');
                        }
                        result += row.join('\t') + '\n';
                    }
                    if (navigator.clipboard) {
                        navigator.clipboard.writeText(result.trim()).catch(err => {
                            console.error('Không thể copy vào clipboard:', err);
                        });
                    }
                }
                break;
            default:
                break;
        }
    }

    GetHeaderMatrix(tableElement) {
        const theadRows = tableElement.querySelectorAll('.tb-header tr');
        const matrix = [];
        let maxCols = 0;

        theadRows.forEach((tr, rowIndex) => {
            matrix[rowIndex] = matrix[rowIndex] || [];
            let colIndex = 0;

            [...tr.children].forEach(th => {
                // Skip filled cells
                while (matrix[rowIndex][colIndex]) {
                    colIndex++;
                }

                const rowspan = parseInt(th.getAttribute('rowspan') || '1');
                const colspan = parseInt(th.getAttribute('colspan') || '1');

                for (let i = 0; i < rowspan; i++) {
                    for (let j = 0; j < colspan; j++) {
                        const r = rowIndex + i;
                        const c = colIndex + j;
                        matrix[r] = matrix[r] || [];
                        matrix[r][c] = i === 0 && j === 0 ? th.innerText : '';
                    }
                }

                colIndex += colspan;
                if (colIndex > maxCols) maxCols = colIndex;
            });
        });

        // Gộp lại theo cột cuối cùng
        const finalHeader = [];
        for (let col = 0; col < maxCols; col++) {
            let parts = [];
            for (let row = 0; row < matrix.length; row++) {
                const val = matrix[row][col];
                if (val) parts.push(val.trim());
            }
            finalHeader.push(parts.join(' - ')); // Join theo cấp độ
        }

        return finalHeader;
    }

    MoveFocusUp(fieldName) {
        let currentItem = this.GetItemFocus();
        if (!currentItem) {
            currentItem = this.GridViewItemEmpty;
        }
        if (!(this.Meta.IsMultiple && this.ComponentType == "GridView")) {
            currentItem.Selected = false;
        }
        var nextIndex = -2;
        if (currentItem.EmptyRow) {
            nextIndex = this.AllListViewItem.length - 1;
        }
        else {
            var indexCurrent = this.AllListViewItem.indexOf(currentItem);
            nextIndex = indexCurrent - 1;
            if (this.AllListViewItem[nextIndex] && this.AllListViewItem[nextIndex].GroupRow) {
                nextIndex = nextIndex - 1;
            }
            if (indexCurrent == 0) {
                if (this.Meta.CanAdd) {
                    nextIndex = -2;
                }
                else {
                    nextIndex = this.AllListViewItem.length - 1;
                }
            }
            else {
                if (nextIndex == -1) {
                    if (this.Meta.CanAdd) {
                        nextIndex = -2;
                    }
                    else {
                        nextIndex = this.AllListViewItem.length - 1;
                    }
                }
            }
        }
        if (nextIndex == -2) {
            this.GridViewItemEmpty.Focused = true;
            var com = this.GridViewItemEmpty.Children.find(x => x.Meta.FieldName == fieldName);
            com.ParentElement.focus();
            com.Focus();
            if (com.Element.tagName === 'INPUT' || com.Element.tagName === 'TEXTAREA') {
                com.Element.select();
            }
        }
        else {
            this.AllListViewItem[nextIndex].Focused = true
            if (!(this.Meta.IsMultiple && this.ComponentType == "GridView")) {
                this.AllListViewItem[nextIndex].Selected = true;
            }
            var com = this.AllListViewItem[nextIndex].Children.find(x => x.Meta.FieldName == fieldName);
            com.ParentElement.focus();
            com.Focus();
            if (com.Element.tagName === 'INPUT' || com.Element.tagName === 'TEXTAREA') {
                com.Element.select();
            }
        }
    }

    MoveFocusDown(fieldName) {
        let currentItem = this.GetItemFocus();
        if (!currentItem) {
            currentItem = this.GridViewItemEmpty;
        }
        if (!(this.Meta.IsMultiple && this.ComponentType == "GridView")) {
            currentItem.Selected = false;
        }
        var nextIndex = -2;
        if (currentItem.EmptyRow) {
            nextIndex = 0;
            if (this.AllListViewItem[nextIndex] && this.AllListViewItem[nextIndex].GroupRow) {
                nextIndex = nextIndex + 1;
            }
        }
        else {
            var indexCurrent = this.AllListViewItem.indexOf(currentItem);
            nextIndex = indexCurrent + 1;
            if (this.AllListViewItem[nextIndex] && this.AllListViewItem[nextIndex].GroupRow) {
                nextIndex = nextIndex + 1;
            }
            if (indexCurrent == this.AllListViewItem.length - 1) {
                if (this.Meta.CanAdd) {
                    nextIndex = -2;
                }
            }
            else {
                if (!this.AllListViewItem[nextIndex]) {
                    if (this.Meta.CanAdd) {
                        nextIndex = -2;
                    }
                    else {
                        nextIndex = 0;
                    }
                }
            }
        }
        if (nextIndex == -2) {
            this.GridViewItemEmpty.Focused = true;
            var com = this.GridViewItemEmpty.Children.find(x => x.Meta.FieldName == fieldName);
            com.ParentElement.focus();
            com.Focus();
            if (com.Element.tagName === 'INPUT' || com.Element.tagName === 'TEXTAREA') {
                com.Element.select();
            }
        }
        else {
            if (!this.AllListViewItem[nextIndex]) {
                nextIndex = 0;
            }
            this.AllListViewItem[nextIndex].Focused = true;
            if (!(this.Meta.IsMultiple && this.ComponentType == "GridView")) {
                this.AllListViewItem[nextIndex].Selected = true;
            }
            var com = this.AllListViewItem[nextIndex].Children.find(x => x.Meta.FieldName == fieldName);
            com.ParentElement.focus();
            com.Focus();
            if (com.Element.tagName === 'INPUT' || com.Element.tagName === 'TEXTAREA') {
                com.Element.select();
            }
        }
    }

    MoveFocusLeft(com) {
        let currentItem = this.GetItemFocus();
        if (!currentItem) {
            currentItem = this.GridViewItemEmpty;
        }
        var ele = this.PreElement(com.Element.closest('td').previousElementSibling);
        let leftItem = currentItem.Children.find(x => x.Element.closest('td') === ele);
        if (!leftItem) {
            return;
        }
        leftItem.Element.closest("td").focus();
        leftItem.Focus();
        if (leftItem.Meta.Editable && !leftItem.Disabled) {
            if (leftItem.Element instanceof HTMLInputElement) {
                leftItem.Element.selectionStart = 0;
                leftItem.Element.selectionEnd = leftItem.GetValueText().length;
            }
        }
    }

    NextElement(ele) {
        if (ele.style.display == "none") {
            return this.NextElement(ele.nextElementSibling);
        }
        else {
            return ele;
        }
    }

    PreElement(ele) {
        if (ele.style.display == "none") {
            return this.PreElement(ele.previousElementSibling);
        }
        else {
            return ele;
        }
    }

    MoveFocusRight(com) {
        let currentItem = this.GetItemFocus();
        if (!currentItem) {
            currentItem = this.GridViewItemEmpty;
        }
        var ele = this.NextElement(com.Element.closest('td').nextElementSibling);
        let leftItem = currentItem.Children.find(x => x.Element.closest('td') === ele);
        if (!leftItem) {
            return;
        }
        leftItem.Element.closest("td").focus();
        leftItem.Focus();
        if (leftItem.Meta.Editable) {
            if (leftItem.Element instanceof HTMLInputElement) {
                leftItem.Element.selectionStart = 0;
                leftItem.Element.selectionEnd = leftItem.GetValueText().length;
            }
        }
    }

    HotKeyF6Handler(e, keyCode) {
        let currentItem = this.GetItemFocus();
        if (!currentItem) {
            currentItem = this.GridViewItemEmpty;
        }
        switch (keyCode) {
            case KeyCodeEnum.F1:
                e.preventDefault();
                e.stopPropagation();
                this.ToggleAll();
                break;
            default:
                break;
        }
        if (!currentItem || !currentItem.Children) {
            return;
        }
        let com = currentItem.Children.find(x => x.Meta.Id === this.LastComponentFocus?.Id);
        this.ActionKeyHandler(e, this.LastComponentFocus, currentItem, com, com.Element.closest('td'), keyCode);
    }

    async AddRow(rowData, index = 0, singleAdd = true) {
        let rowSection = await super.AddRow(rowData, index, singleAdd);
        this.UpdateStickyColumns();
        this.RenderIndex();
        return rowSection;
    }
    /**
     * Adds multiple rows to the ListView.
     * @param {Array<object>} rows An array of objects to be added as rows.
     * @param {number} index The starting index to add new rows.
     * @returns {Promise<Array<ListViewItem>>} A promise that resolves to an array of ListViewItem instances.
     */
    async AddRows(rowsData) {
        let listItem = [];
        await this.LoadMasterData(rowsData);
        await Promise.all(rowsData.map(async x => {
            listItem.push(this.AddRow(x, null, false));
        }));
        await this.DispatchCustomEvent(this.Meta.Events, EventType.Change, this);
        this.RenderIndex();
        this.AddSummaries();
        this.DomLoaded();
        return listItem;
    }

    AddNewEmptyRow() {
        if (this.Meta && this.Meta.AddRowExp) {
            var addFn = Utils.IsFunction(this.Meta.AddRowExp, false, this);
            if (!addFn) {
                return;
            }
        }
        if (this.Disabled || !this.Meta.CanAdd || (this.EmptySection && this.EmptySection.Children.length > 0)) {
            return;
        }
        let emptyRowData = {};
        emptyRowData[this.IdField] = Uuid7.NewGuid();
        this.GridViewItemEmpty = this.RenderRowData(this.Header, emptyRowData, this.EmptySection, null, true);
        if (!this.Meta.TopEmpty) {
            this.DataTable.insertBefore(this.MainSection.Element, this.EmptySection.Element);
        } else {
            this.DataTable.insertBefore(this.EmptySection.Element, this.MainSection.Element);
        }
        this.GridViewItemEmpty.Children.forEach(x => x.SetRequired());
        this.DispatchCustomEvent(this.Meta.Events, 'AfterEmptyRowCreated', emptyRowData).then(() => {
            this.UpdateStickyColumns();
        });
    }

    async ApplyFilter() {
        this.DataTable.parentElement.scrollTop = 0;
        await this.ReloadData(this.cacheHeader = true);
    }

    RenderContent() {
        if (!this.LoadRerender) {
            this.Rerender();
        }
        this.AddSections();
        let viewPort = this.GetViewPortItem();
        this.FormattedRowData = this.Meta.LocalRender ? this.Meta.LocalData : this.RowData.Data;
        if (!this.FormattedRowData || this.FormattedRowData.length === 0) {
            this.MainSection.DisposeChildren();
            if (!this._hasFirstLoad) {
                this.DispatchCustomEvent(this.Meta.Events, 'FirstLoad', this).then();
                this._hasFirstLoad = true;
            }
            this.DomLoaded();
            return;
        }
        if (this.VirtualScroll && this.FormattedRowData.length > viewPort) {
            this.FormattedRowData = this.FormattedRowData.slice(0, viewPort);
        }
        if (this.MainSection.Children.length > 0) {
            if (this._hasFirstLoad) {
                this.UpdateExistRowsWrapper(false, 0, viewPort);
            }
            if (!this._hasFirstLoad) {
                this.DispatchCustomEvent(this.Meta.Events, 'FirstLoad', this).then();
                this._hasFirstLoad = true;
            }
            this.DomLoaded();
            this.RenderIndex();
            return;
        }
        this.MainSection.Show = false;
        for (let index = 0; index < this.FormattedRowData.length; index++) {
            const rowData = this.FormattedRowData[index];
            Html.Take(this.MainSection.Element);
            this.RenderRowData(this.Header, rowData, this.MainSection, index);
        }
        this.MainSection.Show = true;
        this.ContentRendered();
        this.DomLoaded();
        this.UpdateStickyColumns();
    }

    ClearSelection() {
        this.Element.querySelectorAll(".cell-matrix").forEach(x => x.classList.remove("cell-matrix"));
    }

    UpdateExistRowsWrapper(dirty, skip, viewPort) {
        if (!this._hasFirstLoad) {
            this._hasFirstLoad = true;
            return;
        }
        this.UpdateExistRows(dirty);
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
                this.RenderRowData(this.Header, newRow, this.MainSection);
            });
        } else {
            this.MainSection.Children.slice(updatedData.length).forEach(x => x.Dispose());
        }

        if (dirty !== undefined) {
            this.Dirty = dirty;
        }
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
    /**
     * @typedef {import("./groupGridView.js").GroupRowData} GroupRowData
     * @param {import("./models/component.js").Component[]} headers
     * @param {GroupRowData} row - Group row data
     * @param {ListViewSection | import("./groupViewItem.js").GroupViewItem} section
     */
    RenderRowData(headers, row, section, index = null, emptyRow = false) {
        const tbody = section.Element;
        Html.Take(tbody);
        const rowSection = new GridViewItem(ElementType.tr);
        rowSection.EmptyRow = emptyRow;
        rowSection.Entity = row;
        rowSection.ParentElement = tbody;
        rowSection.PreQueryFn = this._preQueryFn;
        rowSection.ListView = this;
        rowSection.Meta = this.Meta
        section.AddChild(rowSection, index);
        var tr = Html.Context;
        tr.tabIndex = -1;
        if (index) {
            if (index >= tr.parentElement.children.length || index < 0) {
                index = 0;
            }
            tr.parentElement.insertBefore(tr, tr.parentElement.children[index]);
        }
        if (headers.length > 0) {
            for (let index2 = 0; index2 < headers.length; index2++) {
                const header = headers[index2];
                rowSection.RenderTableCell(row, header, null, index, index2);
            }
        }
        if (emptyRow) {
            this.Children.forEach(x => x.AlwaysLogHistory = true);
        }
        rowSection.Children.forEach(x => {
            x.PrepareUpdateView();
        });
        return rowSection;
    }
    intWaitingSticky = 0;
    intWaitingSticky = 0;
    UpdateStickyColumns() {
        const stickyColumns = this.Header
            .map((item, index) => item.Frozen ? index : -1)
            .filter(index => index !== -1);
        stickyColumns.sort((a, b) => a - b);
        this.DataTable.querySelectorAll("th, td").forEach((cell) => {
            cell.classList.remove("sticky-column");
            cell.style.left = "";
            cell.classList.remove("sticky-column-right"); // reset phải
            cell.style.right = "";
        });
        let leftOffset = 0;
        stickyColumns.forEach((index) => {
            this.HeaderSection.Element.querySelectorAll(
                `tr:first-child th:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            this.SearchSection.Element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            this.MainSection.Element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            this.EmptySection.Element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            this.FooterSection.Element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            leftOffset += this.HeaderSection.Element.querySelector(
                `tr:first-child th:nth-child(${index + 1})`
            )?.offsetWidth || 0;
        });

        // --- Thêm xử lý FrozenRight ---
        const stickyRightColumns = this.Header
            .map((item, index) => item.FrozenRight ? index : -1)
            .filter(index => index !== -1)
            .sort((a, b) => b - a); // xử lý từ phải qua trái

        let rightOffset = 0;
        stickyRightColumns.forEach((index) => {
            const cellWidth = this.HeaderSection.Element.querySelector(
                `tr:first-child th:nth-child(${index + 1})`
            )?.offsetWidth || 0;

            [this.HeaderSection, this.SearchSection, this.MainSection, this.EmptySection, this.FooterSection]
                .forEach(section => {
                    section.Element.querySelectorAll(
                        `tr td:nth-child(${index + 1}), tr th:nth-child(${index + 1})`
                    ).forEach((cell) => {
                        cell.classList.add("sticky-column-right");
                        cell.style.right = `${rightOffset}px`;
                    });
                });

            rightOffset += cellWidth;
        });
    }

    UpdateStickySummary() {
        const stickyColumns = this.Header
            .map((item, index) => item.Frozen ? index : -1)
            .filter(index => index !== -1);
        stickyColumns.sort((a, b) => a - b);
        this.FooterSection.Element.querySelectorAll("th, td").forEach((cell) => {
            cell.classList.remove("sticky-column");
            cell.style.left = "";
            cell.classList.remove("sticky-column-right");
            cell.style.right = "";
        });

        let leftOffset = 0;
        stickyColumns.forEach((index) => {
            this.FooterSection.Element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            leftOffset += this.HeaderSection.Element.querySelector(
                `tr:first-child th:nth-child(${index + 1})`
            )?.offsetWidth || 0;
        });

        // --- Thêm xử lý FrozenRight ---
        const stickyRightColumns = this.Header
            .map((item, index) => item.FrozenRight ? index : -1)
            .filter(index => index !== -1)
            .sort((a, b) => b - a);

        let rightOffset = 0;
        stickyRightColumns.forEach((index) => {
            const cellWidth = this.HeaderSection.Element.querySelector(
                `tr:first-child th:nth-child(${index + 1})`
            )?.offsetWidth || 0;

            this.FooterSection.Element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column-right");
                cell.style.right = `${rightOffset}px`;
            });

            rightOffset += cellWidth;
        });
    }

    intChangeSummary = 0;
    AddSummaries() {
        window.clearTimeout(this.intChangeSummary);
        this.intChangeSummary = window.setTimeout(() => {
            if (this.Header.some(x => !Utils.isNullOrWhiteSpace(x.GroupFormat))) {
                this.AllListViewItem.filter(x => x.GroupRow).forEach(item => {
                    item.Children.filter(x => !Utils.isNullOrWhiteSpace(x.Meta.GroupFormat)).forEach(cell => {
                        item.Cell = cell;
                        var groupText = Utils.IsFunction(cell.Meta.GroupFormat, false, item);
                        cell.Element.innerHTML = groupText;
                    });
                })
            }
            const sums = this.Header.filter(x => !Utils.isNullOrWhiteSpace(x.Summary));
            if (!sums || sums.length == 0 || this.Item.length == 0) {
                if (this.FooterSection && this.FooterSection.Element.firstChild) {
                    this.FooterSection.Element.firstChild.childNodes.forEach(x => x.innerHTML = '');
                }
                return;
            }
            const summaryElements = this.MainSection.Element.querySelectorAll(`.${this.SummaryClass}`);
            summaryElements.forEach(x => x.remove());
            const count = new Set(sums.map(x => x.Summary)).size;
            sums.forEach(header => {
                this.RenderSummaryRow(header, this.Header, this.FooterSection.Element, count);
            });
            this.UpdateStickySummary();
        }, 100);
    }

    RenderSummaryRow(sum, headers, footer, count) {
        let tr = this.CreateSummaryTableRow(sum, footer, count);
        if (!tr) {
            return;
        }
        tr.classList.add("summary");
        if (!headers.includes(sum)) {
            this.ClearSummaryContent(tr);
            return;
        }
        this.CalcSumCol(sum, headers, tr);
    }
    /**
     * 
     * @param {Component} sum 
     * @param {HTMLTableSectionElement} footer 
     * @param {Number} count 
     * @returns 
     */
    CreateSummaryTableRow(sum, footer, count) {
        let summaryText = sum.Summary;
        if (!footer) {
            return null;
        }
        let summaryRows = Array.from(footer.rows).filter(row => row.classList.contains('summary'));
        let existSumRow = Array.from(footer.rows).reverse().find(row => row.classList.contains('summary') & Array.from(row.cells).some(cell => cell.textContent === summaryText));
        if (!existSumRow) {
            existSumRow = footer.rows[footer.rows.length - 1];
        }
        /**
         * @type {HTMLTableRowElement}
         */
        var firstChild = null;
        if (this.Meta.Editable) {
            if (this.Meta.CanAdd) {
                firstChild = this.EmptySection.Element.firstChild;
            }
            else {
                firstChild = this.MainSection.Children.find(x => !x.GroupRow).Element;
            }
        }
        else {
            firstChild = this.MainSection.Children.find(x => !x.GroupRow).Element;
        }
        if (!firstChild) {
            if (existSumRow != null) {
                existSumRow.remove();
            }
            return null;
        }
        if (summaryRows.length >= count) {
            this.UpdateTdClassFromFirstChild(existSumRow, firstChild);
            return existSumRow;
        }
        var result = firstChild.cloneNode(true);
        footer.appendChild(result);
        result.childNodes.forEach(x => x.innerHTML = null);
        return result;
    }

    UpdateTdClassFromFirstChild(existSumRow, firstChild) {
        var firstChildTds = firstChild.querySelectorAll("td");
        var existSumRowTds = existSumRow.querySelectorAll("td");
        for (var i = 0; i < firstChildTds.length && i < existSumRowTds.length; i++) {
            existSumRowTds[i].className = "text-right";
        }
    }

    CalcSumCol(header, headers, tr) {
        const index = headers.indexOf(header);
        const cellVal = tr.cells[index];
        if (header.IsTotal && header.ComponentType !== "Number") {
            cellVal.textContent = this.Item.length.toString();
        }
        if (header.IsTotal) {
            const sum = this.TotalHeaders.reduce((a, b) => a.plus(b), new Decimal(0));
            var pre = header.GroupTypeId ? parseInt(LangSelect._webConfig[header.GroupTypeId]) : header.Precision;
            if (this.Meta.Frozen) {
                cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toDP(0).toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
            }
            else {
                cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
            }
        }
        else {
            if (this.Meta.IsMultiple) {
                const sum = this.GetSelectedRows().reduce((a, b) => a.plus(new Decimal(b[header.FieldName] || 0)), new Decimal(0))
                var pre = header.GroupTypeId ? parseInt(LangSelect._webConfig[header.GroupTypeId]) : header.Precision;
                if (this.Meta.Frozen) {
                    cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toDP(0).toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
                }
                else {
                    cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
                }
            }
            else {
                const sum = this.AllListViewItem.filter(x => !x.GroupRow).map(x => x.Entity).reduce((a, b) => a.plus(new Decimal(b[header.FieldName] || 0)), new Decimal(0))
                var pre = header.GroupTypeId ? parseInt(LangSelect._webConfig[header.GroupTypeId]) : header.Precision;
                if (this.Meta.Frozen) {
                    cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toDP(0).toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
                }
                else {
                    cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
                }
            }
        }
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

    /**
     * @typedef {import('./listViewItem.js').ListViewItem} ListViewItem
     * Handles custom events based on row changes, applying data updates and managing component state.
     * @param {object} rowData The data of the row that triggered the change.
     * @param {ListViewItem} rowSection The ListViewItem corresponding to the row.
     * @param {import("editableComponent.js").ObservableArgs} observableArgs Additional arguments or data relevant to the event.
     * @param {import("models/observable.js").EditableComponent} [component=null] Optional component that might be affected by the row change.
     * @returns {Promise<boolean>} A promise that resolves to a boolean indicating success or failure of the event handling.
     */
    async RowChangeHandler(rowData, rowSection, observableArgs, component = null) {
        if (rowSection.EmptyRow && observableArgs.EvType === EventType.Change) {
            if (!Utils.isNullOrWhiteSpace(this.Meta.DefaultVal)) {
                var rsObj = Utils.IsFunction(this.Meta.DefaultVal, false, this);
                if (rsObj) {
                    Object.getOwnPropertyNames(rsObj).forEach(x => {
                        rowSection.Entity[x] = rsObj[x];
                    });
                }
            }
            rowSection.Entity["InsertedBy"] = this.Token.UserId;
            await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforeCreated, rowSection, rowData, this);
            if (!Utils.isNullOrWhiteSpace(this.Meta.GroupBy)) {
                let keys = this.Meta.GroupBy.split(",");
                rowSection.Entity[this._groupKey] = keys.map(key => rowSection.Entity[key]).join(" ");
            }
            let rs;
            if (this.Meta.IsRealtime) {
                rs = await rowSection.PatchUpdateOrCreate();
                Object.assign(rowSection.Entity, rs);
                if (this.Meta.ComponentType == "VirtualGrid") {
                    this.CacheData.push(rs);
                }
                this.Dirty = false;
            } else {
                Object.assign(rowSection.Entity, rs);
                this.Dirty = true;
            }
            await this.LoadMasterData([rowSection.Entity]);
            rowSection.UpdateView(true);
            rowSection.EmptyRow = false;
            this.MoveEmptyRow(rowSection);
            this.EmptySection.Children = [];
            this.AddNewEmptyRow();
            this.ClearSelected();
            rowSection.Selected = true;
            rowSection.Focused = true;
            this.LastListViewItem = rowSection;
            this.RenderIndex();
            await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterCreated, rowSection, rowData, this);
            if (this.LastComponentFocus.ComponentType != "Select") {
                window.setTimeout(() => {
                    this.LastElementFocus.focus();
                }, 100);
            }
        }
        else {
            if (!Utils.isNullOrWhiteSpace(this.Meta.GroupBy)) {
                let keys = this.Meta.GroupBy.split(",");
                rowSection.Entity[this._groupKey] = keys.map(key => rowSection.Entity[key]).join(" ");
            }
            if (rowSection.GroupSection) {
                if (rowSection.Entity[this._groupKey] != rowSection.GroupSection.Key) {
                    const index = rowSection.GroupSection.ChildrenItems.indexOf(rowSection);
                    if (index > -1) {
                        rowSection.GroupSection.ChildrenItems.splice(index, 1);
                    }
                    if (rowSection.GroupSection.ChildrenItems.length == 0) {
                        const index1 = this.AllListViewItem.indexOf(rowSection.GroupSection);
                        this.AllListViewItem.splice(index1, 1);
                        rowSection.GroupSection.Dispose();
                    }
                    this.MoveGroupRow(rowSection);
                    this.ClearSelected();
                }
                else {
                    var groupText = Utils.IsFunction(this.Meta.GroupFormat, false, rowSection.GroupSection);
                    if (rowSection.GroupSection.GroupText) {
                        rowSection.GroupSection.GroupText.innerHTML = groupText;
                    }
                }
                this.RenderIndex();
            }
        }
        if (component && component.ComponentType == "GridView") {
            await this.DispatchEvent(component.Meta.Events, observableArgs.EvType, this, rowSection, rowData);
        }
        await this.DispatchEvent(this.Meta.Events, observableArgs.EvType, this, rowSection, rowData);
        if (observableArgs.EvType === EventType.Change) {
            this.RenderIndex();
            if (this.Meta.IsSumary) {
                this.AddSummaries();
            }
            this.LastListViewItem = rowSection;
        }
    }
    /**
     * @param {ListViewItem} rowSection
     */
    MoveGroupRow(rowSection) {
        let groupSection = this.AllListViewItem.find(group => group.GroupRow && group.Key === rowSection.Entity[this._groupKey]);
        var currentIndex = this.AllListViewItem.indexOf(rowSection);
        this.AllListViewItem.splice(currentIndex, 1);
        if (groupSection) {
            rowSection.Parent = this.MainSection;
            rowSection.ListViewSection = this.MainSection;
            rowSection.GroupSection = groupSection;
            rowSection.Element.classList.add("group-detail");
            var lastChild = groupSection.ChildrenItems[groupSection.ChildrenItems.length - 1];
            var index = this.AllListViewItem.indexOf(lastChild);
            if (this.AllListViewItem.length == index + 1) {
                this.MainSection.Element.appendChild(rowSection.Element);
            }
            else {
                this.MainSection.Element.insertBefore(rowSection.Element, this.AllListViewItem[index + 1].Element);
            }
            this.AllListViewItem.splice(index + 1, 0, rowSection);
            groupSection.ChildrenItems.push(rowSection);
            this.Dirty = true;
            return rowSection;
        }
        else {
            Html.Take(this.MainSection);
            groupSection = new GroupViewItem(ElementType.tr);
            groupSection.Key = rowSection.Entity[this._groupKey];
            groupSection.Entity = rowSection.Entity;
            groupSection.ParentElement = this.MainSection.Element;
            groupSection.ListViewSection = true;
            groupSection.ListViewSection = this.MainSection;
            groupSection.ListView = this;
            this.MainSection.AddChild(groupSection);
            groupSection.Element.tabIndex = -1;
            var groupText = Utils.IsFunction(this.Meta.GroupFormat, false, rowSection);
            Html.Instance.TData.ClassName("status-cell").TabIndex(-1).Event(EventType.Click, () => groupSection.ShowChildren1 = !groupSection.ShowChildren1).Icon("fal fa-square");
            groupSection.Chevron = Html.Context;
            Html.Instance.End.End.TData.Event(EventType.Click, () => this.DispatchClick(first))
                .Event(EventType.DblClick, () => this.DispatchDblClick(first))
                .Div.ClassName("d-flex");
            groupSection.GroupText = Html.Context;
            Html.Instance.InnerHTML(groupText);
            Html.Instance.EndOf(ElementType.td);
            this.Header.slice(2).forEach(item => {
                Html.Instance.TData.ClassName("data-summary").Style("font-weight:600");
                var sec = new Section(null, Html.Context);
                sec.Meta = item;
                groupSection.AddChild(sec);
                Html.Instance.EndOf(ElementType.td);
            });
            Html.Instance.EndOf(ElementType.tr);
            Html.Take(this.MainSection.Element);
            rowSection.Element.classList.add("group-detail");
            groupSection.ChildrenItems.push(rowSection);
            rowSection.GroupSection = groupSection;
            var lastChild = groupSection.ChildrenItems[groupSection.ChildrenItems.length - 1];
            var index = this.AllListViewItem.indexOf(groupSection);
            if (this.AllListViewItem.length == index + 1) {
                this.MainSection.Element.appendChild(rowSection.Element);
            }
            else {
                this.MainSection.Element.insertBefore(rowSection.Element, this.AllListViewItem[index + 1].Element);
            }
            this.AllListViewItem.splice(index + 1, 0, rowSection);
            return rowSection;
        }
    }
    /**
     * @param {ListViewItem} rowSection
     */
    MoveEmptyRow(rowSection) {
        if (this.Meta.TopEmpty) {
            if (!this.MainSection.Children.includes(this.EmptySection.FirstChild)) {
                this.MainSection.Children.unshift(this.EmptySection.FirstChild);
            }
            this.MainSection.Element.prepend(this.EmptySection.Element.firstElementChild);
        } else {
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
    HideColumn(...param) {
        if (this.HeaderSection) {
            this.HeaderSection.Children.forEach(column => {
                if (param.includes(column.Meta.FieldName)) {
                    if (column.Element.style.display == "") {
                        var parentElement = this.ThGroup.find(x => x.GroupName == column.Meta.GroupName);
                        if (parentElement) {
                            var col = (parseInt(parentElement.Element.getAttribute("colspan")) - 1);
                            parentElement.Element.setAttribute("colspan", col.toString());
                            if (col == 0) {
                                parentElement.Element.style.display = "none";
                            }
                            else {
                                parentElement.Element.style.display = "";
                            }
                        }
                    }
                    column.Element.style.display = "none";
                }
                else {
                    if (column.Element.style.display == "none") {
                        var parentElement = this.ThGroup.find(x => x.GroupName == column.Meta.GroupName);
                        if (parentElement) {
                            var col = parseInt(parentElement.Element.getAttribute("colspan")) + 1
                            parentElement.Element.setAttribute("colspan", (col).toString());
                            parentElement.Element.style.display = "";
                        }
                    }
                    column.Element.style.display = "";
                }
            })
        }
        if (this.GridViewItemEmpty) {
            this.GridViewItemEmpty.Children.forEach(column => {
                if (param.includes(column.Meta.FieldName)) {
                    column.Element.closest("td").style.display = "none";
                }
                else {
                    column.Element.closest("td").style.display = "";
                }
            })
        }
        if (this.FooterSection && this.FooterSection.Element.firstChild) {
            this.FooterSection.Element.firstChild.childNodes.forEach(column => {
                if (param.includes(column.getAttribute("data-field"))) {
                    column.style.display = "none";
                }
                else {
                    column.style.display = "";
                }
            })
        }
        if (this.AllListViewItem) {
            this.AllListViewItem.forEach(row => {
                row.Element.childNodes.forEach(column => {
                    if (param.includes(column.getAttribute("data-field"))) {
                        column.style.display = "none";
                    }
                    else {
                        column.style.display = "";
                    }
                });
                if (row.GroupRow) {
                    this.MoveElementToSecondVisibleTd(row.GroupText, row.Element);
                }
            })
        }
        this.UpdateStickyColumns();
    }

    MoveElementToSecondVisibleTd(ele, row) {
        const tds = Array.from(row.querySelectorAll("td"));
        let visibleCount = 0;
        for (const td of tds) {
            if (td.style.display !== "none") {
                visibleCount++;
                if (visibleCount === 2) {
                    td.appendChild(ele);
                    return;
                }
            }
        }
    }

    ThGroup = [];
    RenderTableHeader(headers) {
        if (!headers || headers.length == 0) {
            headers = this.Header;
        }
        if (headers.Count != this.Header.Count) {
            this.FilterColumns(headers);
        }
        if (this.HeaderSection.Element === null) {
            this.AddSections();
        }
        headers.forEach((x, index) => x.PostOrder = index);
        this.HeaderSection.DisposeChildren();
        const anyGroup = headers.some(x => x.GroupName && !Utils.isNullOrWhiteSpace(x.GroupName));
        Html.Take(this.HeaderSection.Element).Clear().TRow.ForEach(headers, (header, index) => {
            if (anyGroup && !Utils.isNullOrWhiteSpace(header.GroupName)) {
                if (header !== headers.find(x => x.GroupName === header.GroupName)) {
                    return;
                }
                Html.Th.Attr("component", header.ComponentType || "Number").ColSpan(headers.filter(x => x.GroupName === header.GroupName).length);
                this.ThGroup.push({
                    GroupName: header.GroupName,
                    Element: Html.Context
                });
                var groupCom = this.LoadGridPolicy().find(x => x.GroupName == header.GroupName && x.TopEmpty);
                if (groupCom) {
                    Html.Event(EventType.ContextMenu, this.HeaderContextMenu.bind(this), groupCom)
                    var com = ComponentFactory.GetComponent(groupCom, this.EditForm, null, true);
                    com.ParentElement = Html.Context;
                    this.EditForm.AddChild(com);
                    this.EditForm.ChildCom.push(com);
                    if (groupCom.Disabled) {
                        com.SetDisabled(true);
                    }
                    Html.EndOf("th");
                }
                else {
                    Html.IHtml(header.GroupName, this.EditForm.Meta.Label).End.Render();
                }
                return;
            }
            Html.Th.Attr("component", header.ComponentType || "Number")
                .TabIndex(-1).Width(header.AutoFit ? "auto" : header.Width)
                .Style(`${header.Style};min-width: ${header.MinWidth}; max-width: ${header.MaxWidth}`)
                .TextAlign('center')
                .Event(EventType.ContextMenu, this.HeaderContextMenu.bind(this), header)
                .Event(EventType.FocusOut, e => this.FocusOutHeader(e, header))
                .Event(EventType.KeyDown, e => this.ThHotKeyHandler(e, header));
            var sec = new Section(null, Html.Context);
            sec.Meta = header;
            this.HeaderSection.AddChild(sec);
            if (anyGroup && (!header.GroupName || header.GroupName === "")) {
                Html.Instance.RowSpan(2);
            }
            if (!anyGroup && this.Header.some(x => x.GroupName && x.GroupName.length)) {
                Html.Instance.ClassName("header-group");
            }
            if (header.StatusBar) {
                Html.Instance.A.Icon("fal fa-level-down").Event(EventType.Click, this.ToggleAll.bind(this)).End.End.Render();
            }
            if (header.Icon) {
                Html.Instance.Icon(header.Icon).Margin(Direction.Right, 0).End.Render();
            } else if (!header.StatusBar) {
                if (!anyGroup) {
                    Html.Instance.Event(EventType.Click, e => this.ClickHeader(e, header))
                }
                else {
                    Html.Instance.Event(EventType.Click, (e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        if (this.EditForm.DevToolsElement) {
                            this.EditForm.UpdateMetaData(header);
                        }
                    });
                }
                Html.Instance.IHtml(header.Label, this.EditForm.Meta.Label).Render();
            }
            if (header.ComponentType === "Number") {
                Html.Instance.Div.End.Render();
                Html.Instance.Span.Style("display: block;").End.Render();
            }
            if (header.Description) {
                Html.Instance.Attr("title", header.Description);
            }
            if (!header.StatusBar && header.ComponentType != 'Button' && header.FieldName) {
                Html.Instance.Div.ClassName("th-options").I.ClassName("fal fa-ellipsis-v").End.End.Render();
                this.CreateResizableTable(Html.Context);
            }
            Html.Instance.EndOf(ElementType.th);
        }).EndOf(ElementType.tr).Render();

        if (anyGroup) {
            Html.Instance.TRow.ForEach(headers, (header, index) => {
                if (anyGroup && !Utils.isNullOrWhiteSpace(header.GroupName)) {
                    Html.Instance.Th.Attr("component", header.ComponentType || "Number").Style(`min-width: ${header.MinWidth}; max-width: ${header.MaxWidth}`)
                        .TextAlign(header.TextAlignEnum)
                        .Event(EventType.ContextMenu, this.HeaderContextMenu.bind(this), header)
                        .IHtml(header.Label, this.EditForm.Meta.Label);
                    var sec = new Section(null, Html.Context);
                    sec.Meta = header;
                    this.HeaderSection.AddChild(sec);
                    Html.Instance.EndOf(ElementType.th);
                }
            });
        }
        this.HeaderSection.Children = this.HeaderSection.Children.sort((a, b) => a.Meta.PostOrder - b.Meta.PostOrder);
        if (this.Meta.CanSearch) {
            var headerHeight = this.HeaderSection.Element.clientHeight;
            this.SearchSection.Element.style.top = `${headerHeight}px`;
            this.SearchSection.Element.style.position = "sticky";
            Html.Take(this.SearchSection.Element).Clear().TRow.ForEach(headers, (header, index) => {
                Html.TData.TabIndex(-1);
                if (header.StatusBar) {
                    Html.Instance.Style(`top:${headerHeight}px`).Span.ClassName("fal fa-search").End;
                }
                else {
                    Html.Div.ClassName("input-group-button").TabIndex(-1);
                }
                var sec = new Section(null, Html.Context);
                sec.Meta = header;
                if (header.FieldName == "Id") {
                    this.SearchSection.AddChild(sec);
                }
                else {
                    switch (header.ComponentType) {
                        case "Dropdown":
                        case "Input":
                            var txtSearch = new Textbox({
                                FieldName: header.FieldName,
                                PlainText: 'Input search...',
                                ShowLabel: false
                            });
                            txtSearch.SearchIcon = "fal fa-search";
                            txtSearch.SearchMethod = SearchMethodEnum.Contain;
                            txtSearch.OrderMethod = "asc";
                            txtSearch.IsOrderBy = false;
                            txtSearch.Entity = this.ListViewSearch.EntityVM;
                            this.SearchSection.AddChild(txtSearch);
                            txtSearch.Element.addEventListener("keydown", (e) => {
                                let code = e.KeyCodeEnum();
                                if (code == KeyCodeEnum.Enter) {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    this.ApplyFilter();
                                }
                            });
                            Html.End.Div.ClassName("btn-group").Button.TabIndex(-1).Event("click", (e) => {
                                this.SearchTypeMenu(e, header, txtSearch);
                            }).Span.ClassName(txtSearch.SearchIcon);
                            txtSearch.SearchIconElement = Html.Context;
                            break;
                        case "Datepicker":
                            var txtSearch = new Datepicker({
                                FieldName: header.FieldName,
                                ShowLabel: false,
                                Precision: 2,
                                ShowHotKey: true
                            });
                            txtSearch.SearchMethod = SearchMethodEnum.Range;
                            txtSearch.OrderMethod = "asc";
                            txtSearch.SearchIcon = "fal fa-arrows-alt-h";
                            this.SearchSection.AddChild(txtSearch);
                            txtSearch.Element.addEventListener("keydown", (e) => {
                                let code = e.KeyCodeEnum();
                                if (code == KeyCodeEnum.Enter) {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    this.ApplyFilter();
                                }
                            });
                            Html.End.Div.ClassName("btn-group").Button.TabIndex(-1).Event("click", (e) => {
                                this.SearchTypeMenu(e, header, txtSearch);
                            }).Span.ClassName(txtSearch.SearchIcon);
                            txtSearch.SearchIconElement = Html.Context;
                            break;
                        case "Checkbox":
                            var txtSearch = new Select({
                                FieldName: header.FieldName,
                                ShowLabel: false,
                                Query: `[{
                                            "Id": "1,0",
                                            "Name": "All"
                                        },
                                        {
                                            "Id": "1",
                                            "Name": "Check"
                                        },
                                        {
                                            "Id": "0",
                                            "Name": "Uncheck"
                                        }]`
                            });
                            this.SearchSection.AddChild(txtSearch);
                            txtSearch.SearchMethod = SearchMethodEnum.Contain;
                            txtSearch.Element.addEventListener("keydown", (e) => {
                                let code = e.KeyCodeEnum();
                                if (code == KeyCodeEnum.Enter) {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    this.ApplyFilter();
                                }
                            });
                            txtSearch.SearchIconElement = Html.Context;
                            break;
                        default:
                            var sec = new Section(null, Html.Context);
                            sec.Meta = header;
                            this.SearchSection.AddChild(sec);
                            break;
                    }
                }
                Html.EndOf(ElementType.td);
            }).EndOf(ElementType.tr).Render();
        }
    }

    SearchTypeMenu(e, header, txtSearch) {
        const ele = e.target;
        var buttonRect = ele.getBoundingClientRect();
        var ctxMenu = ContextMenu.Instance;
        ctxMenu.Top = buttonRect.bottom;
        ctxMenu.Left = buttonRect.left;
        ctxMenu.MenuItems = [];
        if (header.ComponentType == "Input" || header.ComponentType == "Dropdown") {
            var className = txtSearch.OrderMethod == "asc" ? "fas fa-sort-amount-up" : "fas fa-sort-amount-down";
            ctxMenu.MenuItems.push({
                Icon: className,
                Text: "Order By",
                Click: () => this.ActionSearch(header, "OrderBy", txtSearch, className)
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-search",
                Text: "Contains",
                Click: () => this.ActionSearch(header, SearchMethodEnum.Contain, txtSearch, "fal fa-search")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-search-minus",
                Text: "No Contains",
                Click: () => this.ActionSearch(this.Meta, SearchMethodEnum.NotContain, txtSearch, "fal fa-search-minus")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-arrow-right",
                Text: "Starts With",
                Click: () => this.ActionSearch(header, SearchMethodEnum.StartWith, txtSearch, "fal fa-arrow-right")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-arrow-left",
                Text: "Ends With",
                Click: () => this.ActionSearch(header, SearchMethodEnum.EndWith, "fal fa-arrow-left")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-minus-circle",
                Text: "Empty",
                Click: () => this.ActionSearch(header, SearchMethodEnum.Empty, txtSearch, "fal fa-minus-circle")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-check-circle",
                Text: "Not Empty",
                Click: () => this.ActionSearch(header, SearchMethodEnum.Filled, txtSearch, "fal fa-check-circle")
            });
        }
        else if (header.ComponentType == "Datepicker" || header.ComponentType == "Number") {
            var className = txtSearch.OrderMethod == "asc" ? "fas fa-sort-amount-up" : "fas fa-sort-amount-down";
            ctxMenu.MenuItems.push({
                Icon: className,
                Text: "Order By",
                Click: () => this.ActionSearch(header, "OrderBy", txtSearch, className)
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-arrows-alt-h",
                Text: "Between",
                Click: () => this.ActionSearch(header, SearchMethodEnum.Range, txtSearch, "fal fa-arrows-alt-h")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-equals",
                Text: "Equals",
                Click: () => this.ActionSearch(header, SearchMethodEnum.Equal, txtSearch, "fal fa-equals")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-not-equal",
                Text: "Not Equals",
                Click: () => this.ActionSearch(header, SearchMethodEnum.NotEqual, txtSearch, "fal fa-not-equal")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-greater-than",
                Text: "Greater Than",
                Click: () => this.ActionSearch(header, SearchMethodEnum.Greater, txtSearch, "fal fa-greater-than")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-less-than",
                Text: "Less Than",
                Click: () => this.ActionSearch(header, SearchMethodEnum.Smaller, txtSearch, "fal fa-less-than")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-greater-than-equal",
                Text: "Greater Than or Equal",
                Click: () => this.ActionSearch(header, SearchMethodEnum.GreaterEqual, txtSearch, "fal fa-greater-than-equal")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-less-than-equal",
                Text: "Less Than or Equal",
                Click: () => this.ActionSearch(header, SearchMethodEnum.SmallerEqual, txtSearch, "fal fa-less-than-equal")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-minus-circle",
                Text: "Empty",
                Click: () => this.ActionSearch(header, SearchMethodEnum.Empty, txtSearch, "fal fa-minus-circle")
            });
            ctxMenu.MenuItems.push({
                Icon: "fal fa-check-circle",
                Text: "Not Empty",
                Click: () => this.ActionSearch(header, SearchMethodEnum.Filled, txtSearch, "fal fa-check-circle")
            });
        }
        ctxMenu.EditForm = this.EditForm;
        ctxMenu.Render();
    }

    ActionSearch(header, type, txtSearch, className) {
        txtSearch.SearchMethod = type;
        if (type == "OrderBy") {
            this.SearchSection.Children.forEach(x => x.IsOrderBy = false);
            txtSearch.OrderMethod = txtSearch.OrderMethod == "asc" ? "desc" : "asc";
            className = txtSearch.OrderMethod == "asc" ? "fas fa-sort-amount-up" : "fas fa-sort-amount-down";
            txtSearch.IsOrderBy = true;
        }
        txtSearch.SearchIconElement.className = className;
        this.ApplyFilter();
    }
    /**
     * @param {HTMLElement} col
     */
    CreateResizableTable(col) {
        var resizer = document.createElement("div");
        resizer.classList.add("resizer");
        col.appendChild(resizer);
        this.CreateResizableColumn(col, resizer);
    }
    /**
     * @param {HTMLElement} col
     * @param {HTMLElement} resizer
     */
    CreateResizableColumn(col, resizer) {
        this.x = 0;
        this.w = 0;
        resizer.addEventListener("mousedown", (e) => this.MouseDownHandler(e, col, resizer));
    }
    /** @type {MouseEvent} */
    mouseMoveHandler;
    /** @type {MouseEvent} */
    mouseUpHandler;
    /** @type {Number} */
    x = 0;
    /** @type {Number} */
    w = 0;
    /**
     * @param {MouseEvent} mouse
     * @param {HTMLElement} col
     * @param {HTMLElement} resizer
     */
    MouseDownHandler(mouse, col, resizer) {
        mouse.preventDefault();
        this.x = mouse.clientX;
        var styles = window.getComputedStyle(col);
        this.w = parseFloat((styles.width.replace("px", "") == "") ? "0" : styles.width.replace("px", ""));
        this.mouseMoveHandler = (a) => this.MouseMoveHandler(a, col, resizer);
        this.mouseUpHandler = (a) => this.MouseUpHandler(a, col, resizer);
        document.addEventListener("mousemove", this.mouseMoveHandler);
        document.addEventListener("mouseup", this.mouseUpHandler);
        resizer.classList.add("resizing");
    }

    /**
     * @param {MouseEvent} mouse
     * @param {HTMLElement} col
     * @param {HTMLElement} resizer
     */
    MouseMoveHandler(mouse, col, resizer) {
        mouse.preventDefault();
        var dx = mouse.clientX - this.x;
        col.style.width = `${this.w + dx}px`;
        col.style.minWidth = `${this.w + dx}px`;
        col.style.maxWidth = `${this.w + dx}px`;
        this.UpdateStickyColumns();
    }
    /**
     * @param {MouseEvent} mouse
     * @param {HTMLElement} col
     * @param {HTMLElement} resizer
     */
    MouseUpHandler(mouse, col, resizer) {
        mouse.preventDefault();
        this.UpdateHeaders();
        resizer.classList.remove("resizing");
        document.removeEventListener("mousemove", this.mouseMoveHandler);
        document.removeEventListener("mouseup", this.mouseUpHandler);
    }
    _imeout = 0;
    UpdateHeaders(sticky) {
        window.clearTimeout(this._imeout);
        this._imeout = window.setTimeout(() => {
            const headerElements = this.HeaderSection.Children.filter(x => x.Meta && x.Meta.Id);
            let index = 0;
            let anyGroup = this.Header.some(x => x.GroupName && !Utils.isNullOrWhiteSpace(x.GroupName));
            if (!anyGroup) {
                headerElements.forEach(header => {
                    header.Order = index;
                    header.Meta.Order = index;
                    index++;
                });
            }
            if (Client.SystemRole) {
                const columns = headerElements.map(header => {
                    const match = header.Element;
                    if (match && !header.Meta.StatusBar && Utils.isNullOrWhiteSpace(match.style.display)) {
                        const width = `${match.offsetWidth}px`;
                        const dirtyPatch = [
                            { Field: "Id", Value: header.Meta.Id },
                            { Field: "FeatureId", Value: header.Meta.FeatureId },
                            { Field: "Frozen", Value: header.Meta.Frozen },
                            { Field: "FrozenRight", Value: header.Meta.FrozenRight },
                            Utils.isNullOrWhiteSpace(header.GroupName) ? { Field: "Width", Value: width } : { Field: "Width", Value: header.Meta.Width },
                            Utils.isNullOrWhiteSpace(header.GroupName) ? { Field: "MaxWidth", Value: width } : { Field: "MaxWidth", Value: header.Meta.MaxWidth },
                            Utils.isNullOrWhiteSpace(header.GroupName) ? { Field: "MinWidth", Value: width } : { Field: "MinWidth", Value: header.Meta.MinWidth },
                        ];
                        if (!anyGroup) {
                            dirtyPatch.push({ Field: "Order", Value: header.Order })
                        }
                        return {
                            Changes: dirtyPatch,
                            NotMessage: true,
                            Table: "Component",
                        };
                    }
                    return null;
                }).filter(x => x != null);
                Client.Instance.PatchAsync2(columns).then();
            }
            else {
                const columns = headerElements.map(header => {
                    const match = header.Element;
                    if (match && !header.Meta.StatusBar && !Utils.isNullOrWhiteSpace(header.Meta.FieldName) && Utils.isNullOrWhiteSpace(match.style.display)) {
                        const width = `${match.offsetWidth}px`;
                        return {
                            Id: header.Meta.Id,
                            FieldName: header.Meta.FieldName,
                            Frozen: header.Meta.Frozen,
                            FrozenRight: header.Meta.FrozenRight,
                            Order: header.Order,
                            Width: width,
                        };
                    }
                    return null;
                }).filter(x => x != null);
                var userSetting = new UserSetting();
                userSetting.FeatureId = this.EditForm.Meta.Label;
                userSetting.ComponentId = this.Meta.Id;
                userSetting.Active = true;
                userSetting.Value = JSON.stringify(columns);
                Client.Instance.PostAsync(userSetting, "/api/UserSetting").then();
            }
            if (sticky) {
                this.UpdateStickyColumns();
            }
        }, 500);
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
            // @ts-ignore
            Client.Instance.PatchAsync(patchVM);
        }, 1000);
    }

    UpdatePagination(total, currentPageCount) {
        if (!this.Paginator) {
            return;
        }
        var options = this.Paginator.Options;
        options.Total = total;
        options.CurrentPageCount = currentPageCount;
        options.PageNumber = (options.PageIndex || 0) + 1;
        options.StartIndex = (options.PageIndex || 0) * options.PageSize + 1;
        options.EndIndex = options.StartIndex + options.CurrentPageCount - 1;
        this.Paginator.UpdateView();
        if (total <= this.Meta.Row) {
            this.Paginator.Show = false;
        }
        else {
            this.Paginator.Show = true;
        }
    }

    ToggleAll() {
        const anySelected = this.AllListViewItem.some(x => x.Selected);
        if (anySelected) {
            this.ClearSelected();
            return;
        }
        this.AllListViewItem.forEach(x => {
            x.Selected = true;
        });
    }

    HeaderContextMenu(e, header) {
        e.preventDefault();
        e.stopPropagation();
        var menu = ContextMenu.Instance;
        menu.Top = e.clientY;
        menu.Left = e.clientX;
        menu.MenuItems = [];
        if (Client.SystemRole) {
            menu.MenuItems.push({ Icon: "fal fa-wrench", Text: "Column Properties", Click: () => this.EditForm.ComponentProperties(header) });
            menu.MenuItems.push({ Icon: "fal fa-table", Text: "Table Properties", Click: () => this.EditForm.ComponentProperties(this.Meta) });
        }
        if (!header.StatusBar && header.FieldName && !header.GroupName) {
            if (Client.SystemRole && ["Input", "Dropdown", "Select", "Checkbox", "Textarea"].some(x => x == header.ComponentType)) {
                menu.MenuItems.push({ Icon: "fal fa-copy", Text: "Set Default Value", Click: this.SetDefaultValue.bind(this), Parameter: header });
            }
            menu.MenuItems.push({
                Icon: "fas fa-thumbtack", Text: "Pin Column", MenuItems: [
                    {
                        Icon: header.Frozen ? "fas fa-check" : "fas fa-ellipsis-h",
                        Text: "Pin Left",
                        Click: () => this.UpdateFrozen(header, 1)
                    },
                    {
                        Icon: header.FrozenRight ? "fas fa-check" : "fas fa-ellipsis-h",
                        Text: "Pin Right",
                        Click: () => this.UpdateFrozen(header, 2)
                    },
                    {
                        Icon: !header.Frozen && !header.FrozenRight ? "fas fa-check" : "fas fa-ellipsis-h",
                        Text: "No Pin",
                        Click: () => this.UpdateFrozen(header, 3)
                    }
                ]
            });
        }

        menu.EditForm = this.EditForm;
        menu.Render();
    }

    SetDefaultValue(component) {
        if (component.ComponentType == "GridView") {
            return;
        }
        var com = JSON.parse(JSON.stringify(component));
        com.FieldName = 'DefaultValue' + com.FieldName;
        var name = com.EntityName || "Entity";
        this[name][com.FieldName] = com.DefaultVal;
        this.EditForm.OpenConfig("Set default value", async () => {
            let dirtyPatchDetail = [
                {
                    Label: "Id",
                    Field: "Id",
                    OldVal: null,
                    Value: com.Id,
                },
                {
                    Label: "FeatureId",
                    Field: "FeatureId",
                    OldVal: null,
                    Value: com.FeatureId,
                },
                {
                    Label: "DefaultVal",
                    Field: "DefaultVal",
                    OldVal: null,
                    Value: this[name][com.FieldName],
                }
            ]
            let patchModelDetail = {
                Changes: dirtyPatchDetail,
                Table: "Component",
                NotMessage: true
            };
            component.DefaultVal = this[name][com.FieldName];
            await Client.Instance.PatchAsync(patchModelDetail);
            this.Dirty = false;
        }, () => { }, true, [com], null, null, null, true);
    }

    UpdateFrozen(header, frozenType) {
        if (frozenType === 1) {
            header.Frozen = true;
        }
        else if (frozenType === 2) {
            header.FrozenRight = true;
        }
        else {
            header.Frozen = false;
            header.FrozenRight = false;
        }
        this.UpdateHeaders(true);
    }

    FrozenColumn(arg) {
        const entity = arg.header;
        const header = this.Header.find(x => x.Id === entity.Id);
        if (header) {
            header.Frozen = !header.Frozen;
        }
        this.UpdateHeaders();
    }

    RemoveRowById(id) {
        super.RemoveRowById(id);
        this.RenderIndex();
    }

    RemoveRow(row) {
        super.RemoveRow(row);
        this.RenderIndex();
    }

    HardDeleteConfirmed(deleted, newId) {
        return new Promise((resolve, reject) => {
            super.HardDeleteConfirmed(deleted, newId).then(async res => {
                this.RenderIndex();
                if (this.Meta.IsSumary) {
                    this.AddSummaries();
                    var parent = this.EditForm.TabGroup.flatMap(x => x.Children);
                    if (parent.length > 0) {
                        for (const element of parent) {
                            await element.CountBadge();
                        }
                    }
                }
                resolve(res);
            }).catch(err => reject(err));
        });
    }
    /**
     * @param {boolean} force
     * @param {boolean} dirty
     */
    PrepareUpdateView(force, dirty) {
        super.PrepareUpdateView(force, dirty);
        if (this.Entity.Id && this.Entity.Id.startsWith("-") && this.Meta.Editable && this.Meta.CanAdd) {
            this.ToggleAddRow(true);
        }
        else {
            if (this.Meta && this.Meta.AddRowExp) {
                this.ToggleAddRow(this.Meta.AddRowExp);
            }
        }
    }
    /**
     * 
     * @param {Boolean | String | Function} disabled 
     */
    ToggleAddRow(add) {
        if (typeof add === "boolean") {
            if (add) {
                this.AddNewEmptyRow();
            }
            else {
                if (this.GridViewItemEmpty) {
                    this.GridViewItemEmpty.Dispose();
                    this.GridViewItemEmpty = null;
                }
            }
            return;
        }
        var addFn = Utils.IsFunction(add, false, this);
        if (addFn) {
            this.AddNewEmptyRow();
        }
        else {
            if (this.GridViewItemEmpty) {
                this.GridViewItemEmpty.Dispose();
                this.GridViewItemEmpty = null;
            }
        }
    }
    UpdateView(force = false, dirty = null, componentNames = []) {
        if (!this.Editable && !this.Meta.CanCache) {
            this.ActionFilter();
        } else {
            this.RowAction(row => !row.EmptyRow, row => row.UpdateView(force, dirty, componentNames));
        }
    }
    async RowChangeHandlerGrid(rowData, rowSection, observableArgs, component = null) {
        await new Promise(resolve => setTimeout(resolve, this.CellCountNoSticky));
        if (rowSection.EmptyRow && observableArgs.EvType === EventType.Change) {
            await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforeCreated, rowSection, rowData);
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
            await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterCreated, rowSection, rowData);
        }
        this.AddSummaries();
        await this.DispatchEvent(this.Meta.Events, EventType.Change, rowSection, rowData);
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
