import EditableComponent from './editableComponent.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";

class Textbox extends EditableComponent {
    constructor(meta, ele) {
        super(meta, ele);
        this.DefaultValue = '';
        this.Input = null;
        this.TextArea = null;
        this.MultipleLine = false;
        this.Password = false;
        this._value = null;
        this._text = '';
        this._oldText = '';
        if (ele instanceof HTMLInputElement) {
        this.Input = ele;
        } 
        else if (ele instanceof HTMLTextAreaElement) {
        this.TextArea = ele;
        }
    }
    Render() {
        this.SetDefaultVal();
        var val = GetPropValue(this.Entity, this.FieldName);
        if (!val && typeof val === "string" && this.EditForm != null && this.EditForm.Feature != null && !this.EditForm.Feature.IgnoreEncode) {
            const ecode = Utils.DecodeSpecialChar(val);
            const Encode = Utils.EncodeSpecialChar(ecode);
            this.Entity && SetComplexPropValue(this.Entity, this.FieldName, Encode);
        }
        var text = val?.ToString();
        if (!this.Meta.FormatData)
            {
                text = Utils.FormatEntity(this.Meta.FormatData, val);
            }
        if (!this.Meta.FormatEntity)
        {
            text = Utils.FormatEntity(this.Meta.FormatEntity, this.Entity);
        }
         this._text = this.EditForm != null && this.EditForm.Feature != null && this.EditForm.Feature.IgnoreEncode ? text : DecodeSpecialChar(text);
        if (this.MultipleLine || this.TextArea != null)
        {
            if (this.TextArea == null)
            {
                Html.Take(this.ParentElement).TextArea.Value(_text).PlaceHolder(Meta.PlainText);
                this.Element = this.TextArea = Html.Context instanceof HTMLTextAreaElement ? Html.Context : null;
                
            }
            else
            {
                this.Element = this.TextArea;
                this.TextArea.Value = this._text;
            }
            if (this.Meta.Row > 0)
            {
                    Html.Instance.Attr("rows", this.Meta.Row ?? 1);
            }
            this.TextArea.oninput += (e) => PopulateUIChange(EventType.Input);
            this.TextArea.onchange += (e) => PopulateUIChange(EventType.Change);
        }
        if (this.Meta.ChildStyle && this.Meta.ChildStyle.trim() !== "") {
            var fn;
            if (Utils.IsFunction(Meta.ChildStyle, function(result) {
                fn = result;
            })) {
                fn.call(this, Entity, this.Element).toString();
            }
        }
        if (this.Password)
        {
            Html.Instance.Style("text-security: disc;-webkit-text-security: disc;-moz-text-security: disc;");
        }
        if (!this.Meta.ShowLabel) 
        {
                Html.Instance.PlaceHolder(Meta.PlainText);
        }
        this.Element.Closest("td")?.AddEventListener(EventType.KeyDown, ListViewItemTab);
        this.DOMContentLoaded?.Invoke();
    }
    PopulateUIChange(type, shouldTrim = false) {
        if (this.Disabled)
            {
                return;
            }
        this._oldText = this._text;
        this._text = this.Input ? this.Input.Value : this.TextArea.Value;
        this._text = this.Password ? this._text : (shouldTrim ? this._text?.trim() : this._text);
        if (this.Meta.UpperCase && this._text != null)
            {
                // Text = _text.ToLocaleUpperCase();
            }
            this._value = (this.EditForm != null && this.EditForm.Feature != null && this.EditForm.Feature.IgnoreEncode) ? this._text : EncodeSpecialChar(this._text);
            this.Entity && SetComplexPropValue(this.Entity,this.FieldName, this._value);
            this.Dirty = true;
            // this.UserInput?.Invoke(new ObservableArgs { NewData = _text, OldData = _oldText, EvType = type });
    }
}

window.Core2 = window.Core || {};
window.Core2.Textbox = Textbox;

export default Textbox;