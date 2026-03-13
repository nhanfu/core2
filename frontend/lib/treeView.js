import { ListView } from "listView";
import { Spinner } from "spinner";
import { Html } from "utils/html";
import { Utils } from "utils/utils";
import { SqlViewModel } from "models/sqlViewModel";
import { ListViewItem } from "listViewItem";
import EventType from "models/eventType";
import { Client } from "clients/client";
import { ElementType } from "models/elementType";

export class TreeView extends ListView {
    constructor(ui) {
        super(ui);
    }

    Rerender() {
        this.disposeNoRecord();
        this.Header = this.Header.filter(x => !x.Hidden);
        this.mainSection.element.addClass("overflow");
        const firstData = this.formattedRowData.nothing() ? this.rowData.Data : this.formattedRowData;
        this.renderContent(this.Header, this.mainSection, true, firstData);
        this.mainSection.disposeChildren();
        if (this.Editable) {
            this.addNewEmptyRow();
        } else if (this.rowData.Data.nothing()) {
            this.noRecordFound();
            this.domLoaded();
            return;
        }
        if (this.mainSection.element instanceof hTMLTableSectionElement) {
            this.mainSection.element.addEventListener(EventType.contextMenu, this.bodyContextMenuHandler.bind(this));
        }
        this.domLoaded();
        Spinner.Hide();
    }

    renderContent(headers, node, first, rowDatas) {
        if (rowDatas.nothing()) {
            return;
        }
        Html.take(node.element).ul.className((!first ? "d-block " : " ") + (first ? " wtree" : " "));
        const ul = Html.Context;
        rowDatas.forEach(async (row) => {
            this.renderRow(headers, node, row, ul);
        });
    }

    renderRow(headers, node, row, ul) {
        const params = Utils.isFunction(this.meta.preQuery, false, this);
        // @ts-ignore
        const data = Client.Instance.comQuery(new SqlViewModel({
            metaConn: this.metaConn,
            dataConn: this.dataConn,
            comId: this.meta.Id,
            Params: params
        })).Done(ds => {
            const datas = ds.length > 0 ? ds[0].toList() : null;
            const count = ds.length > 1 && ds[1].length > 0 ? ds[1].total : 0;
            Html.take(ul);
            const rowSection = new ListViewItem(ElementType.li,
                // @ts-ignore
                {
                    Entity: row,
                    listViewSection: this.mainSection
                });
            node.addChild(rowSection);
            Html.Instance.Div.className(count > 0 ? "has" : "").render();
            const label = Html.Context;
            headers.forEach(header => {
                const com = header;
                Html.take(label).P.render();
                rowSection.renderTableCell(row, com);
                Html.take(label).endOf(ElementType.p);
            });
            if (count > 0) {
                rowSection.element.addEventListener(EventType.Click, () => this.focusIn(rowSection, row, datas));
            }
        });
    }


    focusIn(listViewItem, row, datas) {
        const ul = listViewItem.element.querySelector("ul");
        if (listViewItem.element.hasClass("expanded")) {
            ul.removeClass("d-block");
            ul.addClass("d-none");
            listViewItem.element.removeClass("expanded");
        } else {
            listViewItem.element.addClass("expanded");
            if (!ul) {
                this.renderContent(this.Header, listViewItem, false, datas);
            } else {
                ul.removeClass("d-none");
                ul.addClass("d-block");
                listViewItem.element.addClass("expanded");
            }
        }
    }

    calcFilterQuery() {
        let res = super.calcFilterQuery();
        const resetSearch = this.listViewSearch.entityVM.searchTerm && this.advSearchVM.Conditions.nothing();
        if (!resetSearch) {
            let filterPart = odataExt.getClausePart(res, odataExt.filterKeyword);
            filterPart = filterPart.replace(new regExp("((and|or) )?Parent(\\w|\\W)* eq null( (and|or)$)?", "g"), "");
            filterPart = filterPart.replace(new regExp("^Parent(\\w|\\W)* eq null( (and|or))?", "g"), "");
            res = odataExt.applyClause(res, filterPart);
        }
        return res;
    }
}