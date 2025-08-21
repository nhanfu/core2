import { Component, ElementType } from "./models/";
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
    RenderRowData(headers, row, index = null, emptyRow = false) {
        if (index !== null) {
            if (index >= this.Element.parentElement.children.length || index < 0) {
                index = 0;
            }
            this.Element.parentElement.insertBefore(this.Element, this.Element.parentElement.children[index]);
        }
        for (let index = 0; index < headers.length; index++) {
            const header = headers[index];
            this.RenderTableCell(row, header, null, index, index);
        }
    }

    /**
     * @param {any} rowData
     * @param {Component} header
     */
    RenderTableCell(rowData, header, cellWrapper = null, rowIndex = null, cellIndex = null) {
        if (header && header.ComponentType == "Number") {
            header.TextAlign = "right";
        }
        Html.Take(this.Element).TData.Attr("component", header.ComponentType || "Number").TabIndex(-1).DataAttr("field", header.FieldName || "NonField").TextAlign(header.TextAlign || 'left').Event("focusin", (e) => this.FocusCell(e, header));;
        if (header.StatusBar && this.Meta.ShowHotKey) {
            Html.Instance.ClassName("status-bar");
        }
        var td = Html.Instance.GetContext();
        Html.Instance.Event("keydown", (e) => this.ListViewItemTab(e, td, header));
        if (header.FieldName && header.ComponentType != 'Button' && !header.StatusBar) {
            Html.Instance.Event("mousedown", (e) => {
                this.ListView.ClearSelection();
                this.ListView.IsMouseDown = true;
                this.ListView.StartCell = e.target.closest("td");
            });
            Html.Instance.Event("mouseover",/**@param {Event} e */(e) => {
                if (this.ListView.IsMouseDown && this.ListView.StartCell) {
                    const startRow = parseInt(this.ListView.StartCell.dataset.row);
                    const startCol = parseInt(this.ListView.StartCell.dataset.col);
                    const endRow = parseInt(td.dataset.row);
                    const endCol = parseInt(td.dataset.col);

                    const minRow = Math.min(startRow, endRow);
                    const maxRow = Math.max(startRow, endRow);
                    const minCol = Math.min(startCol, endCol);
                    const maxCol = Math.max(startCol, endCol);
                    for (let i = 0; i < this.ListView.Matrix.length; i++) {
                        for (let j = 0; j < this.ListView.Matrix[i].length; j++) {
                            this.ListView.Matrix[i][j].classList.remove('cell-matrix');
                        }
                    }
                    for (let i = minRow; i <= maxRow; i++) {
                        for (let j = minCol; j <= maxCol; j++) {
                            this.ListView.Matrix[i][j].classList.add('cell-matrix');
                        }
                    }
                }
            });
            Html.Instance.Event("mouseup", (e) => {
                this.ListView.IsMouseDown = false;
                this.ListView.StartCell = null;
            });
        }
        Html.Instance.Div.ClassName("wrapper-cell").Render();
        if (header.ComponentType == "Checkbox") {
            Html.Instance.Style("justify-content: center;");
        }
        header.FocusSearch = !header.IsMultiple;
        super.RenderTableCell(rowData, header, cellWrapper ?? Html.Context);
        Html.Instance.EndOf(ElementType.td);
    }

    /**
     * @param {Event} e
     * @param {Component} header
     */
    FocusCell(e, header) {
        if (this.ListView == null) {
            return;
        }
        if (this.ListView.LastElementFocus) {
            this.ListView.LastElementFocus?.closest("td").classList.remove("cell-selected");
        }

        /** @type {HTMLTableCellElement} */
        // @ts-ignore
        let td = e.target;
        td.closest("td").classList.add("cell-selected");
        this.ListView.LastElementFocus = td;
        this.ListView.LastComponentFocus = header;
        this.ListView.EntityFocusId = this.EntityId;
    }
}