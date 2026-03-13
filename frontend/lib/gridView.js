import { Toast } from './toast.js';
import { Spinner } from './spinner.js';
import { Utils } from "./utils/utils.js";
import { ListView } from './listView.js';
import { Uuid7 } from './structs/uuidv7.js';
import { ContextMenu } from './contextMenu.js';
import { Client } from "./clients/";
import { SearchEntry } from './searchEntry.js';
import { EditableComponent, LangSelect, Numbox } from './index.js';
import {
    EventType, ElementType, HotKeyModel, customEventType, Component,
    operatorEnum, keyCodeEnum, orderbyDirection, advSearchOperation, logicOperation
} from './models/';
import { searchMethodEnum } from './models/enum.js';
import { GridViewItem } from './gridViewItem.js';
import { ListRowItem } from './listRowItem.js';
import { ConfirmDialog } from './confirmDialog.js';
import { Direction, Html } from "./utils/html.js";
import { Section } from './section.js';
import { ListViewSection } from './listViewSection.js';
import { ListViewSearch, ListViewSearchVM } from './listViewSearch.js';
import { ComponentFactory } from './utils/componentFactory.js';
import Decimal from 'decimal.js';
import { GroupViewItem } from './groupViewItem.js';
import { UserSetting } from './models/userSetting.js';
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
        this.summaryClass = "summary";
        this.cellCountNoSticky = 50;
        this._summarys = [];
        this.lastThClick = null;
        this.lastNumClick = null;
        this.autoFocus = false;
        this.loadRerender = false;
        this._waitingLoad = false;
        this._renderPrepareCacheAwaiter = 0;
        /** @type {HTMLElement} */
        this.dataTable = null;
        this.lastElementFocus = null;
        this.lastListViewItem = null;
        this._hasFirstLoad = false;
        this._renderIndexAwaiter = 0;
        this._lastScrollTop = 0;
        this.dOMContentLoaded.add(this.dOMContentLoadedHandler.bind(this));
    }

    static toolbarColumn = {
        statusBar: true,
        Label: '',
        Frozen: true
    };
    /** @type {HTMLElement} */
    menuGridView;
    /** @type {GridViewItem} */
    gridViewItemEmpty;
    dOMContentLoadedHandler() {
        if (this.meta.isSumary) {
            this.addSummaries();
        }
    }

    Rerender() {
        this.loadRerender = true;
        this.Header = this.Header.filter(x => !x.Hidden);
        this.renderTableHeader(this.Header);
        if (this.Editable) {
            this.addNewEmptyRow();
        }
        this.renderContent();
        this.updateStickyColumns();
    }

    isMobile() {
        return /Android|iPhone|iPad|iPod|Opera Mini|iEMobile|wPDesktop/i.test(navigator.userAgent);
    }

    addSections() {
        if (this.headerSection && this.headerSection.element != null) {
            return;
        }
        Html.take(this.parentElement);
        Html.instance.div.style(this.meta.childStyle).className("grid-wrapper").className(this.meta.virtualScroll && this.isMobile() ? "" : this.meta.className).className(this.Editable ? "editable" : "");
        this.element = Html.context;
        if (this.meta.virtualScroll && this.isMobile()) {
            const style = this.element.style.height;
            const newHeight = style.replace(/calc\(\s*([-+]?\d*\.?\d+)rem\s*\+\s*100vh\s*\)/, (match, p1) => {
                const number = parseFloat(p1);
                return `calc(${number - 2}rem + 100vh)`;
            });
            this.element.style.height = newHeight;
        }
        Html.instance.div.className("d-grid").style("grid-template-columns: repeat(12, 1fr)").div.style("grid-area: span 1 / span 10;").className("grid-toolbar search").end.render();
        Html.instance.div.style("grid-area: span 1 / span 2;").className("button-toolbar").render();
        this.menuGridView = Html.context;
        Html.instance.end.render();
        var child = (this.editForm.meta.Components || []).filter(x => x.componentGroupId == this.meta.Id && (x.componentType == "Button" || x.componentType == "importExcel")).sort((a, b) => (a.Order || 0) - (b.Order || 0));;
        child.forEach(ui => {
            const com = ComponentFactory.getComponent(ui, this.editForm);
            if (com == null) return;
            com.parentElement = this.menuGridView;
            this.parent.addChild(com);
            this.editForm.childCom.push(com);
            com.Disabled = ui.Disabled || this.editForm.disabled || com.Disabled;
            if (ui.Focus) {
                com.Focus();
            }
            if (Client.systemRole) {
                com.element.addEventListener('contextmenu', e => {
                    e.preventDefault();
                    e.stopPropagation();
                    this.editForm.sysConfigMenu(e, ui, null, com)
                });
            }
        });
        this.listViewSearch = new ListViewSearch(this.meta);
        this.listViewSearch.entity = new ListViewSearchVM();
        if (this.meta.defaultAddStart) {
            let pre = this.meta.defaultAddStart;
            this.listViewSearch.entityVM.startDate = new Date(Date.now() + pre * 24 * 3600 * 1000);
        }
        let lFrom = window.localStorage.getItem("fromDate" + this.meta.Id);
        if (lFrom != null) {
            this.listViewSearch.entityVM.startDate = new Date(lFrom);
        }
        if (this.meta.defaultAddEnd) {
            let pre = this.meta.defaultAddEnd;
            this.listViewSearch.entityVM.endDate = new Date(Date.now() + pre * 24 * 3600 * 1000);
        }
        let lTo = window.localStorage.getItem("toDate" + this.meta.Id);
        if (lTo != null) {
            this.listViewSearch.entityVM.endDate = new Date(lTo);
        }
        this.addChild(this.listViewSearch);
        if (this.meta.virtualScroll && this.isMobile()) {
            Html.take(this.element)
                .div.className("list-wrapper")
                .div.className("list-container").render();
            this.dataTable = Html.context;
            Html.instance.div.className("list-header").tabIndex(-1).render();
            this.headerSection = new ListViewSection(null, Html.context);
            this.headerSection.parentElement = this.dataTable;
            this.addChild(this.headerSection);

            Html.take(this.dataTable).div.className("list-search").render();
            this.searchSection = new ListViewSection(null, Html.context);
            this.searchSection.entity = { Id: Uuid7.Guid() };
            this.searchSection.parentElement = this.dataTable;
            this.addChild(this.searchSection);

            Html.take(this.dataTable).div.className("list-empty").render();
            this.emptySection = new ListViewSection(null, Html.context);
            this.emptySection.parentElement = this.dataTable;
            this.addChild(this.emptySection);

            Html.take(this.dataTable).ul.className("list-body").render();
            this.mainSection = new ListViewSection(null, Html.context);
            this.mainSection.parentElement = this.dataTable;
            this.addChild(this.mainSection);

            if (!this.addContentRendered) {
                this.mainSection.element.addEventListener('contextmenu', this.bodyContextMenuHandler.bind(this));
                this.emptySection.element.addEventListener('contextmenu', this.bodyContextMenuHandler.bind(this));
                this.addContentRendered = true;
            }

            Html.take(this.dataTable).div.className("list-footer").render();
            this.footerSection = new ListViewSection(null, Html.context);
            this.footerSection.parentElement = this.dataTable;
            this.addChild(this.footerSection);

            Html.instance.endOf(".list-wrapper");
        }
        else {
            Html.take(this.element).div.className("table-wrapper").table.className("table").event('keydown', e => this.hotKeyF6Handler(e, e.keyCodeEnum()))
            this.dataTable = Html.context;
            Html.instance.tHead.tabIndex(-1).className("tb-header").render();
            this.headerSection = new ListViewSection(null, Html.context);
            this.headerSection.parentElement = this.dataTable;
            this.addChild(this.headerSection);
            Html.take(this.dataTable).tHead.className("tb-search").render();
            this.searchSection = new ListViewSection(null, Html.context);
            this.searchSection.entity = {
                Id: Uuid7.Guid()
            }
            this.searchSection.parentElement = this.dataTable;
            this.addChild(this.searchSection);
            Html.take(this.dataTable).tBody.className("tb-empty").render();
            this.emptySection = new ListViewSection(null, Html.context);
            this.emptySection.parentElement = this.dataTable;
            this.addChild(this.emptySection);
            Html.take(this.dataTable).tBody.className("tb-body").render();
            this.mainSection = new ListViewSection(null, Html.context);
            this.mainSection.parentElement = this.dataTable;
            this.addChild(this.mainSection);
            if (this.meta.showHotKey) {
                var seft = this;
                new Sortable(this.mainSection.element, {
                    animation: 150,
                    ghostClass: 'blue-background-class',
                    handle: '.status-bar',
                    autoScroll: true,
                    scrollSensitivity: 30,
                    scrollSpeed: 10,
                    onEnd: async function (evt) {
                        await seft.renderIndex2();
                    }
                });
            }
            if (!this.addContentRendered) {
                this.mainSection.element.addEventListener('contextmenu', this.bodyContextMenuHandler.bind(this));
                this.emptySection.element.addEventListener('contextmenu', this.bodyContextMenuHandler.bind(this));
                this.addContentRendered = true;
            }
            Html.take(this.dataTable).tFooter.className("tb-footer").render();
            this.footerSection = new ListViewSection(null, Html.context);
            this.footerSection.parentElement = this.dataTable;
            this.addChild(this.footerSection);
            Html.instance.endOf(".table-wrapper");
        }
        Html.take(this.element);
        this.renderPaginator();
    }

    clickHeader(e, header) {
        e.preventDefault();
        e.stopPropagation();
        if (this.editForm.devToolsElement) {
            this.editForm.updateMetaData(header);
        }
        let index = this.lastNumClick;
        const table = this.dataTable;
        if (this.lastNumClick != null) {
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
        this.lastThClick = th;
        this.lastNumClick = index;
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

    focusOutHeader(e, header) {
        let index = this.lastNumClick;
        const table = this.dataTable;
        if (this.lastNumClick !== null) {
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

    thHotKeyHandler(e, header) {
        var hasGroup = this.Header.some(x => !Utils.isNullOrWhiteSpace(x.groupName));
        if (this.meta.Focus || hasGroup) {
            return;
        }
        const keyCode = e.keyCode;
        if (keyCode === 39) {
            if (!Utils.isNullOrWhiteSpace(header.groupName)) {
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
            this.swapHeader(index, index + 1);
            this.swapSectionHeader(index, index + 1);
            this.updateHeaders();
            th.focus();
            this.updateStickyColumns();
        }
        else if (keyCode === 37) { // Left arrow
            if (!Utils.isNullOrWhiteSpace(header.groupName)) {
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
            this.swapHeader(index1, index1 - 1);
            this.swapSectionHeader(index1, index1 - 1);
            this.updateHeaders();
            th1.focus();
            this.updateStickyColumns();
        }
    }

    swapHeader(oldIndex, newIndex) {
        const item = this.Header[oldIndex];
        this.Header.splice(oldIndex, 1);
        this.Header.splice(newIndex, 0, item);
    }

    swapSectionHeader(oldIndex, newIndex) {
        const item = this.headerSection.Children[oldIndex];
        this.headerSection.Children.splice(oldIndex, 1);
        this.headerSection.Children.splice(newIndex, 0, item);
    }

    focusCell(e, header) {
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
     * @param {mouseEvent} e 
     * @returns 
     */
    actionKeyHandler(e, header, focusedRow, com, el, keyCode) {
        let fieldName = "";
        if ([keyCodeEnum.F8, keyCodeEnum.upArrow, keyCodeEnum.downArrow, keyCodeEnum.leftArrow, keyCodeEnum.rightArrow].includes(keyCode)) {
            e.preventDefault();
            e.stopPropagation();
            if (!com) {
                return;
            }
            fieldName = header.fieldName;
        }
        switch (keyCode) {
            case keyCodeEnum.F8:
                this.hardDeleteSelected();
                break;
            case keyCodeEnum.u:
                if (e.ctrlKey && this.meta.canAdd) {
                    e.preventDefault();
                    e.stopPropagation();
                    this.duplicateSelected();
                }
                break;
            case keyCodeEnum.r:
                if (e.ctrlKey) {
                    e.preventDefault();
                    e.stopPropagation();
                    this.actionFilter();
                }
                break;
            case keyCodeEnum.upArrow:
                this.moveFocusUp(fieldName);
                break;
            case keyCodeEnum.downArrow:
                this.moveFocusDown(fieldName);
                break;
            case keyCodeEnum.leftArrow:
                this.moveFocusLeft(com);
                break;
            case keyCodeEnum.rightArrow:
                this.moveFocusRight(com);
                break;
            case keyCodeEnum.c:
                if (e.ctrlKey) {
                    const selected = Array.from(this.element.querySelectorAll('td.cell-matrix'));
                    if (selected.length === 0) {
                        return;
                    }
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

    getHeaderMatrix(tableElement) {
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

    moveFocusUp(fieldName) {
        let currentItem = this.getItemFocus();
        if (!currentItem) {
            currentItem = this.gridViewItemEmpty;
        }
        if (!(this.meta.isMultiple && this.componentType == "GridView")) {
            currentItem.Selected = false;
        }
        var nextIndex = -2;
        if (currentItem.emptyRow) {
            nextIndex = this.allListViewItem.length - 1;
        }
        else {
            var indexCurrent = this.allListViewItem.indexOf(currentItem);
            nextIndex = indexCurrent - 1;
            if (this.allListViewItem[nextIndex] && this.allListViewItem[nextIndex].groupRow) {
                nextIndex = nextIndex - 1;
            }
            if (indexCurrent == 0) {
                if (this.meta.canAdd) {
                    nextIndex = -2;
                }
                else {
                    nextIndex = this.allListViewItem.length - 1;
                }
            }
            else {
                if (nextIndex == -1) {
                    if (this.meta.canAdd) {
                        nextIndex = -2;
                    }
                    else {
                        nextIndex = this.allListViewItem.length - 1;
                    }
                }
            }
        }
        if (nextIndex == -2) {
            this.gridViewItemEmpty.Focused = true;
            var com = this.gridViewItemEmpty.Children.find(x => x.Meta.fieldName == fieldName);
            com.parentElement.focus();
            com.Focus();
            if (com.element.tagName === 'INPUT' || com.element.tagName === 'TEXTAREA') {
                com.element.select();
            }
        }
        else {
            this.allListViewItem[nextIndex].Focused = true
            if (!(this.meta.isMultiple && this.componentType == "GridView")) {
                this.allListViewItem[nextIndex].Selected = true;
            }
            var com = this.allListViewItem[nextIndex].Children.find(x => x.Meta.fieldName == fieldName);
            com.parentElement.focus();
            com.Focus();
            if (com.element.tagName === 'INPUT' || com.element.tagName === 'TEXTAREA') {
                com.element.select();
            }
        }
    }

    moveFocusDown(fieldName) {
        let currentItem = this.getItemFocus();
        if (!currentItem) {
            currentItem = this.gridViewItemEmpty;
        }
        if (!(this.meta.isMultiple && this.componentType == "GridView")) {
            currentItem.Selected = false;
        }
        var nextIndex = -2;
        if (currentItem.emptyRow) {
            nextIndex = 0;
            if (this.allListViewItem[nextIndex] && this.allListViewItem[nextIndex].groupRow) {
                nextIndex = nextIndex + 1;
            }
        }
        else {
            var indexCurrent = this.allListViewItem.indexOf(currentItem);
            nextIndex = indexCurrent + 1;
            if (this.allListViewItem[nextIndex] && this.allListViewItem[nextIndex].groupRow) {
                nextIndex = nextIndex + 1;
            }
            if (indexCurrent == this.allListViewItem.length - 1) {
                if (this.meta.canAdd) {
                    nextIndex = -2;
                }
            }
            else {
                if (!this.allListViewItem[nextIndex]) {
                    if (this.meta.canAdd) {
                        nextIndex = -2;
                    }
                    else {
                        nextIndex = 0;
                    }
                }
            }
        }
        if (nextIndex == -2) {
            this.gridViewItemEmpty.Focused = true;
            var com = this.gridViewItemEmpty.Children.find(x => x.Meta.fieldName == fieldName);
            com.parentElement.focus();
            com.Focus();
            if (com.element.tagName === 'INPUT' || com.element.tagName === 'TEXTAREA') {
                com.element.select();
            }
        }
        else {
            if (!this.allListViewItem[nextIndex]) {
                nextIndex = 0;
            }
            this.allListViewItem[nextIndex].Focused = true;
            if (!(this.meta.isMultiple && this.componentType == "GridView")) {
                this.allListViewItem[nextIndex].Selected = true;
            }
            var com = this.allListViewItem[nextIndex].Children.find(x => x.Meta.fieldName == fieldName);
            com.parentElement.focus();
            com.Focus();
            if (com.element.tagName === 'INPUT' || com.element.tagName === 'TEXTAREA') {
                com.element.select();
            }
        }
    }

    moveFocusLeft(com) {
        let currentItem = this.getItemFocus();
        if (!currentItem) {
            currentItem = this.gridViewItemEmpty;
        }
        var ele = this.preElement(com.element.closest('td').previousElementSibling);
        let leftItem = currentItem.Children.find(x => x.element.closest('td') === ele);
        if (!leftItem) {
            return;
        }
        leftItem.element.closest("td").focus();
        leftItem.Focus();
        if (leftItem.Meta.Editable && !leftItem.Disabled) {
            if (leftItem.element instanceof hTMLInputElement) {
                leftItem.element.selectionStart = 0;
                leftItem.element.selectionEnd = leftItem.getValueText().length;
            }
        }
    }

    nextElement(ele) {
        if (ele.style.display == "none") {
            return this.nextElement(ele.nextElementSibling);
        }
        else {
            return ele;
        }
    }

    preElement(ele) {
        if (ele.style.display == "none") {
            return this.preElement(ele.previousElementSibling);
        }
        else {
            return ele;
        }
    }

    moveFocusRight(com) {
        let currentItem = this.getItemFocus();
        if (!currentItem) {
            currentItem = this.gridViewItemEmpty;
        }
        var ele = this.nextElement(com.element.closest('td').nextElementSibling);
        let leftItem = currentItem.Children.find(x => x.element.closest('td') === ele);
        if (!leftItem) {
            return;
        }
        leftItem.element.closest("td").focus();
        leftItem.Focus();
        if (leftItem.Meta.Editable) {
            if (leftItem.element instanceof hTMLInputElement) {
                leftItem.element.selectionStart = 0;
                leftItem.element.selectionEnd = leftItem.getValueText().length;
            }
        }
    }

    hotKeyF6Handler(e, keyCode) {
        let currentItem = this.getItemFocus();
        if (!currentItem) {
            currentItem = this.gridViewItemEmpty;
        }
        switch (keyCode) {
            case keyCodeEnum.F1:
                e.preventDefault();
                e.stopPropagation();
                this.toggleAll();
                break;
            default:
                break;
        }
        if (!currentItem || !currentItem.Children) {
            return;
        }
        let com = currentItem.Children.find(x => x.Meta.Id === this.lastComponentFocus?.Id);
        this.actionKeyHandler(e, this.lastComponentFocus, currentItem, com, com.element.closest('td'), keyCode);
    }

    async addRow(rowData, index = 0, singleAdd = true) {
        let rowSection = await super.addRow(rowData, index, singleAdd);
        this.updateStickyColumns();
        this.renderIndex();
        return rowSection;
    }
    /**
     * Adds multiple rows to the ListView.
     * @param {Array<object>} rows An array of objects to be added as rows.
     * @param {number} index The starting index to add new rows.
     * @returns {Promise<Array<ListViewItem>>} A promise that resolves to an array of ListViewItem instances.
     */
    async addRows(rowsData) {
        let listItem = [];
        await this.loadMasterData(rowsData);
        await Promise.all(rowsData.map(async x => {
            listItem.push(this.addRow(x, null, false));
        }));
        await this.dispatchCustomEvent(this.meta.events, EventType.Change, this);
        this.renderIndex();
        this.addSummaries();
        this.domLoaded();
        return listItem;
    }

    addNewEmptyRow() {
        if (this.meta && this.meta.addRowExp) {
            var addFn = Utils.isFunction(this.meta.addRowExp, false, this);
            if (!addFn) {
                return;
            }
        }
        if (this.disabled || !this.meta.canAdd || (this.emptySection && this.emptySection.Children.length > 0)) {
            return;
        }
        let emptyRowData = {};
        emptyRowData[this.idField] = Uuid7.newGuid();
        this.gridViewItemEmpty = this.renderRowData(this.Header, emptyRowData, this.emptySection, null, true);
        if (!this.meta.topEmpty) {
            this.dataTable.insertBefore(this.mainSection.element, this.emptySection.element);
        } else {
            this.dataTable.insertBefore(this.emptySection.element, this.mainSection.element);
        }
        this.gridViewItemEmpty.Children.forEach(x => x.setRequired());
        this.dispatchCustomEvent(this.meta.events, 'afterEmptyRowCreated', emptyRowData).then(() => {
            this.updateStickyColumns();
        });
    }

    async applyFilter() {
        this.dataTable.parentElement.scrollTop = 0;
        await this.reloadData(this.cacheHeader = true);
    }

    renderContent() {
        if (!this.loadRerender) {
            this.Rerender();
        }
        this.addSections();
        let viewPort = this.getViewPortItem();
        this.formattedRowData = this.meta.localRender ? this.meta.localData : this.rowData.Data;
        if (!this.formattedRowData || this.formattedRowData.length === 0) {
            this.mainSection.disposeChildren();
            if (!this._hasFirstLoad) {
                this.dispatchCustomEvent(this.meta.events, 'firstLoad', this).then();
                this._hasFirstLoad = true;
            }
            this.domLoaded();
            return;
        }
        if (this.mainSection.Children.length > 0) {
            if (this._hasFirstLoad) {
                this.updateExistRowsWrapper(false, 0, viewPort);
            }
            if (!this._hasFirstLoad) {
                this.dispatchCustomEvent(this.meta.events, 'firstLoad', this).then();
                this._hasFirstLoad = true;
            }
            this.domLoaded();
            this.renderIndex();
            return;
        }
        this.mainSection.Show = false;
        for (let index = 0; index < this.formattedRowData.length; index++) {
            const rowData = this.formattedRowData[index];
            Html.take(this.mainSection.element);
            this.renderRowData(this.Header, rowData, this.mainSection, index);
        }
        this.mainSection.Show = true;
        this.contentRendered();
        this.domLoaded();
        this.updateStickyColumns();
    }

    clearSelection() {
        this.element.querySelectorAll(".cell-matrix").forEach(x => x.classList.remove("cell-matrix"));
    }

    updateExistRowsWrapper(dirty, skip, viewPort) {
        if (!this._hasFirstLoad) {
            this._hasFirstLoad = true;
            return;
        }
        this.updateExistRows(dirty);
    }

    updateExistRows(dirty) {
        const updatedData = this.formattedRowData.slice();
        const dataSections = this.allListViewItem.slice(0, updatedData.length);
        dataSections.forEach((child, index) => {
            child.entity = updatedData[index];
            this.flattenChildren(child).forEach(x => {
                x.Entity = updatedData[index];
            });
            child.updateView();
        });

        const shouldAddRow = this.allListViewItem.length <= updatedData.length;
        if (shouldAddRow) {
            updatedData.slice(dataSections.length).forEach(newRow => {
                this.renderRowData(this.Header, newRow, this.mainSection);
            });
        } else {
            this.mainSection.Children.slice(updatedData.length).forEach(x => x.Dispose());
        }

        if (dirty !== undefined) {
            this.Dirty = dirty;
        }
    }

    flattenChildren(component) {
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
     * @typedef {import("./groupGridView.js").groupRowData} GroupRowData
     * @param {import("./models/component.js").Component[]} headers
     * @param {GroupRowData} row - Group row data
     * @param {ListViewSection | import("./groupViewItem.js").groupViewItem} section
     */
    renderRowData(headers, row, section, index = null, emptyRow = false) {
        const tbody = section.element;
        Html.take(tbody);
        if (this.meta.virtualScroll && this.isMobile()) {
            var rowSection = new ListRowItem(ElementType.li);
            rowSection.emptyRow = emptyRow;
            rowSection.entity = row;
            rowSection.parentElement = tbody;
            rowSection.preQueryFn = this._preQueryFn;
            rowSection.listView = this;
            rowSection.meta = this.meta
            section.addChild(rowSection, index);
            var tr = Html.context;
            tr.tabIndex = -1;
            if (index) {
                if (index >= tr.parentElement.children.length || index < 0) {
                    index = 0;
                }
                tr.parentElement.insertBefore(tr, tr.parentElement.children[index]);
            }
            var newHeaders = headers.filter(x => x.virtualScroll).sort((a, b) => (a.Order ?? 0) - (b.Order ?? 0));
            if (newHeaders.length > 0) {
                for (let index2 = 0; index2 < newHeaders.length; index2++) {
                    const header = newHeaders[index2];
                    rowSection.renderTableCell(row, header, null, index, index2);
                }
            }
            if (emptyRow) {
                this.Children.forEach(x => x.alwaysLogHistory = true);
            }
            rowSection.Children.forEach(x => {
                x.prepareUpdateView();
            });
            return rowSection;
        }
        else {
            var rowSection = new GridViewItem(ElementType.tr);
            rowSection.emptyRow = emptyRow;
            rowSection.entity = row;
            rowSection.parentElement = tbody;
            rowSection.preQueryFn = this._preQueryFn;
            rowSection.listView = this;
            rowSection.meta = this.meta
            section.addChild(rowSection, index);
            var tr = Html.context;
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
                    rowSection.renderTableCell(row, header, null, index, index2);
                }
            }
            if (emptyRow) {
                this.Children.forEach(x => x.alwaysLogHistory = true);
            }
            rowSection.Children.forEach(x => {
                x.prepareUpdateView();
            });
            return rowSection;
        }
    }

    updateValidation() {
        return;
    }

    intWaitingSticky = 0;
    intWaitingSticky = 0;
    updateStickyColumns() {
        const stickyColumns = this.Header
            .map((item, index) => item.Frozen ? index : -1)
            .filter(index => index !== -1);
        stickyColumns.sort((a, b) => a - b);
        this.dataTable && this.dataTable.querySelectorAll("th, td").forEach((cell) => {
            cell.classList.remove("sticky-column");
            cell.style.left = "";
            cell.classList.remove("sticky-column-right"); // reset phải
            cell.style.right = "";
        });
        let leftOffset = 0;
        stickyColumns.forEach((index) => {
            this.headerSection.element.querySelectorAll(
                `tr:first-child th:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            this.searchSection.element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            this.mainSection.element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            this.emptySection.element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            this.footerSection.element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            leftOffset += this.headerSection.element.querySelector(
                `tr:first-child th:nth-child(${index + 1})`
            )?.offsetWidth || 0;
        });

        // --- Thêm xử lý frozenRight ---
        const stickyRightColumns = this.Header
            .map((item, index) => item.frozenRight ? index : -1)
            .filter(index => index !== -1)
            .sort((a, b) => b - a); // xử lý từ phải qua trái

        let rightOffset = 0;
        stickyRightColumns.forEach((index) => {
            const cellWidth = this.headerSection.element.querySelector(
                `tr:first-child th:nth-child(${index + 1})`
            )?.offsetWidth || 0;

            [this.headerSection, this.searchSection, this.mainSection, this.emptySection, this.footerSection]
                .forEach(section => {
                    section.element.querySelectorAll(
                        `tr td:nth-child(${index + 1}), tr th:nth-child(${index + 1})`
                    ).forEach((cell) => {
                        cell.classList.add("sticky-column-right");
                        cell.style.right = `${rightOffset}px`;
                    });
                });

            rightOffset += cellWidth;
        });
    }

    updateStickySummary() {
        const stickyColumns = this.Header
            .map((item, index) => item.Frozen ? index : -1)
            .filter(index => index !== -1);
        stickyColumns.sort((a, b) => a - b);
        this.footerSection.element.querySelectorAll("th, td").forEach((cell) => {
            cell.classList.remove("sticky-column");
            cell.style.left = "";
            cell.classList.remove("sticky-column-right");
            cell.style.right = "";
        });

        let leftOffset = 0;
        stickyColumns.forEach((index) => {
            this.footerSection.element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column");
                cell.style.left = `${leftOffset}px`;
            });
            leftOffset += this.headerSection.element.querySelector(
                `tr:first-child th:nth-child(${index + 1})`
            )?.offsetWidth || 0;
        });

        // --- Thêm xử lý frozenRight ---
        const stickyRightColumns = this.Header
            .map((item, index) => item.frozenRight ? index : -1)
            .filter(index => index !== -1)
            .sort((a, b) => b - a);

        let rightOffset = 0;
        stickyRightColumns.forEach((index) => {
            const cellWidth = this.headerSection.element.querySelector(
                `tr:first-child th:nth-child(${index + 1})`
            )?.offsetWidth || 0;

            this.footerSection.element.querySelectorAll(
                `td:nth-child(${index + 1})`
            ).forEach((cell) => {
                cell.classList.add("sticky-column-right");
                cell.style.right = `${rightOffset}px`;
            });

            rightOffset += cellWidth;
        });
    }

    intChangeSummary = 0;
    addSummaries() {
        window.clearTimeout(this.intChangeSummary);
        this.intChangeSummary = window.setTimeout(() => {
            if (this.Header.some(x => !Utils.isNullOrWhiteSpace(x.groupFormat))) {
                this.allListViewItem.filter(x => x.groupRow).forEach(item => {
                    item.Children.filter(x => !Utils.isNullOrWhiteSpace(x.Meta.groupFormat)).forEach(cell => {
                        item.Cell = cell;
                        var groupText = Utils.isFunction(cell.Meta.groupFormat, false, item);
                        cell.element.innerHTML = groupText;
                    });
                })
            }
            const sums = this.Header.filter(x => !Utils.isNullOrWhiteSpace(x.Summary));
            if (!sums || sums.length == 0 || this.Item.length == 0) {
                if (this.footerSection && this.footerSection.element.firstChild) {
                    this.footerSection.element.firstChild.childNodes.forEach(x => x.innerHTML = '');
                }
                return;
            }
            const summaryElements = this.mainSection.element.querySelectorAll(`.${this.summaryClass}`);
            summaryElements.forEach(x => x.remove());
            const count = new Set(sums.map(x => x.Summary)).size;
            sums.forEach(header => {
                this.renderSummaryRow(header, this.Header, this.footerSection.element, count);
            });
            this.updateStickySummary();
        }, 100);
    }

    renderSummaryRow(sum, headers, footer, count) {
        let tr = this.createSummaryTableRow(sum, footer, count);
        if (!tr) {
            return;
        }
        tr.classList.add("summary");
        if (!headers.includes(sum)) {
            this.clearSummaryContent(tr);
            return;
        }
        this.calcSumCol(sum, headers, tr);
    }
    /**
     * 
     * @param {Component} sum 
     * @param {hTMLTableSectionElement} footer 
     * @param {Number} count 
     * @returns 
     */
    createSummaryTableRow(sum, footer, count) {
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
         * @type {hTMLTableRowElement}
         */
        var firstChild = null;
        if (this.meta.editable) {
            if (this.meta.canAdd) {
                firstChild = this.emptySection.element.firstChild;
            }
            else {
                firstChild = this.mainSection.Children.find(x => !x.groupRow).element;
            }
        }
        else {
            firstChild = this.mainSection.Children.find(x => !x.groupRow).element;
        }
        if (!firstChild) {
            if (existSumRow != null) {
                existSumRow.remove();
            }
            return null;
        }
        if (summaryRows.length >= count) {
            this.updateTdClassFromFirstChild(existSumRow, firstChild);
            return existSumRow;
        }
        var result = firstChild.cloneNode(true);
        footer.appendChild(result);
        result.childNodes.forEach(x => x.innerHTML = null);
        return result;
    }

    updateTdClassFromFirstChild(existSumRow, firstChild) {
        var firstChildTds = firstChild.querySelectorAll("td");
        var existSumRowTds = existSumRow.querySelectorAll("td");
        for (var i = 0; i < firstChildTds.length && i < existSumRowTds.length; i++) {
            existSumRowTds[i].className = "text-right";
        }
    }

    calcSumCol(header, headers, tr) {
        const index = headers.indexOf(header);
        const cellVal = tr.cells[index];
        if (header.isTotal && header.componentType !== "Number") {
            cellVal.textContent = this.Item.length.toString();
        }
        if (header.isTotal) {
            const sum = this.totalHeaders.reduce((a, b) => a.plus(b), new Decimal(0));
            var pre = header.groupTypeId ? parseInt(LangSelect._webConfig[header.groupTypeId]) : header.Precision;
            if (this.meta.Frozen) {
                cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toDP(0).toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
            }
            else {
                cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
            }
        }
        else {
            if (this.meta.isMultiple) {
                const sum = this.getSelectedRows().reduce((a, b) => a.plus(new Decimal(b[header.fieldName] || 0)), new Decimal(0))
                var pre = header.groupTypeId ? parseInt(LangSelect._webConfig[header.groupTypeId]) : header.Precision;
                if (this.meta.Frozen) {
                    cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toDP(0).toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
                }
                else {
                    cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
                }
            }
            else {
                const sum = this.allListViewItem.filter(x => !x.groupRow).map(x => x.entity).reduce((a, b) => a.plus(new Decimal(b[header.fieldName] || 0)), new Decimal(0))
                var pre = header.groupTypeId ? parseInt(LangSelect._webConfig[header.groupTypeId]) : header.Precision;
                if (this.meta.Frozen) {
                    cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toDP(0).toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
                }
                else {
                    cellVal.textContent = this.Decimal(0).eq(sum) ? "" : sum.toFixed(pre || 0).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
                }
            }
        }
    }

    resetSummaryRow(tr, colSpan) {
        for (let i = 1; i < colSpan; i++) {
            if (tr.cells[0]) {
                tr.cells[0].remove();
            }
        }
        this.clearSummaryContent(tr);
    }

    clearSummaryContent(tr) {
        Array.from(tr.cells).forEach(cell => {
            cell.innerHTML = '';
        });
    }

    /**
     * @typedef {import('./listViewItem.js').listViewItem} ListViewItem
     * Handles custom events based on row changes, applying data updates and managing component state.
     * @param {object} rowData The data of the row that triggered the change.
     * @param {ListViewItem} rowSection The ListViewItem corresponding to the row.
     * @param {import("editableComponent.js").observableArgs} observableArgs Additional arguments or data relevant to the event.
     * @param {import("models/observable.js").editableComponent} [component=null] Optional component that might be affected by the row change.
     * @returns {Promise<boolean>} A promise that resolves to a boolean indicating success or failure of the event handling.
     */
    async rowChangeHandler(rowData, rowSection, observableArgs, component = null) {
        if (rowSection.emptyRow && observableArgs.evType === EventType.Change) {
            if (!Utils.isNullOrWhiteSpace(this.meta.defaultVal)) {
                var rsObj = Utils.isFunction(this.meta.defaultVal, false, this);
                if (rsObj) {
                    Object.getOwnPropertyNames(rsObj).forEach(x => {
                        rowSection.entity[x] = rsObj[x];
                    });
                }
            }
            rowSection.entity["insertedBy"] = this.Token.userId;
            await this.dispatchCustomEvent(this.meta.events, customEventType.beforeCreated, rowSection, rowData, this);
            if (!Utils.isNullOrWhiteSpace(this.meta.groupBy)) {
                let keys = this.meta.groupBy.split(",");
                rowSection.entity[this._groupKey] = keys.map(key => rowSection.entity[key]).join(" ");
            }
            let rs;
            if (this.meta.isRealtime) {
                rs = await rowSection.patchUpdateOrCreate();
                Object.assign(rowSection.entity, rs);
                if (this.meta.componentType == "virtualGrid") {
                    this.cacheData.push(rs);
                }
                this.Dirty = false;
            } else {
                Object.assign(rowSection.entity, rs);
                this.Dirty = true;
            }
            await this.loadMasterData([rowSection.entity]);
            rowSection.updateView(true);
            rowSection.emptyRow = false;
            this.moveEmptyRow(rowSection);
            this.emptySection.Children = [];
            this.addNewEmptyRow();
            this.clearSelected();
            rowSection.Selected = true;
            rowSection.Focused = true;
            this.lastListViewItem = rowSection;
            this.renderIndex();
            await this.dispatchCustomEvent(this.meta.events, customEventType.afterCreated, rowSection, rowData, this);
        }
        else {
            if (!Utils.isNullOrWhiteSpace(this.meta.groupBy)) {
                let keys = this.meta.groupBy.split(",");
                rowSection.entity[this._groupKey] = keys.map(key => rowSection.entity[key]).join(" ");
            }
            if (rowSection.groupSection) {
                if (rowSection.entity[this._groupKey] != rowSection.groupSection.Key) {
                    const index = rowSection.groupSection.childrenItems.indexOf(rowSection);
                    if (index > -1) {
                        rowSection.groupSection.childrenItems.splice(index, 1);
                    }
                    if (rowSection.groupSection.childrenItems.length == 0) {
                        const index1 = this.allListViewItem.indexOf(rowSection.groupSection);
                        this.allListViewItem.splice(index1, 1);
                        rowSection.groupSection.Dispose();
                    }
                    this.moveGroupRow(rowSection);
                    this.clearSelected();
                }
                else {
                    var groupText = Utils.isFunction(this.meta.groupFormat, false, rowSection.groupSection);
                    if (rowSection.groupSection.groupText) {
                        rowSection.groupSection.groupText.innerHTML = groupText;
                    }
                }
                this.renderIndex();
            }
        }
        if (this.lastComponentFocus.componentType == "Dropdown" && rowData[this.lastComponentFocus.fieldName]) {
            window.setTimeout(() => {
                const headers = this.Header.filter(x => x.virtualScroll).orderBy(x => x.Order);
                const currentComponent = headers.find(y => y.Id === this.lastComponentFocus.Id);
                const index = headers.indexOf(currentComponent);
                if (headers.length > index + 1) {
                    const nextGrid = headers[index + 1];
                    const nextComponent = rowSection.Children.find(y => y.Meta.Id === nextGrid.Id);
                    if (nextComponent) {
                        nextComponent.Focus();
                    }
                }
            }, 200);
        }
        if (component && component.componentType == "GridView") {
            await this.dispatchEvent(component.Meta.Events, observableArgs.evType, this, rowSection, rowData);
        }
        await this.dispatchEvent(this.meta.events, observableArgs.evType, this, rowSection, rowData);
        if (observableArgs.evType === EventType.Change) {
            this.renderIndex();
            if (this.meta.isSumary) {
                this.addSummaries();
            }
            this.lastListViewItem = rowSection;
        }
    }
    /**
     * @param {ListViewItem} rowSection
     */
    moveGroupRow(rowSection) {
        let groupSection = this.allListViewItem.find(group => group.groupRow && group.Key === rowSection.entity[this._groupKey]);
        var currentIndex = this.allListViewItem.indexOf(rowSection);
        this.allListViewItem.splice(currentIndex, 1);
        if (groupSection) {
            rowSection.Parent = this.mainSection;
            rowSection.listViewSection = this.mainSection;
            rowSection.groupSection = groupSection;
            rowSection.element.classList.add("group-detail");
            var lastChild = groupSection.childrenItems[groupSection.childrenItems.length - 1];
            var index = this.allListViewItem.indexOf(lastChild);
            if (this.allListViewItem.length == index + 1) {
                this.mainSection.element.appendChild(rowSection.element);
            }
            else {
                this.mainSection.element.insertBefore(rowSection.element, this.allListViewItem[index + 1].element);
            }
            this.allListViewItem.splice(index + 1, 0, rowSection);
            groupSection.childrenItems.push(rowSection);
            this.Dirty = true;
            return rowSection;
        }
        else {
            Html.take(this.mainSection);
            groupSection = new GroupViewItem(ElementType.tr);
            groupSection.Key = rowSection.entity[this._groupKey];
            groupSection.entity = rowSection.entity;
            groupSection.parentElement = this.mainSection.element;
            groupSection.listViewSection = true;
            groupSection.listViewSection = this.mainSection;
            groupSection.listView = this;
            this.mainSection.addChild(groupSection);
            groupSection.element.tabIndex = -1;
            var groupText = Utils.isFunction(this.meta.groupFormat, false, rowSection);
            Html.instance.tData.className("status-cell").tabIndex(-1).event(EventType.Click, () => groupSection.showChildren1 = !groupSection.showChildren1).icon("fal fa-square");
            groupSection.Chevron = Html.context;
            Html.instance.end.end.tData.event(EventType.Click, () => this.dispatchClick(first))
                .event(EventType.dblClick, () => this.dispatchDblClick(first))
                .div.className("d-flex");
            groupSection.groupText = Html.context;
            Html.instance.innerHTML(groupText);
            Html.instance.endOf(ElementType.td);
            this.Header.slice(2).forEach(item => {
                Html.instance.tData.className("data-summary").style("font-weight:600");
                var sec = new Section(null, Html.context);
                sec.meta = item;
                groupSection.addChild(sec);
                Html.instance.endOf(ElementType.td);
            });
            Html.instance.endOf(ElementType.tr);
            Html.take(this.mainSection.element);
            rowSection.element.classList.add("group-detail");
            groupSection.childrenItems.push(rowSection);
            rowSection.groupSection = groupSection;
            var lastChild = groupSection.childrenItems[groupSection.childrenItems.length - 1];
            var index = this.allListViewItem.indexOf(groupSection);
            if (this.allListViewItem.length == index + 1) {
                this.mainSection.element.appendChild(rowSection.element);
            }
            else {
                this.mainSection.element.insertBefore(rowSection.element, this.allListViewItem[index + 1].element);
            }
            this.allListViewItem.splice(index + 1, 0, rowSection);
            return rowSection;
        }
    }
    /**
     * @param {ListViewItem} rowSection
     */
    moveEmptyRow(rowSection) {
        if (this.meta.topEmpty) {
            if (!this.mainSection.Children.includes(this.emptySection.firstChild)) {
                this.mainSection.Children.unshift(this.emptySection.firstChild);
            }
            this.mainSection.element.prepend(this.emptySection.element.firstElementChild);
        } else {
            this.mainSection.element.appendChild(this.emptySection.element.firstElementChild);
            if (!this.mainSection.Children.includes(this.emptySection.firstChild)) {
                this.mainSection.Children.push(this.emptySection.firstChild);
            }
        }
        if (this.meta.isRealtime) {
            rowSection.element.classList.remove("new-row");
        }
        rowSection.Parent = this.mainSection;
        rowSection.listViewSection = this.mainSection;
    }
    hideColumn(...param) {
        if (this.headerSection) {
            this.headerSection.Children.forEach(column => {
                if (param.includes(column.Meta.fieldName)) {
                    if (column.element.style.display == "") {
                        var parentElement = this.thGroup.find(x => x.groupName == column.Meta.groupName);
                        if (parentElement) {
                            var col = (parseInt(parentElement.element.getAttribute("colspan")) - 1);
                            parentElement.element.setAttribute("colspan", col.toString());
                            if (col == 0) {
                                parentElement.element.style.display = "none";
                            }
                            else {
                                parentElement.element.style.display = "";
                            }
                        }
                    }
                    column.element.style.display = "none";
                }
                else {
                    if (column.element.style.display == "none") {
                        var parentElement = this.thGroup.find(x => x.groupName == column.Meta.groupName);
                        if (parentElement) {
                            var col = parseInt(parentElement.element.getAttribute("colspan")) + 1
                            parentElement.element.setAttribute("colspan", (col).toString());
                            parentElement.element.style.display = "";
                        }
                    }
                    column.element.style.display = "";
                }
            })
        }
        if (this.gridViewItemEmpty) {
            this.gridViewItemEmpty.Children.forEach(column => {
                if (param.includes(column.Meta.fieldName)) {
                    column.element.closest("td").style.display = "none";
                }
                else {
                    column.element.closest("td").style.display = "";
                }
            })
        }
        if (this.footerSection && this.footerSection.element.firstChild) {
            this.footerSection.element.firstChild.childNodes.forEach(column => {
                if (param.includes(column.getAttribute("data-field"))) {
                    column.style.display = "none";
                }
                else {
                    column.style.display = "";
                }
            })
        }
        if (this.allListViewItem) {
            this.allListViewItem.forEach(row => {
                if (!row.element) {
                    return;
                }
                row.element.childNodes.forEach(column => {
                    if (param.includes(column.getAttribute("data-field"))) {
                        column.style.display = "none";
                    }
                    else {
                        column.style.display = "";
                    }
                });
                if (row.groupRow) {
                    this.moveElementToSecondVisibleTd(row.groupText, row.element);
                }
            })
        }
        this.updateStickyColumns();
    }

    moveElementToSecondVisibleTd(ele, row) {
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

    thGroup = [];
    renderTableHeader(headers) {
        if (this.meta.virtualScroll && this.isMobile()) {
            return;
        }
        if (!headers || headers.length == 0) {
            headers = this.Header;
        }
        if (headers.Count != this.Header.Count) {
            this.filterColumns(headers);
        }
        if (this.headerSection.element === null) {
            this.addSections();
        }
        headers.forEach((x, index) => x.postOrder = index);
        this.headerSection.disposeChildren();
        const anyGroup = headers.some(x => x.groupName && !Utils.isNullOrWhiteSpace(x.groupName));
        Html.take(this.headerSection.element).clear().tRow.forEach(headers, (header, index) => {
            if (anyGroup && !Utils.isNullOrWhiteSpace(header.groupName)) {
                if (header !== headers.find(x => x.groupName === header.groupName)) {
                    return;
                }
                Html.th.attr("component", header.componentType || "Number").colSpan(headers.filter(x => x.groupName === header.groupName).length);
                this.thGroup.push({
                    groupName: header.groupName,
                    Element: Html.context
                });
                var groupCom = this.loadGridPolicy().find(x => x.groupName == header.groupName && x.topEmpty);
                if (groupCom) {
                    Html.event(EventType.contextMenu, this.headerContextMenu.bind(this), groupCom)
                    var com = ComponentFactory.getComponent(groupCom, this.editForm, null, true);
                    com.parentElement = Html.context;
                    this.editForm.addChild(com);
                    this.editForm.childCom.push(com);
                    if (groupCom.Disabled) {
                        com.setDisabled(true);
                    }
                    Html.endOf("th");
                }
                else {
                    Html.iHtml(header.groupName, this.editForm.meta.label).end.render();
                }
                return;
            }
            Html.th.attr("component", header.componentType || "Number")
                .tabIndex(-1).width(header.autoFit ? "auto" : header.Width)
                .style(`${header.Style};min-width: ${header.minWidth}; max-width: ${header.maxWidth}`)
                .textAlign('center')
                .event(EventType.contextMenu, this.headerContextMenu.bind(this), header)
                .event(EventType.focusOut, e => this.focusOutHeader(e, header))
                .event(EventType.keyDown, e => this.thHotKeyHandler(e, header));
            var sec = new Section(null, Html.context);
            sec.meta = header;
            this.headerSection.addChild(sec);
            if (anyGroup && (!header.groupName || header.groupName === "")) {
                Html.instance.rowSpan(2);
            }
            if (!anyGroup && this.Header.some(x => x.groupName && x.groupName.length)) {
                Html.instance.className("header-group");
            }
            if (header.statusBar) {
                Html.instance.a.icon("fal fa-level-down").event(EventType.Click, this.toggleAll.bind(this)).end.end.render();
            }
            if (header.Icon) {
                Html.instance.icon(header.Icon).margin(Direction.Right, 0).end.render();
            } else if (!header.statusBar) {
                if (!anyGroup) {
                    Html.instance.event(EventType.Click, e => this.clickHeader(e, header))
                }
                else {
                    Html.instance.event(EventType.Click, (e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        if (this.editForm.devToolsElement) {
                            this.editForm.updateMetaData(header);
                        }
                    });
                }
                Html.instance.iHtml(header.Label, this.editForm.meta.label).render();
            }
            if (header.componentType === "Number") {
                Html.instance.div.end.render();
                Html.instance.span.style("display: block;").end.render();
            }
            if (header.Description) {
                Html.instance.attr("title", header.Description);
            }
            if (!header.statusBar && header.componentType != 'Button' && header.fieldName) {
                Html.instance.div.className("th-options").i.className("fal fa-ellipsis-v").end.end.render();
                this.createResizableTable(Html.context);
            }
            Html.instance.endOf(ElementType.th);
        }).endOf(ElementType.tr).render();

        if (anyGroup) {
            Html.instance.tRow.forEach(headers, (header, index) => {
                if (anyGroup && !Utils.isNullOrWhiteSpace(header.groupName)) {
                    Html.instance.th.attr("component", header.componentType || "Number").style(`min-width: ${header.minWidth}; max-width: ${header.maxWidth}`)
                        .textAlign(header.textAlignEnum)
                        .event(EventType.contextMenu, this.headerContextMenu.bind(this), header)
                        .iHtml(header.Label, this.editForm.meta.label);
                    var sec = new Section(null, Html.context);
                    sec.meta = header;
                    this.headerSection.addChild(sec);
                    Html.instance.endOf(ElementType.th);
                }
            });
        }
        this.headerSection.Children = this.headerSection.Children.sort((a, b) => a.Meta.postOrder - b.Meta.postOrder);
        if (this.meta.canSearch) {
            var headerHeight = this.headerSection.element.clientHeight;
            this.searchSection.element.style.top = `${headerHeight}px`;
            this.searchSection.element.style.position = "sticky";
            Html.take(this.searchSection.element).clear().tRow.forEach(headers, (header, index) => {
                Html.tData.tabIndex(-1);
                if (header.statusBar) {
                    Html.instance.style(`top:${headerHeight}px`).span.className("fal fa-search").end;
                }
                else {
                    Html.div.className("input-group-button").tabIndex(-1);
                }
                var sec = new Section(null, Html.context);
                sec.meta = header;
                if (header.fieldName == "Id") {
                    this.searchSection.addChild(sec);
                }
                else {
                    switch (header.componentType) {
                        case "Dropdown":
                        case "Input":
                            var txtSearch = new Textbox({
                                fieldName: header.fieldName,
                                Visibility: true,
                                plainText: 'Input search...',
                                showLabel: false,
                                Id: header.Id,
                                searchFieldName: header.searchFieldName
                            });
                            txtSearch.searchIcon = "fal fa-search";
                            txtSearch.searchMethod = searchMethodEnum.contain;
                            txtSearch.orderMethod = "asc";
                            txtSearch.isOrderBy = false;
                            txtSearch.entity = this.listViewSearch.entityVM;
                            this.searchSection.addChild(txtSearch);
                            txtSearch.element.addEventListener("keydown", (e) => {
                                let code = e.keyCodeEnum();
                                if (code == keyCodeEnum.enter) {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    this.applyFilter();
                                }
                            });
                            Html.end.div.className("btn-group").button.tabIndex(-1).event("click", (e) => {
                                this.searchTypeMenu(e, header, txtSearch);
                            }).span.className(txtSearch.searchIcon);
                            txtSearch.searchIconElement = Html.context;
                            break;
                        case "Number":
                            var txtSearch = new Textbox({
                                fieldName: header.fieldName,
                                Visibility: true,
                                plainText: 'Input search...',
                                showLabel: false,
                                Id: header.Id,
                                searchFieldName: header.searchFieldName
                            });
                            txtSearch.searchIcon = "fal fa-search";
                            txtSearch.searchMethod = searchMethodEnum.equal;
                            txtSearch.orderMethod = "asc";
                            txtSearch.isOrderBy = false;
                            txtSearch.entity = this.listViewSearch.entityVM;
                            this.searchSection.addChild(txtSearch);
                            txtSearch.element.addEventListener("keydown", (e) => {
                                let code = e.keyCodeEnum();
                                if (code == keyCodeEnum.enter) {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    this.applyFilter();
                                }
                            });
                            Html.end.div.className("btn-group").button.tabIndex(-1).event("click", (e) => {
                                this.searchTypeMenu(e, header, txtSearch);
                            }).span.className(txtSearch.searchIcon);
                            txtSearch.searchIconElement = Html.context;
                            break;
                        case "Datepicker":
                            var txtSearch = new Datepicker({
                                fieldName: header.fieldName,
                                Visibility: true,
                                showLabel: false,
                                Precision: 2,
                                showHotKey: true,
                                Id: header.Id,
                                searchFieldName: header.searchFieldName
                            });
                            txtSearch.searchMethod = searchMethodEnum.range;
                            txtSearch.orderMethod = "asc";
                            txtSearch.searchIcon = "fal fa-arrows-alt-h";
                            this.searchSection.addChild(txtSearch);
                            txtSearch.element.addEventListener("keydown", (e) => {
                                let code = e.keyCodeEnum();
                                if (code == keyCodeEnum.enter) {
                                    e.preventDefault();
                                    this.actionFilter();
                                }
                            });
                            Html.end.div.className("btn-group").button.tabIndex(-1).event("click", (e) => {
                                this.searchTypeMenu(e, header, txtSearch);
                            }).span.className(txtSearch.searchIcon);
                            txtSearch.searchIconElement = Html.context;
                            break;
                        case "Checkbox":
                            var txtSearch = new Select({
                                fieldName: header.fieldName,
                                Visibility: true,
                                showLabel: false,
                                Id: header.Id,
                                searchFieldName: header.searchFieldName,
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
                            this.searchSection.addChild(txtSearch);
                            txtSearch.searchMethod = searchMethodEnum.contain;
                            txtSearch.element.addEventListener("keydown", (e) => {
                                let code = e.keyCodeEnum();
                                if (code == keyCodeEnum.enter) {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    this.applyFilter();
                                }
                            });
                            txtSearch.searchIconElement = Html.context;
                            break;
                        default:
                            var sec = new Section(null, Html.context);
                            sec.meta = header;
                            this.searchSection.addChild(sec);
                            break;
                    }
                }
                Html.endOf(ElementType.td);
            }).endOf(ElementType.tr).render();
        }
    }

    searchTypeMenu(e, header, txtSearch) {
        const ele = e.target;
        var buttonRect = ele.getBoundingClientRect();
        var ctxMenu = ContextMenu.Instance;
        ctxMenu.Top = buttonRect.bottom;
        ctxMenu.Left = buttonRect.left;
        ctxMenu.menuItems = [];
        if (header.componentType == "Input" || header.componentType == "Dropdown") {
            var className = txtSearch.orderMethod == "asc" ? "fas fa-sort-amount-up" : "fas fa-sort-amount-down";
            ctxMenu.menuItems.push({
                Icon: className,
                Text: "Order By",
                Click: () => this.actionSearch(header, "OrderBy", txtSearch, className)
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-list",
                Text: "Multiple data",
                Click: () => this.actionSearch(header, searchMethodEnum.equal, txtSearch, "fal fa-list", true)
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-search",
                Text: "Contains",
                Click: () => this.actionSearch(header, searchMethodEnum.contain, txtSearch, "fal fa-search")
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-search-minus",
                Text: "No Contains",
                Click: () => this.actionSearch(this.meta, searchMethodEnum.notContain, txtSearch, "fal fa-search-minus")
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-arrow-right",
                Text: "Starts With",
                Click: () => this.actionSearch(header, searchMethodEnum.startWith, txtSearch, "fal fa-arrow-right")
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-arrow-left",
                Text: "Ends With",
                Click: () => this.actionSearch(header, searchMethodEnum.endWith, "fal fa-arrow-left")
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-minus-circle",
                Text: "Empty",
                Click: () => this.actionSearch(header, searchMethodEnum.empty, txtSearch, "fal fa-minus-circle")
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-check-circle",
                Text: "Not Empty",
                Click: () => this.actionSearch(header, searchMethodEnum.filled, txtSearch, "fal fa-check-circle")
            });
        }
        else if (header.componentType == "Datepicker") {
            var className = txtSearch.orderMethod == "asc" ? "fas fa-sort-amount-up" : "fas fa-sort-amount-down";
            ctxMenu.menuItems.push({
                Icon: className,
                Text: "Order By",
                Click: () => this.actionSearch(header, "OrderBy", txtSearch, className)
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-arrows-alt-h",
                Text: "Between",
                Click: () => this.actionSearch(header, searchMethodEnum.range, txtSearch, "fal fa-arrows-alt-h")
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-minus-circle",
                Text: "Empty",
                Click: () => this.actionSearch(header, searchMethodEnum.empty, txtSearch, "fal fa-minus-circle")
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-check-circle",
                Text: "Not Empty",
                Click: () => this.actionSearch(header, searchMethodEnum.filled, txtSearch, "fal fa-check-circle")
            });
        }
        else {
            var className = txtSearch.orderMethod == "asc" ? "fas fa-sort-amount-up" : "fas fa-sort-amount-down";
            ctxMenu.menuItems.push({
                Icon: className,
                Text: "Order By",
                Click: () => this.actionSearch(header, "OrderBy", txtSearch, className)
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-minus-circle",
                Text: "Empty",
                Click: () => this.actionSearch(header, searchMethodEnum.empty, txtSearch, "fal fa-minus-circle")
            });
            ctxMenu.menuItems.push({
                Icon: "fal fa-check-circle",
                Text: "Not Empty",
                Click: () => this.actionSearch(header, searchMethodEnum.filled, txtSearch, "fal fa-check-circle")
            });
        }
        ctxMenu.editForm = this.editForm;
        ctxMenu.render();
    }

    actionSearch(header, type, txtSearch, className, multiple) {
        txtSearch.searchMethod = type;
        if (type == "OrderBy") {
            this.searchSection.Children.forEach(x => x.isOrderBy = false);
            txtSearch.orderMethod = txtSearch.orderMethod == "asc" ? "desc" : "asc";
            className = txtSearch.orderMethod == "asc" ? "fas fa-sort-amount-up" : "fas fa-sort-amount-down";
            txtSearch.isOrderBy = true;
        }
        txtSearch.searchIconElement.className = className;
        if (multiple) {
            var com = JSON.parse(JSON.stringify(header));
            com.componentType = "Textarea";
            com.fieldName = com.fieldName + "Search";
            com.formatData = null;
            com.Row = 7;
            var name = com.entityName || "Entity";
            this.editForm.openConfig("Search multiple data", async () => {
                txtSearch.multipleData = this.editForm[name][com.fieldName];
                this.applyFilter();
            }, () => { }, true, [com], null, null, null, true);
        }
        else {
            txtSearch.multipleData = null;
            this.applyFilter();
        }
    }
    /**
     * @param {HTMLElement} col
     */
    createResizableTable(col) {
        var resizer = document.createElement("div");
        resizer.classList.add("resizer");
        col.appendChild(resizer);
        this.createResizableColumn(col, resizer);
    }
    /**
     * @param {HTMLElement} col
     * @param {HTMLElement} resizer
     */
    createResizableColumn(col, resizer) {
        this.x = 0;
        this.w = 0;
        resizer.addEventListener("mousedown", (e) => this.mouseDownHandler(e, col, resizer));
    }
    /** @type {mouseEvent} */
    mouseMoveHandler;
    /** @type {mouseEvent} */
    mouseUpHandler;
    /** @type {Number} */
    x = 0;
    /** @type {Number} */
    w = 0;
    /**
     * @param {mouseEvent} mouse
     * @param {HTMLElement} col
     * @param {HTMLElement} resizer
     */
    mouseDownHandler(mouse, col, resizer) {
        mouse.preventDefault();
        this.x = mouse.clientX;
        var styles = window.getComputedStyle(col);
        this.w = parseFloat((styles.width.replace("px", "") == "") ? "0" : styles.width.replace("px", ""));
        this.mouseMoveHandler = (a) => this.mouseMoveHandler(a, col, resizer);
        this.mouseUpHandler = (a) => this.mouseUpHandler(a, col, resizer);
        document.addEventListener("mousemove", this.mouseMoveHandler);
        document.addEventListener("mouseup", this.mouseUpHandler);
        resizer.classList.add("resizing");
    }

    /**
     * @param {mouseEvent} mouse
     * @param {HTMLElement} col
     * @param {HTMLElement} resizer
     */
    mouseMoveHandler(mouse, col, resizer) {
        mouse.preventDefault();
        var dx = mouse.clientX - this.x;
        col.style.width = `${this.w + dx}px`;
        col.style.minWidth = `${this.w + dx}px`;
        col.style.maxWidth = `${this.w + dx}px`;
        this.updateStickyColumns();
    }
    /**
     * @param {mouseEvent} mouse
     * @param {HTMLElement} col
     * @param {HTMLElement} resizer
     */
    mouseUpHandler(mouse, col, resizer) {
        mouse.preventDefault();
        this.updateHeaders();
        resizer.classList.remove("resizing");
        document.removeEventListener("mousemove", this.mouseMoveHandler);
        document.removeEventListener("mouseup", this.mouseUpHandler);
    }
    _imeout = 0;
    updateHeaders(sticky) {
        window.clearTimeout(this._imeout);
        this._imeout = window.setTimeout(() => {
            const headerElements = this.headerSection.Children.filter(x => x.Meta && x.Meta.Id);
            let index = 0;
            let anyGroup = this.Header.some(x => x.groupName && !Utils.isNullOrWhiteSpace(x.groupName));
            if (!anyGroup) {
                headerElements.forEach(header => {
                    header.Order = index;
                    header.Meta.Order = index;
                    index++;
                });
            }
            if (Client.systemRole) {
                const columns = headerElements.map(header => {
                    const match = header.element;
                    if (match && !header.Meta.statusBar && Utils.isNullOrWhiteSpace(match.style.display)) {
                        const width = `${match.offsetWidth}px`;
                        const dirtyPatch = [
                            { Field: "Id", Value: header.Meta.Id },
                            { Field: "featureId", Value: header.Meta.featureId },
                            { Field: "Frozen", Value: header.Meta.Frozen },
                            { Field: "frozenRight", Value: header.Meta.frozenRight },
                            Utils.isNullOrWhiteSpace(header.groupName) ? { Field: "Width", Value: width } : { Field: "Width", Value: header.Meta.Width },
                            Utils.isNullOrWhiteSpace(header.groupName) ? { Field: "maxWidth", Value: width } : { Field: "maxWidth", Value: header.Meta.maxWidth },
                            Utils.isNullOrWhiteSpace(header.groupName) ? { Field: "minWidth", Value: width } : { Field: "minWidth", Value: header.Meta.minWidth },
                        ];
                        if (!anyGroup) {
                            dirtyPatch.push({ Field: "Order", Value: header.Order })
                        }
                        return {
                            Changes: dirtyPatch,
                            notMessage: true,
                            Table: "Component",
                        };
                    }
                    return null;
                }).filter(x => x != null);
                Client.instance.patchAsync2(columns).then();
            }
            else {
                const columns = headerElements.map(header => {
                    const match = header.element;
                    if (match && !header.Meta.statusBar && !Utils.isNullOrWhiteSpace(header.Meta.fieldName) && Utils.isNullOrWhiteSpace(match.style.display)) {
                        const width = `${match.offsetWidth}px`;
                        return {
                            Id: header.Meta.Id,
                            fieldName: header.Meta.fieldName,
                            Frozen: header.Meta.Frozen,
                            frozenRight: header.Meta.frozenRight,
                            Order: header.Order,
                            Width: width,
                        };
                    }
                    return null;
                }).filter(x => x != null);
                var userSetting = new UserSetting();
                userSetting.featureId = this.editForm.meta.Id;
                userSetting.componentId = this.meta.Id;
                userSetting.Active = true;
                userSetting.Value = JSON.stringify(columns);
                Client.instance.postAsync(userSetting, "/api/UserSetting").then();
            }
            if (sticky) {
                this.updateStickyColumns();
            }
        }, 500);
    }

    changeHeader(e, header) {
        clearTimeout(this._imeout);
        this._imeout = setTimeout(() => {
            let html = e.target;
            let patchVM = {
                Table: "Component",
                Changes: [
                    { Field: "Component.Id", Value: header.Id, oldVal: header.Id },
                    { Field: "Component.Label", Value: html.textContent.trim(), oldVal: header.Label }
                ]
            };
            // @ts-ignore
            Client.instance.patchAsync(patchVM);
        }, 1000);
    }

    updatePagination(total, currentPageCount) {
        if (!this.paginator) {
            return;
        }
        var options = this.paginator.Options;
        options.Total = total;
        options.currentPageCount = currentPageCount;
        options.pageNumber = (options.pageIndex || 0) + 1;
        options.startIndex = (options.pageIndex || 0) * options.pageSize + 1;
        options.endIndex = options.startIndex + options.currentPageCount - 1;
        this.paginator.updateView();
        if (total <= this.meta.row) {
            this.paginator.Show = false;
        }
        else {
            this.paginator.Show = true;
        }
    }

    toggleAll() {
        const anySelected = this.allListViewItem.some(x => x.Selected);
        if (anySelected) {
            this.clearSelected();
            this.dispatchEvent(this.meta.events, EventType.Click, this, this.entity).then();
            return;
        }
        this.allListViewItem.forEach(x => {
            x.Selected = true;
        });
        this.dispatchEvent(this.meta.events, EventType.Click, this, this.entity).then();
    }

    headerContextMenu(e, header) {
        e.preventDefault();
        e.stopPropagation();
        var menu = ContextMenu.Instance;
        menu.Top = e.clientY;
        menu.Left = e.clientX;
        menu.menuItems = [];
        if (Client.systemRole) {
            menu.menuItems.push({ Icon: "fal fa-wrench", Text: "Column Properties", Click: () => this.editForm.componentProperties(header) });
            menu.menuItems.push({ Icon: "fal fa-table", Text: "Table Properties", Click: () => this.editForm.componentProperties(this.meta) });
        }
        if (!header.statusBar && header.fieldName && !header.groupName) {
            if (Client.systemRole && ["Input", "Dropdown", "Select", "Checkbox", "Textarea"].some(x => x == header.componentType)) {
                menu.menuItems.push({ Icon: "fal fa-copy", Text: "Set Default Value", Click: this.setDefaultValue.bind(this), Parameter: header });
            }
            menu.menuItems.push({
                Icon: "fas fa-thumbtack", Text: "Pin Column", menuItems: [
                    {
                        Icon: header.Frozen ? "fas fa-check" : "fas fa-ellipsis-h",
                        Text: "Pin Left",
                        Click: () => this.updateFrozen(header, 1)
                    },
                    {
                        Icon: header.frozenRight ? "fas fa-check" : "fas fa-ellipsis-h",
                        Text: "Pin Right",
                        Click: () => this.updateFrozen(header, 2)
                    },
                    {
                        Icon: !header.Frozen && !header.frozenRight ? "fas fa-check" : "fas fa-ellipsis-h",
                        Text: "No Pin",
                        Click: () => this.updateFrozen(header, 3)
                    }
                ]
            });
        }

        menu.editForm = this.editForm;
        menu.render();
    }

    setDefaultValue(component) {
        if (component.componentType == "GridView") {
            return;
        }
        var com = JSON.parse(JSON.stringify(component));
        com.fieldName = 'defaultValue' + com.fieldName;
        var name = com.entityName || "Entity";
        this[name][com.fieldName] = com.defaultVal;
        this.editForm.openConfig("Set default value", async () => {
            let dirtyPatchDetail = [
                {
                    Label: "Id",
                    Field: "Id",
                    oldVal: null,
                    Value: com.Id,
                },
                {
                    Label: "featureId",
                    Field: "featureId",
                    oldVal: null,
                    Value: com.featureId,
                },
                {
                    Label: "defaultVal",
                    Field: "defaultVal",
                    oldVal: null,
                    Value: this[name][com.fieldName],
                }
            ]
            let patchModelDetail = {
                Changes: dirtyPatchDetail,
                Table: "Component",
                notMessage: true
            };
            component.defaultVal = this[name][com.fieldName];
            await Client.instance.patchAsync(patchModelDetail);
            this.Dirty = false;
        }, () => { }, true, [com], null, null, null, true);
    }

    updateFrozen(header, frozenType) {
        if (frozenType === 1) {
            header.Frozen = true;
        }
        else if (frozenType === 2) {
            header.frozenRight = true;
        }
        else {
            header.Frozen = false;
            header.frozenRight = false;
        }
        this.updateHeaders(true);
    }

    frozenColumn(arg) {
        const entity = arg.header;
        const header = this.Header.find(x => x.Id === entity.Id);
        if (header) {
            header.Frozen = !header.Frozen;
        }
        this.updateHeaders();
    }

    removeRowById(id) {
        super.removeRowById(id);
        this.renderIndex();
    }

    removeRow(row) {
        super.removeRow(row);
        this.renderIndex();
    }

    hardDeleteConfirmed(deleted, newId) {
        return new Promise((resolve, reject) => {
            super.hardDeleteConfirmed(deleted, newId).then(async res => {
                this.renderIndex();
                if (this.meta.isSumary) {
                    this.addSummaries();
                    var parent = this.editForm.tabGroup.flatMap(x => x.Children);
                    if (parent.length > 0) {
                        for (const element of parent) {
                            await element.countBadge();
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
    prepareUpdateView(force, dirty) {
        super.prepareUpdateView(force, dirty);
        if (this.entity.Id && this.entity.Id.startsWith("-") && this.meta.editable && this.meta.canAdd) {
            this.toggleAddRow(true);
        }
        else {
            if (this.meta && this.meta.addRowExp) {
                this.toggleAddRow(this.meta.addRowExp);
            }
        }
    }
    /**
     * 
     * @param {Boolean | String | Function} disabled 
     */
    toggleAddRow(add) {
        if (typeof add === "boolean") {
            if (add) {
                this.addNewEmptyRow();
            }
            else {
                if (this.gridViewItemEmpty) {
                    this.gridViewItemEmpty.Dispose();
                    this.gridViewItemEmpty = null;
                }
            }
            return;
        }
        var addFn = Utils.isFunction(add, false, this);
        if (addFn) {
            this.addNewEmptyRow();
        }
        else {
            if (this.gridViewItemEmpty) {
                this.gridViewItemEmpty.Dispose();
                this.gridViewItemEmpty = null;
            }
        }
    }
    updateView(force = false, dirty = null, componentNames = []) {
        if (!this.Editable && !this.meta.canCache) {
            this.actionFilter();
        } else {
            this.rowAction(row => !row.emptyRow, row => row.updateView(force, dirty, componentNames));
        }
    }

    async rowChangeHandlerGrid(rowData, rowSection, observableArgs, component = null) {
        await new Promise(resolve => setTimeout(resolve, this.cellCountNoSticky));
        if (rowSection.emptyRow && observableArgs.evType === EventType.Change) {
            await this.dispatchCustomEvent(this.meta.events, customEventType.beforeCreated, rowSection, rowData);
            rowSection.emptyRow = false;
            this.moveEmptyRow(rowSection);
            const headers = this.Header.filter(y => y.Editable);
            const currentComponent = headers.find(y => y.fieldName === component.fieldName);
            const index = headers.indexOf(currentComponent);
            if (headers.length > index + 1) {
                const nextGrid = headers[index + 1];
                const nextComponent = rowSection.Children.find(y => y.fieldName === nextGrid.fieldName);
                if (nextComponent) {
                    nextComponent.Focus();
                }
            }
            this.emptySection.Children = [];
            this.addNewEmptyRow();
            await this.dispatchCustomEvent(this.meta.events, customEventType.afterCreated, rowSection, rowData);
        }
        this.addSummaries();
        await this.dispatchEvent(this.meta.events, EventType.Change, rowSection, rowData);
    }

    getViewPortItem() {
        if (!this.element || !this.element.classList.contains('sticky')) {
            return this.rowData.Data.length;
        }
        let mainSectionHeight = this.element.clientHeight
            - (this.headerSection.element ? this.headerSection.element.clientHeight : 0)
            - this.paginator.element.clientHeight
            - this._theadTable;

        this.Header = this.Header.filter(x => x != null);

        if (this.Header.some(x => x.Summary && x.Summary.trim() !== "")) {
            mainSectionHeight -= this._tfooterTable;
        }
        if (this.meta.canAdd) {
            mainSectionHeight -= this._rowHeight;
        }
        return this.getRowCountByHeight(mainSectionHeight);
    }
}
