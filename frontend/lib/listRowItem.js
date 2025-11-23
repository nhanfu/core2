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
    RenderRowData(headers, row, index = null, emptyRow = false) {
        var newHeaders = headers.filter(x => x.VirtualScroll).OrderBy(x => x.Order);
        if (index !== null) {
            if (index >= this.Element.parentElement.children.length || index < 0) {
                index = 0;
            }
            this.Element.parentElement.insertBefore(this.Element, this.Element.parentElement.children[index]);
        }
        for (let index = 0; index < newHeaders.length; index++) {
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
        Html.Instance.Div.ClassName("wrapper-cell-mobile").Event("focusin", (e) => {
            this.ListView.LastComponentFocus = header;
        }).Span.ClassName("cell-label").IText(header.ComponentType == "Button" ? "View" : header.Label).End.Div.ClassName("cell-value").Render();
        super.RenderTableCell(rowData, header, cellWrapper ?? Html.Context);
        Html.Instance.EndOf(".wrapper-cell-mobile");
    }
}