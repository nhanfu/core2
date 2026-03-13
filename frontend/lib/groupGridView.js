import { GridView } from "./gridView.js";
import { GroupViewItem } from "./groupViewItem.js";
import { ListViewItem } from "./listViewItem.js";
import { EventType, ElementType } from "./models/";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { Section } from "./index.js";

export class GroupRowData {
    constructor() {
        this.Key = null;
        this.Children = [];
    }
}

export class GroupGridView extends GridView {

    constructor(ui) {
        super(ui);
    }
    render() {
        super.render();
        Html.take(this.element).className("group-table").end.render();
    }
    renderContent() {
        if (!this.loadRerender) {
            this.header = this.header.filter(x => !x.Hidden);
            this.renderTableHeader(this.header);
            this.loadRerender = true;
        }
        if (this.editable) {
            this.addNewEmptyRow();
        }
        this.addSections();
        this.formattedRowData = this.rowData.Data;
        if (this.formattedRowData.length == 0) {
            return;
        }
        this.mainSection.Show = false;
        this.mainSection.disposeChildren();
        this.formattedRowData.forEach((row, index) => {
            Html.take(this.mainSection.element);
            this.renderRowData1(this.header, row, this.mainSection, null);
        });
        this.updateStickyColumns();
        this.mainSection.Show = true;
        this.contentRendered();
        window.setTimeout(() => {
            this.renderIndex();
        }, 500);
    }

    async applyFilter() {
        this.mainSection.disposeChildren();
        return super.applyFilter();
    }

    noRowData(list) {
        if (this.editable) {
            this.addNewEmptyRow();
        } else if (list.nothing()) {
            this.noRecordFound();
        }
    }

