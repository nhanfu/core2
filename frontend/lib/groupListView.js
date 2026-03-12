import { GroupRowData } from "./groupGridView.js";
import { GroupViewItem } from "./groupViewItem.js";
import { ListView } from "./listView.js";
import { ListViewItem } from "./listViewItem.js";
import { ElementType } from "./models/elementType.js";
import EventType from "./models/eventType.js";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";

export class GroupListView extends ListView {
    static _groupKey = "__groupkey__";
    static groupRowClass = "group-row";

    constructor(ui) {
        super(ui);
    }

    Render() {
        super.render();
        Html.Take(this.element).className("group-listview").End.render();
    }

    addRow(item, fromIndex, singleAdd = true) {
        return new Promise((resolve) => {
            fromIndex = 0;
            this.disposeNoRecord();
            const keys = this.meta.groupBy.split(",");
            item[GroupListView._groupKey] = keys.map(key => item[key]?.toString()).join(" "); 
            const groupKey = item[GroupListView._groupKey]; 
            const existGroup = this.allListViewItem.find(group => group.groupRow && group.Entity.As('GroupRowData').Key === groupKey); 
    
            if (existGroup === null) {
                const groupData = new GroupRowData();
                groupData.Key = groupKey;
                groupData.Children.push(item); 
                this.formattedRowData.push(groupData);
                const rowSection = this.renderRowData(this.Header, groupData, this.mainSection, this.mainSection.Children.length);
                if (singleAdd) {
                    this.addNewEmptyRow(); 
                }
                this.Dirty = true; 
                resolve(rowSection);
            } else {
                existGroup.Entity.As('GroupRowData').Children.push(item);
                const index = this.mainSection.Children.indexOf(existGroup);
                const rowSection = this.renderRowData(this.Header, item, this.mainSection, index + existGroup.Children.length); // Thực hiện render dữ liệu cho item mới trong nhóm
                if (singleAdd) {
                    this.addNewEmptyRow(); 
                }
                this.Dirty = true; 
                resolve(rowSection);
            }
        });
    }

    addRows(rowsData, index = 0) {
        let listItem = [];
        rowsData.forEach(async x => {
            listItem.push(await this.addRow(x, 0, false));
        });
        return Promise.all(listItem);
    }

    renderRowData(headers, row, listViewSection, index, emptyRow = false) {
        if (!(row instanceof GroupRowData)) {
            return super.renderRowData(headers, row, listViewSection, index, emptyRow);
        }
        let wrapper = listViewSection.element;
        if (!row.Key || row.Key.toString().isNullOrWhiteSpace()) {
            let rowResult = null;
            row.Children.forEach(child => {
                Html.Take(wrapper);
                rowResult = this.renderRowData(headers, child, listViewSection, null);
            });
            return rowResult;
        }
        let groupSection = new GroupViewItem(ElementType.div);
        groupSection.Entity = row;
        groupSection.parentElement = wrapper;
        groupSection.groupRow = true;
        groupSection.preQueryFn = this._preQueryFn;
        groupSection.listView = this;
        groupSection.Meta = this.meta;
        listViewSection.addChild(groupSection);
        let first = row.Children[0];
        let groupText = Utils.formatEntity2(this.meta.groupFormat, null, first, x => "N/A", x => "N/A");
        Html.Take(groupSection.element).Event(EventType.Click, this.dispatchClick.bind(this), first)
            .Event(EventType.dblClick, this.dispatchDblClick.bind(this), first)
            // @ts-ignore
            .Icon("fa fa-chevron-right").Event(EventType.Click, this.toggleGroupRow.bind(this), groupSection).End
            .Span.innerHTML(groupText);
        groupSection.groupText = Html.Context;
        row.Children.forEach(child => {
            Html.Take(groupSection.element);
            let childRow = this.renderRowData(headers, child, groupSection, null);
            childRow.groupSection = groupSection;
            Html.Take(childRow.element).smallCheckbox().render();
            let chk = Html.Context.previousElementSibling;
            if(chk instanceof hTMLInputElement) {
                Html.Instance.End.End.Event(EventType.Click, (e) => {
                    e.preventDefault();
                    childRow.Selected = !childRow.Selected;
                    chk.checked = childRow.Selected;
                });
            }
        });
        return groupSection;
    }

    dispatchClick(row) {
        this.dispatchEvent(this.meta.groupEvent, EventType.Click, row).Done();
    }

    dispatchDblClick(row) {
        this.dispatchEvent(this.meta.groupEvent, EventType.dblClick, row).Done();
    }

    toggleGroupRow(groupSection, e) {
        const target = e.target;
        if (!target.classList.contains("fa-chevron-right") && !target.classList.contains("fa-chevron-down")) {
            return;
        }
        if (target.classList.contains("fa-chevron-right")) {
            target.classList.replace("fa-chevron-right", "fa-chevron-down");
            groupSection.Children.forEach(x => x.Show = false);
        } else {
            target.classList.replace("fa-chevron-down", "fa-chevron-right");
            groupSection.Children.forEach(x => x.Show = true);
        }
    }

    removeRowById(id) {
        const index = this.rowData.Data.findIndex(x => x[this.idField].toString() === id);
        if (index < 0) {
            return;
        }
        this.rowData.Data.splice(index, 1);
        this.filterChildren(x => x instanceof ListViewItem && x.Entity[this.idField].toString() === id)
            .forEach(x => {
                if (x instanceof ListViewItem) {
                if (x.groupSection && x.groupSection.Entity instanceof GroupRowData) {
                    const groupChildren = x.groupSection.Entity.Children;
                    groupChildren.remove(x.Entity);
                    if (!groupChildren.length) {
                        this.rowData.Data.remove(x.groupSection.Entity);
                        x.groupSection.Dispose();
                    }
                }
            }
                x.Dispose();
            });
        if (!this.rowData.Data.length) {
            this.noRecordFound();
        }
    }

    removeRange(data) {
        data.forEach(x => this.removeRowById(x[this.idField].toString()));
    }

    async addOrUpdateRow(rowData, singleAdd = true, force = false, fields = []) {
        let existRowData = this.filterChildren(x => x instanceof ListViewItem && x.Entity === rowData).pop(); // pop() lấy phần tử cuối cùng tương đương firstOrDefault

        if (!existRowData) {
            await this.addRow(rowData, 0, singleAdd);
            return;
        }

        if (existRowData.emptyRow) {
            existRowData.Entity = null;
            await this.addRow(rowData, 0, singleAdd);
        } else {
            existRowData.Entity.copyPropFrom(rowData);
            // @ts-ignore
            this.rowAction(x => x instanceof ListViewItem && x.Entity === existRowData.Entity, x => {
                if (x instanceof ListViewItem) {
                    x.emptyRow = false;
                    x.updateView(force, fields);
                    x.Dirty = true;
                }
            });
        }

        if (singleAdd) {
            this.addNewEmptyRow();
        }
    }
}
