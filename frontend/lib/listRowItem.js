import { Component, ElementType, EventType } from "./models/index.js";
import { ListViewItem } from "./listViewItem.js";
import { Html } from "./utils/html.js";

export class ListRowItem extends ListViewItem {
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
        var newHeaders = headers.filter(x => x.virtualScroll).orderBy(x => x.Order);
        if (index !== null) {
            if (index >= this.element.parentElement.children.length || index < 0) {
                index = 0;
            }
            this.element.parentElement.insertBefore(this.element, this.element.parentElement.children[index]);
        }
        for (let index = 0; index < newHeaders.length; index++) {
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
        Html.instance.div.className("wrapper-cell-mobile").event("focusin", (e) => {
            this.listView.lastComponentFocus = header;
        }).span.className("cell-label").iText(header.componentType == "Button" ? "View" : header.label).end.div.className("cell-value").render();
        super.renderTableCell(rowData, header, cellWrapper ?? Html.context);
        Html.instance.endOf(".wrapper-cell-mobile");
    }
}