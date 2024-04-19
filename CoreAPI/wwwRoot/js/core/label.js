import EditableComponent from './editableComponent.js';
import { GetPropValue, string } from './utils.js';
import { Utils } from "./utils/utils.js";
import { HtmlEvent, Direction, HTML, html } from './html.js';
import { ComponentType, IdField } from './const.js';

class Label extends EditableComponent {
    constructor(meta, ele) {
        super(meta, ele);
    }

    Render() {
        this.SetDefaultVal();
        const cellData = GetPropValue(this.Entity, this.FieldName);
        const isBool = cellData instanceof Boolean;
        var cellText = null;
        if (this.Element == null) {
            this.RenderNewEle(cellText, cellData, isBool);
        }
        var formatter = {};
        if (this.Meta.Query?.HasNonSpaceChar()
            && Utils.IsFunction(this.Meta.FormatEntity, formatter)) {
            this.RenderCellText(formatter);
            return;
        }
        else {
            cellText = this.CalcCellText(cellData);
            this.UpdateEle(cellText, cellData, isBool);
        }
    }
    RenderNewEle(cellText, cellData, isBool) {
        html.Take(this.ParentElement).Style(this.CalcTextAlign(this.Meta, cellData));
        if (isBool) {
            if (this.Meta.SimpleText) {
                html.Text(cellData ? '☑' : '☐');
                html.Context.style.fontSize = "1.2rem";
            } else {
                html.Padding(Direction.bottom, 0)
                    .SmallCheckbox(GetPropValue(this.Entity, this.FieldName));
                html.Context.PreviousElementSibling.disabled = true;
            }
        } else {
            var containDiv = cellText?.subStr(0, 5)?.includes('<div>');
            if (containDiv) {
                html.Div.Render();
            } else {
                html.Span.Render();
            }
            html.Event(HtmlEvent.click, this.LabelClickHandler).ClassName("cell-text").InnerHTML(cellText);
        }
        this.Element = html.Context;
        html.End.Render();
    }

    LabelClickHandler(e) {

    }

    CalcTextAlign(header, cellData) {
        if (header.ComponentType == 'Number') return 'right';
        if (header.RefName != null) return 'left';
        if (cellData instanceof Boolean) return 'center';
        return 'center';
    }
     
    CalcCellText(cellData) {
        var header = this.Meta;
        /** @type {string} */
        var fieldText = header.FieldText;
        var isRef = fieldText?.HasNonSpaceChar();
        if (this.EmptyRow) return '';
        if (header.FieldName == IdField && cellData === string.Empty) return '';
        let fn = {};
        if (Utils.IsFunction(header.FormatEntity, fn))
        {
            return fn.v.call(row, row).ToString();
        }
        if (cellData == null)
        {
            return header.PlainText ?? string.Empty;
        }
        if (isRef)
        {
            if (header.FieldText?.IsNullOrWhiteSpace()) return string.Empty;
            if (header.DisplayField == null) {
                const parts = header.FieldText.split('.');
                header.DisplayField = parts[0];
                header.DisplayDetail = parts[1];
            }
            var display = GetPropValue(this.Entity, header.DisplayField);
            if (display != null && typeof display === 'string') {
                display = JSON.parse(display);
            }
            return GetPropValue(display, header.DisplayDetail);
        }
        else
        {
            return cellData.toString();
        }
    }

    UpdateEle(cellText, cellData, isBool) {
        if (isBool)
        {
            if (this.Meta.SimpleText)
            {
                this.Element.InnerHTML = cellData === true ? "☑" : "☐";
            }
            else
            {
                this.Element.PreviousElementSibling.checked = cellData;
            }
            return;
        }
        this.Element.innerHTML = cellText;
        this.Element.setAttribute("title", cellText);
    }
}

window.Core2 = window.Core || {};
window.Core2.Label = Label;

export default Label;