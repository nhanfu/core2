import { Client } from "./clients";
import { ConfirmDialog } from "./confirmDialog";
import { EditableComponent } from "./editableComponent";
import { Section } from "./section";
import { ComponentExt } from "./utils/componentExt";
import { Html } from "./utils/html";
import { Utils } from "./utils/utils";
import { Component, ExcelExt, ElementType } from "./models";

export class PdfReport extends EditableComponent {
    static ErrorMessage = "ErrorMessage";
    static DataNotFound = "Không tìm thấy dữ liệu";
    static TemplateNotFound = "Template is null or empty";

    /**
    * @param {Component} ui 
    * @param {HTMLElement} ele
    */
    constructor(ui, ele = null) {
        super(ui);
        if (!ui) throw new Error("ArgumentNullException: ui");
        this.Meta = ui;
        this.Element = ele;
        this.Selected = null;
        this.Data = null;
        this.HiddenButton = false;
        this._rptContent = null;
    }

    Render() {
        Html.Take(this.ParentElement);
        this._rptContent = Html.GetContext();
        this.Element = Html.GetContext();
        this.RenderInternal();
    }

    RenderInternal() {
        this.DisposeChildren();
        this.TemplateLoaded().then();
    }

    async TemplateLoaded() {
        var html = await this.LoadData();
        this.ParentElement.innerHTML = html;
        window.setTimeout(() => {
            this.Element.querySelectorAll("tbody[data-table]").forEach(ele => {
                if (ele.children.length == 0) {
                    ele.parentElement.remove();
                }
            });
        }, 100);
    }

    CloneRow(templateRow) {
        let res = [];
        for (let i = 0; i < templateRow.length; i++) {
            res.push(templateRow[i].cloneNode(true));
        }
        return res;
    }

    async LoadData() {
        var gridViews = this.EditForm.ChildCom.filter(x => x.IsListView);
        var entity = JSON.parse(JSON.stringify(this.Entity));
        gridViews.forEach((grid, index) => {
            entity["t" + index] = grid.AllListViewItem.filter(x => !x.GroupRow).map(x => x.Entity);
            entity["t" + index + "h"] = grid.Header;
        })
        try {
            var res = await Client.Instance.PostAsync({ ComId: this.Meta.Id, Data: entity }, "/api/CreateHtml");
            return res;
        } catch (error) {
            return error.Message;
        }
    }

    UpdateView(force = false, dirty = null, ...componentNames) {
        this.Data = null;
        window.clearTimeout(this._updateViewAwaiter);
        this._updateViewAwaiter = window.setTimeout(() => this.RenderInternal(), 200);
    }
}
