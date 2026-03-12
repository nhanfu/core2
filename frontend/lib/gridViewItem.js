import { Component, ElementType, EventType } from "./models/";
import { ListViewItem } from "./listViewItem.js";
import { Html } from "./utils/html.js";

export class GridViewItem extends ListViewItem {
    constructor(tr) {
        super(tr);
    }

    /**
     * 
     * @param {Component[]} headers 
     * @param {any} row 
     * @param {number} index 
     * @param {*} emptyRow 
     */
    renderRowData(headers, row, index = null, emptyRow = false) {
        if (index !== null) {
            if (index >= this.element.parentElement.children.length || index < 0) {
                index = 0;
            }
            this.element.parentElement.insertBefore(this.element, this.element.parentElement.children[index]);
        }
        for (let index = 0; index < headers.length; index++) {
            const header = headers[index];
            this.renderTableCell(row, header, null, index, index);
        }
    }

    /**
     * @param {any} rowData
     * @param {Component} header
     */
    renderTableCell(rowData, header, cellWrapper = null, rowIndex = null, cellIndex = null) {
        if (header && header.componentType == "Number") {
            header.textAlign = "right";
        }
        Html.take(this.element).tData.attr("component", header.componentType || "Number").tabIndex(-1).dataAttr("field", header.fieldName || "nonField").textAlign(header.textAlign || 'left').event("focusin", (e) => this.focusCell(e, header));;
        if (header.statusBar && this.meta.showHotKey) {
            Html.instance.className("status-bar");
        }
        var td = Html.instance.getContext();
        Html.instance.event("keydown", (e) => this.listViewItemTab(e, td, header));
        if (header.fieldName && header.componentType != 'Button' && !header.statusBar) {
            Html.instance.event("mousedown", (e) => {
                this.listView.clearSelection();
                this.listView.isMouseDown = true;
                this.listView.startCell = e.target.closest("td");
            });
            Html.instance.event("mouseover",/**@param {Event} e */(e) => {
                if (this.listView.isMouseDown && this.listView.startCell) {
                    window.getSelection().removeAllRanges();
                    const startRow = parseInt(this.listView.startCell.dataset.row);
                    const startCol = parseInt(this.listView.startCell.dataset.col);
                    const endRow = parseInt(td.dataset.row);
                    const endCol = parseInt(td.dataset.col);

                    const minRow = Math.min(startRow, endRow);
                    const maxRow = Math.max(startRow, endRow);
                    const minCol = Math.min(startCol, endCol);
                    const maxCol = Math.max(startCol, endCol);
                    for (let i = 0; i < this.listView.Matrix.length; i++) {
                        for (let j = 0; j < this.listView.Matrix[i].length; j++) {
                            this.listView.Matrix[i][j].classList.remove('cell-matrix');
                        }
                    }
                    for (let i = minRow; i <= maxRow; i++) {
                        for (let j = minCol; j <= maxCol; j++) {
                            this.listView.Matrix[i][j].classList.add('cell-matrix');
                        }
                    }
                }
            });
            Html.instance.event("mouseup", (e) => {
                this.listView.isMouseDown = false;
                this.listView.startCell = null;
            });
        }
        Html.instance.div.className("wrapper-cell").render();
        if (header.componentType == "Checkbox") {
            Html.instance.style("justify-content: center;");
        }
        header.focusSearch = !header.isMultiple;
        super.renderTableCell(rowData, header, cellWrapper ?? Html.context);
        Html.instance.endOf(ElementType.td);
    }

    /**
     * @param {Event} e
     * @param {Component} header
     */
    focusCell(e, header) {
        if (this.listView == null) {
            return;
        }
        if (this.listView.lastElementFocus) {
            this.listView.lastElementFocus?.closest("td").classList.remove("cell-copy");
            this.listView.lastElementFocus?.closest("td").classList.remove("cell-selected");
        }
        let td = e.target;
        /**
         * @type {hTMLTableCellElement}
         */
        var tdElement = td.closest("td");
        tdElement.classList.add("cell-selected");
        this.listView.lastElementFocus = td;
        this.listView.lastComponentFocus = header;
        this.listView.entityFocusId = this.entityId;
        if (!tdElement._hasCopyListener) {
            tdElement._hasCopyListener = true;
            tdElement.addEventListener(EventType.keyDown, (ev) => {
                if (ev.ctrlKey && ev.key.toLowerCase() === "c") {
                    const selectedText = window.getSelection()?.toString() || "";
                    if (selectedText.length > 0) return;
                    td.classList.add("cell-copy");
                    const text = td.innerText == "" ? td.value.trim() : td.innerText.trim();
                    navigator.clipboard.writeText(text).catch(() => { });
                }
            });
        }
    }
}