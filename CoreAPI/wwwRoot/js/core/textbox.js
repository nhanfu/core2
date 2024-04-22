import EditableComponent from './editableComponent.js';
import { GetPropValue, DecodeSpecialChar , EncodeSpecialChar, Utils, SetComplexPropValue} from './utils.js';
import { HtmlEvent, Direction, HTML, html } from './html.js';
import { Utils } from "./utils/utils.js";
import { ComponentType, IdField } from './const.js';


class Textbox extends EditableComponent {
    constructor(meta, ele) {
        super(meta, ele);
        this.DefaultValue = '';
        if (ele instanceof HTMLInputElement) {
        this.Input = ele;
        } 
        else if (ele instanceof HTMLTextAreaElement) {
        this.TextArea = ele;
        }
    }
    Render() {
        SetDefaultVal();
        var val = GetPropValue(this.Entity, this.FieldName);
        if (!val && typeof val === "string" && EditForm != null && EditForm.Feature != null && !EditForm.Feature.IgnoreEncode) {
            const ecode = DecodeSpecialChar(val);
            const Encode = EncodeSpecialChar(ecode);
            SetComplexPropValue(this.Entity, this.FieldName, Encode);
        }
        var text = val?.ToString();
        if (!this.Meta.FormatData)
            {
                text = Utils.FormatEntity(Meta.FormatData, val);
            }
        if (!this.Meta.FormatEntity)
        {
            text = Utils.FormatEntity(this.Meta.FormatEntity, this.Entity);
        }
        const _text = this.EditForm != null && this.EditForm.Feature != null && this.EditForm.Feature.IgnoreEncode ? text : DecodeSpecialChar(text);
        if (this.MultipleLine || this.TextArea != null)
        {
            if (this.TextArea == null)
            {
                html.Take(this.ParentElement).TextArea.Value(_text).PlaceHolder(Meta.PlainText);
                html.Context instanceof HTMLTextAreaElement ? html.Context : null
            }
        }
    }
}

window.Core2 = window.Core || {};
window.Core2.Textbox = Textbox;

export default Textbox;