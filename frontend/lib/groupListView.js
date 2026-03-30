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

    render() {
        super.render();
        Html.take(this.element).className("group-listview").end.render();
    }

    addRow(item, fromIndex, singleAdd = true) {
        return new Promise((resolve) => {
            fromIndex = 0;
            this.disposeNoRecord();
            const keys = this.meta.groupBy.split(",");
            item[GroupListView._groupKey] = keys.map(key => item[key]?.toString()).join(" "); 
            const groupKey = item[GroupListView._groupKey]; 
            const existGroup = this.allListViewItem.find(group => group.groupRow && group.entity.As('GroupRowData').key === groupKey); 
    
            if (existGroup === null) {
                const groupData = new GroupRowData();
                groupData.key = groupKey;
                groupData.Children.push(item); 
                this.formattedRowData.push(groupData);
                const rowSection = this.renderRowData(this.header, groupData, this.mainSection, this.mainSection.Children.length);
                if (singleAdd) {
                    this.addNewEmptyRow(); 
                }
                this.Dirty = true; 
                resolve(rowSection);
            } else {
                existGroup.entity.As('GroupRowData').Children.push(item);
                const index = this.mainSection.Children.indexOf(existGroup);
                const rowSection = this.renderRowData(this.header, item, this.mainSection, index + existGroup.Children.length); // Thực hiện render dữ liệu cho item mới trong nhóm
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
        if (!row.key || row.key.toString().isNullOrWhiteSpace()) {
            let rowResult = null;
            row.Children.forEach(child => {
                Html.take(wrapper);
                rowResult = this.renderRowData(headers, child, listViewSection, null);
            });
            return rowResult;
        }
        let groupSection = new GroupViewItem(ElementType.div);
        groupSection.entity = row;
        groupSection.parentElement = wrapper;
        groupSection.groupRow = true;
        groupSection.preQueryFn = this._preQueryFn;
        groupSection.listView = this;
        groupSection.Meta = this.meta;
        listViewSection.addChild(groupSection);
        let first = row.Children[0];
        let groupText = Utils.formatEntity2(this.meta.groupFormat, null, first, x => "N/A", x => "N/A");
        Html.take(groupSection.element).event(EventType.Click, this.dispatchClick.bind(this), first)
            .event(EventType.dblClick, this.dispatchDblClick.bind(this), first)
            // @ts-ignore
            .Icon("fa fa-chevron-right").event(EventType.Click, this.toggleGroupRow.bind(this), groupSection).end
            .Span.innerHTML(groupText);
        groupSection.groupText = Html.context;
        row.Children.forEach(child => {
            Html.take(groupSection.element);
            let childRow = this.renderRowData(headers, child, groupSection, null);
            childRow.groupSection = groupSection;
            Html.take(childRow.element).smallCheckbox().render();
            let chk = Html.context.previousElementSibling;
            if(chk instanceof hTMLInputElement) {
                Html.instance.end.end.event(EventType.Click, (e) => {
                    e.preventDefault();
                    childRow.selected = !childRow.selected;
                    chk.checked = childRow.selected;
                });
            }
        });
        return groupSection;
    }

    dispatchClick(row) {
        this.dispatchEvent(this.meta.groupEvent, EventType.Click, row).then();
    }

    dispatchDblClick(row) {
        this.dispatchEvent(this.meta.groupEvent, EventType.dblClick, row).then();
    }

    toggleGroupRow(groupSection, e) {
        const target = e.target;
        if (!target.classList.contains("fa-chevron-right") && !target.classList.contains("fa-chevron-down")) {
            return;
        }
        if (target.classList.contains("fa-chevron-right")) {
            target.classList.replace("fa-chevron-right", "fa-chevron-down");
            groupSection.Children.forEach(x => x.show = false);
        } else {
            target.classList.replace("fa-chevron-down", "fa-chevron-right");
            groupSection.Children.forEach(x => x.show = true);
        }
    }

    removeRowById(id) {
        const index = this.rowData.data.findIndex(x => x[this.idField].toString() === id);
        if (index < 0) {
            return;
        }
        this.rowData.data.splice(index, 1);
        this.filterChildren(x => x instanceof ListViewItem && x.entity[this.idField].toString() === id)
            .forEach(x => {
                if (x instanceof ListViewItem) {
                if (x.groupSection && x.groupSection.entity instanceof GroupRowData) {
                    const groupChildren = x.groupSection.entity.children;
                    groupChildren.remove(x.entity);
                    if (!groupChildren.length) {
                        this.rowData.data.remove(x.groupSection.entity);
                        x.groupSection.dispose();
                    }
                }
            }
                x.dispose();
            });
        if (!this.rowData.data.length) {
            this.noRecordFound();
        }
    }

    removeRange(data) {
        data.forEach(x => this.removeRowById(x[this.idField].toString()));
    }

    async addOrUpdateRow(rowData, singleAdd = true, force = false, fields = []) {
        let existRowData = this.filterChildren(x => x instanceof ListViewItem && x.entity === rowData).pop(); // pop() lấy phần tử cuối cùng tương đương firstOrDefault

        if (!existRowData) {
            await this.addRow(rowData, 0, singleAdd);
            return;
        }

        if (existRowData.emptyRow) {
            existRowData.entity = null;
            await this.addRow(rowData, 0, singleAdd);
        } else {
            existRowData.entity.copyPropFrom(rowData);
            // @ts-ignore
            this.rowAction(x => x instanceof ListViewItem && x.entity === existRowData.entity, x => {
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
