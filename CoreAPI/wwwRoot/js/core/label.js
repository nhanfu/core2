import { Component } from "./models/component.js";
import { Client } from "./clients/client.js";
import EditableComponent from "./editableComponent.js";
import { Direction, Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";


export class Label extends EditableComponent {
    /**
     * @param {Component} ui
     * @param {HTMLElement} [ele=null] 
*/
    constructor(ui, ele = null) {
        super(ui, ele);
    }

    Render() {
        this.SetDefaultVal();
        const cellData = Utils.GetPropValue(this.Entity, this.Name);
        const isBool = cellData !== null && typeof cellData === "boolean";
        let cellText = '';

        if (!this.Element) {
            this.RenderNewEle(cellText, cellData, isBool);
        }
        var formatter = Utils.IsFunction(this.Meta.Renderer);
        if (this.Meta.PreQuery) {
            this.QueryCellText(formatter);
            return;
        } else {
            let finalCellText = this.CalcCellText(cellData, formatter);
            if (finalCellText) this.UpdateEle(finalCellText, cellData, isBool);
        }
        this.Element.closest("td")?.addEventListener("keydown", this.ListViewItemTab);
        this.Element.parentElement.tabIndex = -1;
    }

    UpdateEle(cellText, cellData, isBool) {
        if (isBool) {
            if (this.Meta.SimpleText) {
                this.Element.innerHTML = cellData === true ? "☑" : "☐";
            } else {
                if (this.Element.previousElementSibling instanceof HTMLInputElement) {
                    this.Element.previousElementSibling.checked = cellData;
                }
            }
            return;
        }
        this.Element.innerHTML = cellText;
        this.Element.setAttribute("title", cellText);
    }

    RenderNewEle(cellText, cellData, isBool) {
        Html.Take(this.ParentElement).TextAlign(this.CalcTextAlign(this.Meta, cellData));
        if (isBool) {
            if (this.Meta.SimpleText) {
                Html.Instance.Text(cellData === true ? "☑" : "☐");
                Html.Context.style.fontSize = "1.2rem";
            } else {
                Html.Instance.Padding(Direction.bottom, 0)
                    .SmallCheckbox(cellData);
                    if (Html.Context.previousElementSibling instanceof HTMLInputElement) {
                        Html.Context.previousElementSibling.disabled = true;
                    }
            }
        } else {
            const containDiv = cellText.substring(0, 4) === "<div>";
            if (containDiv) {
                Html.Instance.Div.Render();
            } else {
                Html.Instance.Span.Render();
            }
            Html.Instance.Event("click", this.LabelClickHandler.bind(this)).ClassName("cell-text").InnerHTML(cellText);
        }
        this.Element = Html.Context;
        Html.Instance.End.Render();
    }

    CalcCellText(cellData, formatter) {
        let cellText = null;
        if (this.Meta.IsPivot) {
            const fields = this.Name.split(".");
            if (fields.length < 3) {
                return cellText;
            }
            const listData = Utils.GetPropValue(this.Entity, fields[0]);
            const restPivotField = fields.slice(1, -1).join(".");
            const row = listData.find(x => x.GetPropValue(restPivotField).toString() === fields.at(-1).toString());
            cellText = row ? Utils.FormatEntity(this.Meta.FormatEntity, row) : "";
        } else {
            cellText = Utils.GetCellText(this.Meta, cellData, this.Entity, this.EmptyRow);
        }
        if (cellText === null || cellText === "null") {
            cellText = "N/A";
        }
        return formatter ? formatter.call(this, this, cellText) : cellText;
    }

    QueryCellText(formatter) {
        if (!this.Meta.PreQuery || formatter === null) {
            return;
        }
        const fn = Utils.IsFunction(this.Meta.PreQuery);
        const entity = fn ? fn.call(this, this).toString() : "";
        const submit = {
            MetaConn: this.MetaConn,
            DataConn: this.DataConn,
            Params: JSON.stringify(entity),
            ComId: this.Meta.Id
        };
        // @ts-ignore
        Client.Instance.SubmitAsync({Method: "POST", Url: Utils.ComQuery, Value: JSON.stringify(submit)})
        .then(data => {
            if (data.Nothing()) {
                return;
            }
            const text = formatter.call(this, this, data).toString();
            this.UpdateEle(text, null, false);
        });
    }

    LabelClickHandler(e) {
        this.DispatchEvent(this.Meta.Events, "click", this.Entity);
    }

    /**
     * 
     * @param {Component} header 
     * @param {any} cellData 
     * @returns {string}
     */
    CalcTextAlign(header, cellData) {
        const textAlign = header.TextAlignEnum;
        if (textAlign) {
            return textAlign;
        }
        if (header.ReferenceId || cellData === null || typeof cellData === "string") {
            return "left";
        }
        if (typeof cellData === "number") {
            return "right";
        }
        if (typeof cellData === "boolean") {
            return "center";
        }
        return "center";
    }

    UpdateView(force = false, dirty = null, componentNames) {
        this.PrepareUpdateView(force, dirty);
        this.Render();
    }

    GetValueTextAct() {
        return this.Element.textContent;
    }
}