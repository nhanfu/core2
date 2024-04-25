import EditableComponent from './editableComponent.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { ValidationRule } from "./models/validationRule.js";
import { LangSelect } from "./utils/langSelect.js";
import { Client } from "./clients/client.js";
import EventType from './models/eventType.js';
import { ComponentType } from './models/componentType.js';
import { string } from './utils/ext.js';


class Textbox extends EditableComponent {
    constructor(meta, ele) {
        super(meta, ele);
        /** @type {HTMLInputElement} */
        this.Input = null;
        /** @type {HTMLTextAreaElement} */
        this.TextArea = null;
        this.MultipleLine = false;
        this.Password = false;
        this._value = null;
        this._oldText = '';
        this._text = '';
        this.DefaultValue = '';
        if (ele?.tagName === ComponentType.Input) {
            this.Input = ele;
        }
        else if (ele.tagName === ComponentType.Textarea) {
            this.TextArea = ele;
        }
    }
    /** @type {String} */
    get Text() { return this._text; }
    set Text(val) { 
        this._text = val; 
        if (this.Input) this.Input.value = val;
        if (this.Textarea) this.Textarea.value = val;
    }

    get Value() { return this._value; }

    set Value(newValue) {
        this._value = newValue;
        if (this._value !== null && typeof this._value === string.Type) {
            if (this.EditForm && this.EditForm.Meta && !this.EditForm.Meta.IgnoreEncode || !this.EditForm.Meta) {
                this.Entity.SetComplexPropValue(this.FieldName, Utils.EncodeSpecialChar(Utils.DecodeSpecialChar(this._value)));
            }
        }
        if (this.Entity) {
            this.Entity.SetComplexPropValue(this.FieldName, this._value);
        }

        let text = (this.EditForm && this.EditForm.Meta && this.EditForm.Meta.IgnoreEncode) ? this._value : Utils.DecodeSpecialChar(this._value);
        if (this.Meta.FormatData && Utils.HasAnyChar(this.Meta.FormatData)) {
            text = Utils.FormatEntity(this.Meta.FormatData, this.Entity.GetPropValue(this.FieldName));
        }

        if (this.Meta.FormatEntity && Utils.HasAnyChar(this.Meta.FormatEntity)) {
            text = Utils.FormatEntity(this.Meta.FormatEntity, null, this.Entity, Utils.EmptyFormat, Utils.EmptyFormat);
        }
        Utils.IsFunction(this.Meta.Renderer)?.call(this, this);
        this.Text = text;
        this.PopulateFields();
    }

    Render() {
        this.SetDefaultVal();
        var val = this.Entity && this.Entity.GetComplexProp(this.FieldName);
        var shouldEncode = val !== null && val !== undefined && typeof val === string.Type && this.EditForm != null 
            && this.EditForm.Meta != null && !this.EditForm.Meta.IgnoreEncode;
        if (shouldEncode) {
            const decode = Utils.DecodeSpecialChar(val);
            const encode = Utils.EncodeSpecialChar(decode);
            this.Entity.SetComplexPropValue(this.FieldName, encode);
        }
        var text = val || '';
        if (!this.Meta.FormatData) {
            text = Utils.FormatEntity(this.Meta.FormatData, val);
        } else if (!this.Meta.FormatEntity) {
            text = Utils.FormatEntity(this.Meta.FormatEntity, this.Entity);
        }
        this._text = this.EditForm != null && this.EditForm.Meta != null && this.EditForm.Meta.IgnoreEncode ? text : Utils.DecodeSpecialChar(text);
        this.OldValue = this._text;
        if (this.MultipleLine || this.TextArea != null) {
            if (this.TextArea == null) {
                Html.Take(this.ParentElement).TextArea.Value(this._text).PlaceHolder(this.Meta.PlainText);
                this.Element = this.TextArea = Html.Context;
            } else if (this.TextArea) {
                this.Element = this.TextArea;
                this.TextArea.value = this._text;
            }
            if (this.Meta.Row > 0) {
                Html.Instance.Attr("rows", this.Meta.Row ?? 1);
            }
            this.TextArea.addEventListener("input", (e) => this.PopulateUIChange(EventType.Input).bind(this));
            this.TextArea.addEventListener("change", (e) => this.PopulateUIChange(EventType.Change).bind(this));
        }
        else {
            if (this.Input == null) {
                Html.Take(this.ParentElement).Input.Value(this._text)?.PlaceHolder(this.Meta.PlainText);
                this.Element = this.Input = Html.Context;
            } else {
                this.Element = this.Input;
                this.Input.value = this._text;
            }
            this.Input.addEventListener("input", (e) => this.PopulateUIChange(EventType.Input).bind(this));
            this.Input.addEventListener("change", (e) => this.PopulateUIChange(EventType.Change).bind(this));
        }
        Utils.IsFunction(this.Meta.Renderer)?.call(this, this);
        if (this.Password) {
            Html.Instance.Style("text-security: disc;-webkit-text-security: disc;-moz-text-security: disc;");
        }
        if (!this.Meta.ShowLabel) {
            Html.Instance.PlaceHolder(this.Meta.PlainText);
        }
        if (this.Element && this.Element.closest("td")) {
            this.Element.closest("td").addEventListener("keydown", this.ListViewItemTab.bind(this));
        }
        this.DOMContentLoaded?.Invoke();
    }

    PopulateUIChange(type, shouldTrim = false) {
        if (this.Disabled) {
            return;
        }
        this._oldText = this._text;
        this._text = this.Input ? this.Input.Value : this.TextArea.Value;
        this._text = this.Password ? this._text : (shouldTrim ? this._text?.trim() : this._text);
        if (this.Meta.UpperCase && this._text != null) {
            this.Text = this._text.toLocaleUpperCase();
        }
        this._value = (this.EditForm != null && this.EditForm.Meta != null && this.EditForm.Meta.IgnoreEncode) ? this._text : EncodeSpecialChar(this._text);
        this.Entity && SetComplexPropValue(this.Entity, this.FieldName, this._value);
        this.Dirty = true;
        this.UserInput?.Invoke({ NewData: this._text, OldData: this._oldText, EvType: type });
        this.PopulateFields();
        this.DispatchEvent(this.Meta.Events, type, this.Entity);

    }
    UpdateView(force = false, dirty = null, ...componentNames) {
        this.Value = this.Entity && Utils.GetPropValue(this.Entity, this.FieldName);
        if (!this.Dirty) {
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
            this.ValidationResult.Clear();
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
        if (rule === null || _text.trim() !== "") {
            return Promise.resolve(true);
        }
        var isFn;
        var fn;
        Utils.IsFunction(Meta.PreQuery, function (result) {
            isFn = result !== null && typeof result === 'function';
            if (isFn) {
                fn = result;
            }
        });
        var table = !this.Meta.RefName ? this.Meta.RefName : this.EditForm.Meta.EntityName;
        const submit = {
            ComId: this.Meta.Id,
            Params: isFn ? JSON.Stringify(fn.Call(null, this)) : null,
            MetaConn: this.MetaConn,
            DataConn: this.DataConn,
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
        if (this.Input != null) {
            this.Input.ReadOnly = value;
        }

        if (this.TextArea != null) {
            this.TextArea.ReadOnly = value;
        }
    }
}

window.Core2 = window.Core || {};
window.Core2.Textbox = Textbox;

export default Textbox;