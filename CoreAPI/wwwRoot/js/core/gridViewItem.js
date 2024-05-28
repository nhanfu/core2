import { Component } from "./models/component.js";
import { ListViewItem } from "./listViewItem.js";
import { ElementType } from "./models/elementType.js";
import EventType from "./models/eventType.js";
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

        headers.filter(x => !x.Hidden).forEach(header => {
            this.RenderTableCell(row, header, null);
        });
        this.BindingEvents();
    }

    /**
     * @param {any} rowData
     * @param {Component} header
     */
    RenderTableCell(rowData, header, cellWrapper = null) {
        Html.Take(this.Element).TData.TabIndex(-1)
        .Event(EventType.FocusIn, (e) => this.FocusCell(e, header))
        .DataAttr("field", header.FieldName).Render();
    
        if (header.StatusBar) {
            Html.Instance.Icon('fa fa-pencil').End.Render();
        }
    
        if (!header.FieldName) {
            return;
        }
    }
    
    /**
     * @param {Event} e
     * @param {Component} header
     */
    FocusCell(e, header) {
        if (this.ListViewSection == null) return;
        if (this.ListViewSection.ListView.LastElementFocus) {
            this.ListViewSection.ListView.LastElementFocus?.Closest(ElementType.td.toString()).RemoveClass("cell-selected");
        }
    
        /** @type {HTMLTableCellElement} */
        // @ts-ignore
        let td = e.target;
        td.closest(ElementType.td).classList.add("cell-selected");
        this.ListViewSection.ListView.LastElementFocus = td;
        this.ListViewSection.ListView.LastComponentFocus = header;
        this.ListViewSection.ListView.EntityFocusId = this.EntityId;
    }
}