    /**
     * @param {ListViewItem} rowSection
     */
    moveEmptyRow(rowSection) {
        let groupSection1 = this.allListViewItem.find(group => group.groupRow && group.entity[this._groupKey] === rowSection.entity[this._groupKey]);
        if (groupSection1) {
            var tr = rowSection;
            tr.Parent = this.mainSection;
            tr.listViewSection = this.mainSection;
            tr.groupSection = groupSection1;
            tr.element.classList.add("group-detail");
            var lastChild = groupSection1.childrenItems[groupSection1.childrenItems.length - 1];
            var index = this.allListViewItem.indexOf(lastChild);
            if (this.allListViewItem.length == index + 1) {
                this.mainSection.element.appendChild(tr.element);
            }
            else {
                this.mainSection.element.insertBefore(tr.element, this.allListViewItem[index + 1].element);
            }
            this.mainSection.Children.splice(index + 1, 0, tr);
            groupSection1.childrenItems.push(tr);
            this.Dirty = true;
            return tr;
        }
        else {
            Html.take(this.mainSection);
            let first = rowSection.entity;
            var groupSection = new GroupViewItem(ElementType.tr);
            groupSection.Key = rowSection.entity[this._groupKey];
            groupSection.entity = first;
            groupSection.parentElement = this.mainSection.element;
            groupSection.listViewSection = true;
            groupSection.listViewSection = this.mainSection;
            groupSection.listView = this;
            this.mainSection.addChild(groupSection);
            groupSection.element.tabIndex = -1;
            var groupText = Utils.isFunction(this.meta.groupFormat, false, groupSection);
            Html.instance.tData.className("status-cell").tabIndex(-1).event(EventType.Click, () => groupSection.showChildren1 = !groupSection.showChildren1).icon("fal fa-square");
            groupSection.Chevron = Html.context;
            Html.instance.end.end.tData.event(EventType.Click, () => this.dispatchClick(first))
                .event(EventType.dblClick, () => this.dispatchDblClick(first))
                .div.className("d-flex");
            groupSection.groupText = Html.context;
            Html.instance.innerHTML(groupText);
            Html.instance.endOf(ElementType.td);
            this.header.slice(2).forEach(item => {
                Html.instance.tData.attr("component", "Number").className("data-summary").style("font-weight:600");
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
            this.mainSection.Children.splice(index + 1, 0, rowSection);
            return rowSection;
        }
    }

    addRow(row, fromIndex, singleAdd = true) {
        if (!Utils.isNullOrWhiteSpace(this.meta.groupBy)) {
            let keys = this.meta.groupBy.split(",");
            row[this._groupKey] = keys.map(key => row[key]).join(" ");
        }
        let groupSection1 = this.allListViewItem.find(group => group.groupRow && group.entity[this._groupKey] === row[this._groupKey]);
        if (groupSection1) {
            var tr = super.renderRowData(this.header, row, this.mainSection, fromIndex, false);
            this.moveGroupRow(tr);
            this.Dirty = true;
            return tr;
        }
        else {
            Html.take(this.mainSection);
            let first = row;
            var groupSection = new GroupViewItem(ElementType.tr);
            groupSection.Key = row[this._groupKey];
            groupSection.entity = row;
            groupSection.parentElement = this.mainSection.element;
            groupSection.listViewSection = true;
            groupSection.listViewSection = this.mainSection;
            groupSection.listView = this;
            this.mainSection.addChild(groupSection);
            groupSection.element.tabIndex = -1
            var groupText = Utils.isFunction(this.meta.groupFormat, false, groupSection);
            Html.instance.tData.className("status-cell").tabIndex(-1).event(EventType.Click, () => groupSection.showChildren1 = !groupSection.showChildren1).icon("fal fa-square");
            groupSection.Chevron = Html.context;
            Html.instance.end.end.tData.event(EventType.Click, () => this.dispatchClick(first))
                .event(EventType.dblClick, () => this.dispatchDblClick(first))
                .div.className("d-flex");
            groupSection.groupText = Html.context;
            Html.instance.innerHTML(groupText);
            Html.instance.endOf(ElementType.td);
            this.header.slice(2).forEach(item => {
                Html.instance.tData.attr("component", "Number").className("data-summary").style("font-weight:600");
                var sec = new Section(null, Html.context);
                sec.meta = item;
                groupSection.addChild(sec);
                Html.instance.endOf(ElementType.td);
            });
            Html.instance.endOf(ElementType.tr);
            Html.take(this.mainSection.element);
            let rowSection = super.renderRowData(this.header, row, this.mainSection);
            rowSection.element.classList.add("group-detail");
            groupSection.childrenItems.push(rowSection);
            rowSection.groupSection = groupSection;
            this.Dirty = true;
            return rowSection;
        }
    }

    // @ts-ignore
    renderRowData1(headers, row, section, index, emptyRow = false) {
        let groupSection1 = this.allListViewItem.find(group => group.groupRow && group.entity[this._groupKey] === row[this._groupKey]);
        if (groupSection1) {
            var tr = super.renderRowData(headers, row, section, index, emptyRow);
            tr.groupSection = groupSection1;
            tr.element.classList.add("group-detail");
            groupSection1.childrenItems.push(tr);
            return tr;
        }
        Html.take(section.element);
        let first = row;
        var groupSection = new GroupViewItem(ElementType.tr);
        groupSection.Key = row[this._groupKey];
        if (this.meta.isMultiple) {
            groupSection.entity = JSON.parse(JSON.stringify(row[this.meta.groupBy.substr(0, this.meta.groupBy.length - 2)]));
            groupSection.entity[this._groupKey] = row[this._groupKey];
        }
        else {
            groupSection.entity = row;
        }
        groupSection.parentElement = section.element;
        groupSection.listViewSection = true;
        groupSection.listViewSection = section;
        groupSection.listView = this;
        section.addChild(groupSection);
        groupSection.element.tabIndex = -1;
        if (!this.meta.isMultiple) {
            var groupText = Utils.isFunction(this.meta.groupFormat, false, groupSection);
            Html.instance.tData.className("status-cell").tabIndex(-1).event(EventType.Click, () => groupSection.showChildren1 = !groupSection.showChildren1).icon("fal fa-square");
            groupSection.Chevron = Html.context;
            Html.instance.end.end.tData.dataAttr("field", this.header[1].fieldName).event(EventType.dblClick, () => this.dispatchDblClick(first))
                .div.className("d-flex");
            groupSection.groupText = Html.context;
            Html.instance.innerHTML(groupText);
            Html.instance.event(EventType.Click, () => groupSection.showChildren = !groupSection.showChildren)
            Html.instance.endOf(ElementType.td);
            this.header.slice(2).forEach(item => {
                Html.instance.tData.attr("component", "Number").dataAttr("field", item.fieldName).tabIndex(-1).event(EventType.Click, () => groupSection.showChildren = !groupSection.showChildren).className("data-summary").style("font-weight:600");
                var sec = new Section(null, Html.context);
                sec.meta = item;
                groupSection.addChild(sec);
                Html.instance.endOf(ElementType.td);
            });
        }
        else {
            Html.instance.tData.className("status-cell").tabIndex(-1).event(EventType.Click, () => groupSection.showChildren1 = !groupSection.showChildren1).icon("fal fa-square");
            groupSection.Chevron = Html.context;
            Html.instance.end.endOf(ElementType.td)
            this.header.slice(1).forEach(item => {
                Html.instance.tData.attr("component", "Number").tabIndex(-1).event(EventType.Click, () => groupSection.showChildren = !groupSection.showChildren).className("data-group");
                groupSection.renderTableCell(groupSection.entity, item);
                Html.instance.endOf(ElementType.td);
            });
        }
        Html.instance.endOf(ElementType.tr);
        Html.take(section.element);
        let rowSection = super.renderRowData(headers, row, section);
        rowSection.element.classList.add("group-detail");
        groupSection.childrenItems.push(rowSection);
        rowSection.groupSection = groupSection;
        return groupSection;
    }

    dispatchClick(row) {
        this.dispatchEvent(this.meta.groupEvent, EventType.Click, row).then();
    }

    dispatchDblClick(row) {
        this.dispatchEvent(this.meta.groupEvent, EventType.dblClick, row).then();
    }

    toggleAll() {
        let allSelected = this.allListViewItem
            .filter(x => !x.groupRow && !x.emptyRow)
            .every(x => x.Selected);
        if (allSelected) {
            this.clearSelected();
        } else {
            this.rowAction(x => {
                if (x instanceof ListViewItem) {
                    x.Selected = !x.groupRow && !x.emptyRow;
                }
            });
        }
    }

    removeRowById(id) {
        let index = this.rowData.Data.findIndex(x => x[this.idField].toString() === id);
        if (index < 0) {
            return;
        }

        this.rowData.Data.splice(index, 1);
        this.filterChildren(x => x instanceof ListViewItem && x.entity[this.idField].toString() === id)
            .forEach(x => {
                if (x instanceof ListViewItem && x.groupSection && x.groupSection.Entity instanceof GroupRowData) {
                    let groupChildren = x.groupSection.Entity.Children;
                    groupChildren.splice(groupChildren.indexOf(x.entity), 1);
                    if (groupChildren.length === 0) {
                        this.rowData.Data.splice(this.rowData.Data.indexOf(x.groupSection.Entity), 1);
                        x.groupSection.Dispose();
                    }
                }
                x.Dispose();
            });
        this.noRowData(this.rowData.Data);
    }

    removeRange(data) {
        data.forEach(x => this.removeRowById(x[this.idField].toString()));
    }

    async addRows(rowsData) {
        let listItem = [];
        await Promise.all(rowsData.map(async x => {
            listItem.push(this.addRow(x, null, false));
        }));
        this.renderIndex();
        this.domLoaded();
        return listItem;
    }

    async addOrUpdateRow(rowData, singleAdd = true, force = false, ...fields) {
        let existRowData = this
            .filterChildren(x => x instanceof ListViewItem && x.entity[this.idField] === rowData[this.idField])
            .find(x => true);
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
            this.rowAction(x => x.entity === existRowData.entity, x => x.updateView({ force, componentNames: fields }));
        }
    }

    // @ts-ignore
    renderRowData(headers, row, section, index, emptyRow = false) {
        if (!(row instanceof GroupRowData)) {
            return super.renderRowData(headers, row, section, index, emptyRow);
        }
        if (!(section.element instanceof hTMLTableSectionElement)) {
            throw new Error("The section is not an HTML table element");
        }
        Html.take(section.element);
        if (row.Key === null || row.Key.toString().trim() === "") {
            let rowResult = null;
            row.Children.forEach(child => {
                Html.take(section.element);
                rowResult = super.renderRowData(headers, child, section, null);
            });
            return rowResult;
        }
        let first = row.Children[0];
        let groupSection = new GroupViewItem({
            type: ElementType.tr,
            Entity: row,
            parentElement: section.element,
            groupRow: true,
            listViewSection: section,
            listView: this
        });

        section.addChild(groupSection);
        groupSection.element.tabIndex = -1;
        var groupText = Utils.isFunction(this.meta.groupFormat, false, this);
        if (!groupText) {
            groupText = Utils.formatEntity2(this.meta.groupFormat, null, first, Utils.emptyFormat, Utils.emptyFormat);
        }
        if (this.meta.groupReferenceId) {
            let val = first[this.meta.groupBy.substr(0, this.meta.groupBy.length - 2)];
            groupSection.entity = val;
            groupSection.entity["modelName"] = this.meta.refName;
            headers.filter(x => !x.Hidden).forEach(header => {
                Html.instance.tData.tabIndex(-1)
                    .style(header.Style)
                    .event(EventType.focusIn, e => this.focusCell(e, header))
                    .dataAttr("field", header.fieldName).render();
                let td = Html.context;
                groupSection.renderTableCell(val, header, td);
                Html.instance.endOf(ElementType.td);
            });
        } else {
            Html.instance.tData.className("status-cell").icon("mif-pencil").endOf(ElementType.td)
                .tData.colSpan(headers.length - 1)
                .event(EventType.Click, () => this.dispatchClick(first))
                .event(EventType.dblClick, () => this.dispatchDblClick(first))
                .icon("fa fa-chevron-down").event(EventType.Click, () => groupSection.showChildren = !groupSection.showChildren).end
                .div.className("d-flex").innerHTML(groupText);
            groupSection.groupText = Html.context;
            groupSection.Chevron = Html.context.previousElementSibling;
            groupSection.Chevron.parentElement.previousElementSibling.appendChild(groupSection.Chevron);
            Html.instance.endOf(ElementType.td);
        }
        Html.instance.endOf(ElementType.tr);
        row.Children.forEach(child => {
            Html.take(section.element);
            let rowSection = super.renderRowData(headers, child, section);
            rowSection.element.addClass("group-detail");
            groupSection.childrenItems.push(rowSection);
            rowSection.groupSection = groupSection;
        });
        return groupSection;
    }
    /**
     * Updates pagination details based on the current data state.
     */
    renderIndex() {
        if (this.mainSection.Children.length === 0) {
            return;
        }
        var indexText = 0;
        this.allListViewItem.forEach((row, rowIndex) => {
            indexText++;
            for (let i = 0; i < row.element.children.length; i++) {
                const element = row.element.children[i];
                element.dataset.row = rowIndex;
                element.dataset.col = i;

                if (!this.Matrix[rowIndex]) this.Matrix[rowIndex] = [];
                this.Matrix[rowIndex][i] = element;
            }
            if (row.groupRow) {
                indexText--;
                return;
            }
            var previous = row.firstChild.element.closest("td").previousElementSibling;
            if (!previous) {
                return;
            }
            if (row.emptyRow) {
                previous.innerHTML = "<i class='fal fa-plus'></i>";
            }
            else {
                previous.innerHTML = indexText.toString();
                row.rowNo = indexText - 1;
            }
        });
    }
}
