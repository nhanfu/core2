import { Client } from "./clients";
import { ConfirmDialog } from "./confirmDialog";
import { EditableComponent } from "./editableComponent";
import { Section } from "./section";
import { ComponentExt } from "./utils/componentExt";
import { Html } from "./utils/html";
import { Utils } from "./utils/utils";
import { Component, ExcelExt, ElementType } from "./models";

export class PdfReport extends EditableComponent {
    static errorMessage = "errorMessage";
    static dataNotFound = "Không tìm thấy dữ liệu";
    static templateNotFound = "Template is null or empty";

    /**
    * @param {Component} ui 
    * @param {HTMLElement} ele
    */
    constructor(ui, ele = null) {
        super(ui);
        if (!ui) throw new Error("argumentNullException: ui");
        this.element = ele;
        this.Selected = null;
        this.Data = null;
        this.hiddenButton = false;
        this._rptContent = null;
    }

    Render() {
        Html.take(this.parentElement);
        this._rptContent = Html.getContext();
        this.element = Html.getContext();
        this.renderInternal();
    }

    renderInternal() {
        this.disposeChildren();
        this.templateLoaded().then();
    }

    async templateLoaded() {
        var html = await this.loadData();
        this.parentElement.innerHTML = html;
        window.setTimeout(() => {
            this.element.querySelectorAll("tbody[data-table]").forEach(ele => {
                if (ele.children.length == 0) {
                    ele.parentElement.remove();
                }
            });
        }, 100);
    }

    cloneRow(templateRow) {
        let res = [];
        for (let i = 0; i < templateRow.length; i++) {
            res.push(templateRow[i].cloneNode(true));
        }
        return res;
    }

    async loadData() {
        var entity2 = this.entity;
        if (this.Parent.isAction) {
            entity2 = this.Parent.entity;
        }
        var gridViews = this.editForm.childCom.filter(x => x.isListView);
        var entity = JSON.parse(JSON.stringify(entity2));
        gridViews.forEach((grid, index) => {
            entity["t" + index] = grid.allListViewItem.filter(x => !x.groupRow).map(x => x.Entity);
            entity["t" + index + "h"] = grid.Header;
        })
        try {
            var res = await Client.instance.postAsync({ comId: this.meta.Id, Data: entity }, "/api/createHtml");
            return res;
        } catch (error) {
            return error.Message;
        }
    }

    updateView(force = false, dirty = null, ...componentNames) {
        this.Data = null;
        window.clearTimeout(this._updateViewAwaiter);
        this._updateViewAwaiter = window.setTimeout(() => this.renderInternal(), 200);
    }
}
