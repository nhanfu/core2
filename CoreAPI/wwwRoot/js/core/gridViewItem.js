import { ListViewItem } from "listViewItem";
import { ElementType } from "models/elementType";
import EventType from "models/eventType";
import { Html } from "utils/html";

export class GridViewItem extends ListViewItem {
    constructor(tr) {
        super(tr);
    }

    renderRowData(headers, row, index = null, emptyRow = false) {
        if (index !== null) {
            if (index >= this.Element.parentElement.children.length || index < 0) {
                index = 0;
            }

            this.Element.parentElement.insertBefore(this.Element, this.Element.parentElement.children[index]);
        }

        headers.filter(x => !x.hidden).forEach(header => {
            this.RenderTableCell(row, header, null);
        });
        this.BindingEvents();
    }

    RenderTableCell(rowData, header, cellWrapper = null) {
        Html.Take(this.Element).TData.TabIndex(-1)
        .Event(EventType.FocusIn, (e) => this.FocusCell(e, header))
        .DataAttr("field", header.FieldName).Render();
    
        if (header.statusBar) {
            Html.Instance.Icon('fa fa-pencil').End.Render();
        }
    
        if (!header.fieldName) {
            return;
        }
    }
    
    FocusCell(e, header) {
        if (this.ListViewSection.ListView.LastElementFocus) {
            this.ListViewSection.ListView.LastElementFocus?.Closest(ElementType.td.toString()).RemoveClass("cell-selected");
        }
    
        let td = e.target;
        td.closest(ElementType.td.toString()).classList.add("cell-selected");
        this.ListViewSection.ListView.LastElementFocus = td;
        this.ListViewSection.ListView.LastComponentFocus = header;
        this.ListViewSection.ListView.EntityFocusId = this.Entity[this.IdField]?.toString();
    }
    
   
}