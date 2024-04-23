import EditableComponent from './editableComponent.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { ValidationRule } from "./models/validationRule.js";
import { LangSelect } from "./langSelect.js";
import { Client } from "./clients/client.js";


class Textbox extends EditableComponent {
    constructor(meta, ele) {
        super(meta, ele);
        this.Input = null;
        this.TextArea = null;
        this.MultipleLine = false;
        this.Password = false;
        this._value = null;
        this._oldText = '';
        this._text = '';
        this.Text = '';
        this.DefaultValue = '';
        if (ele instanceof HTMLInputElement) {
        this.Input = ele;
        } 
        else if (ele instanceof HTMLTextAreaElement) {
        this.TextArea = ele;
        }
    }
    Render() {
        this.SetDefaultVal();
        var val = this.Entity && Utils.GetPropValue(this.Entity, this.FieldName);
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
            Utils.IsFunction(Meta.ChildStyle, function(fn) {
                if (fn !== null && typeof fn === 'function') {
                    fn.call(this, Entity, this.Element).toString();
                }
            });
            
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
                this.Text = this._text.toLocaleUpperCase();
            }
            this._value = (this.EditForm != null && this.EditForm.Feature != null && this.EditForm.Feature.IgnoreEncode) ? this._text : EncodeSpecialChar(this._text);
            this.Entity && SetComplexPropValue(this.Entity,this.FieldName, this._value);
            this.Dirty = true;
            this.UserInput?.Invoke({ NewData: this._text, OldData: this._oldText, EvType: type });
            this.PopulateFields();
            this.DispatchEvent(this.Meta.Events, type, this.Entity);

    }
    UpdateView(force = false, dirty = null, ...componentNames) {
        Value = this.Entity && Utils.GetPropValue(this.Entity,this.FieldName);
        if (!Dirty)
        {
            this.DOMContentLoaded?.Invoke();
            this.OldValue = this._text;
        }
    }
    validateRegEx(value, regText) {
        if (value === null) {
            return true;
        }
        var regEx = new RegExp(regText);
        var res = regEx.test(value);
        var rule = this.ValidationRules[ValidationRule.RegEx];
        if (!res && rule.RejectInvalid) {
            var end = this.Input.SelectionEnd;
            this.Text = this._oldText;
            this._value = this._oldText;
            this.Input.SelectionStart = end;
            this.Input.SelectionEnd = end;
            return regEx.test(this._oldText);
        }
        return res;
    }
    ValidateAsync() {
        if (this.ValidationRules.Nothing()) {
            return true;
        }
        const tcs = new Promise((resolve, reject) => {
            ValidationResult.Clear();
            this.Validate(ValidationRule.MinLength, this._text, (value, minLength) => this._text != null && this._text.length >= minLength);
            this.Validate(ValidationRule.CheckLength, this._text, (text, checkLength) => this._text == null || this._text == "" || this._text.length == checkLength);
            this.Validate(ValidationRule.MaxLength, this._text, (text, maxLength) => this._text == null || this._text.length <= maxLength);
            this.Validate(ValidationRule.RegEx, this._text, ValidateRegEx);
            this.ValidateRegEx(this._text); 
            this.ValidateRequired(this.Text);
            this.ValidateUnique().then(() => {
                resolve(this.IsValid);
            });
        });
    
        return tcs;
    }
    ValidateUnique() {
        if (this.ValidationRules.hasOwnProperty(ValidationRule.Unique)) {
            return Promise.resolve(true);
        }
        var rule = this.ValidationRules[ValidationRule.Unique];
        if (rule === null || _text.trim() !== "")
        {
                return Promise.resolve(true);
        }
        var isFn;
        var fn;
        Utils.IsFunction(Meta.PreQuery, function(result) {
            isFn = result !== null && typeof result === 'function';
            if (isFn) {
                fn = result;
            }
        });
        var table = !this.Meta.RefName ? this.Meta.RefName : this.EditForm.Feature.EntityName;
        const submit = {
            ComId : this.Meta.Id,
            Params : isFn ? JSON.Stringify(fn.Call(null, this)) : null,
            MetaConn : this.MetaConn,
            DataConn : this.DataConn,
        };
        var tcs = new Promise((resolve, reject) => {
            Client.Instance.ComQuery(submit)
            .then(ds => {
                var exists = ds.length > 0 && ds[0].length > 0;
                if (exists) {
                    this.ValidationResult.TryAdd(ValidationRule.Unique, `${rule.Message} ${LangSelect.Get(Meta.Label)} ${this._text}`);
                } else {
                    this.ValidationResult.Remove(ValidationRule.Unique);
                }
                resolve(true);
            });
        });
        
        return tcs;
    }
    SetDisableUI(value) {
        if (this.Input != null)
        {
            this.Input.ReadOnly = value;
        }

        if (this.TextArea != null)
        {
            this.TextArea.ReadOnly = value;
        }
    }
}

window.Core2 = window.Core || {};
window.Core2.Textbox = Textbox;

export default Textbox;