import EditableComponent from './editableComponent.js';
import EventType from './models/eventType.js';
import { Utils } from './utils/utils.js';
import { ValidationRule } from './models/validationRule.js';
import { Html } from './utils/html.js';

export class NumberBox extends EditableComponent {
    constructor(ui, ele = null) {
        super(ui, ele);
        /** @type {HTMLInputElement} */
        this._input = ele instanceof HTMLInputElement ? ele : null;
        this._value = null;
        this._nullable = false;
        this._isString = false;
        this._decimalSeparator = '.';
        this.SetSelection = true;
        this.DefaultValue = 0;
    }

    get Value() {
        return this._value;
    }

    set Value(value) {
        const oldValue = this._value;
        this._value = value;
        if (this._value !== null) {
            this._value = Number(this._value.toFixed(this.Meta.Precision || 0));
            const dotCount = (this._input.value.match(/,/g) || []).length;
            const selectionEnd = this._input.selectionEnd;
            this._input.value = this.EmptyRow ? '' : this._value.toLocaleString(undefined, { minimumFractionDigits: this.Meta.Precision || 0 });
            const addedDot = (this._input.value.match(/,/g) || []).length - dotCount;
            if (this.SetSelection) {
                this._input.selectionStart = selectionEnd + addedDot;
                this._input.selectionEnd = selectionEnd + addedDot;
            }
        } else if (!this._nullable) {
            this._value = 0;
            this._input.value = this._value.toString();
        } else {
            this._input.value = '';
        }
        if (oldValue !== this._value) {
            this.Dirty = true;
        }
        this.Entity.SetComplexPropValue(this.FieldName, this._value);
        this.PopulateFields();
        var customizeFn = Utils.IsFunction(this.Meta.Renderer);
        if (customizeFn) {
            customizeFn.call(this, this);
        }
    }

    // Rest of the methods from previous message...

    SetValue() {
        const oldVal = this._value;
        this.EmptyRow = false;
        if (this._input.value.IsNullOrWhiteSpace()) {
            this.Value = null;
            return;
        }
        this._input.value = this._input.value.trim();
        if (this._input.value.slice(-1) === this._decimalSeparator) {
            this._input.value = this._input.value.substring(0, this._input.value.length - 1);
        }

        const text = this._input.value.replace(/,/g, "");
        const [success, parsedResult] = Utils.TryParseDecimal(text);
        if (!success) {
            this.Value = this._value; // Set old value to avoid accepting an invalid value
            return;
        }
        this._value = parsedResult;
        this.Value = this._value;
        this.UserInput?.invoke({ NewData: this._value, OldData: oldVal, EvType: EventType.Input });
        this.DispatchEvent(this.Meta.Events, EventType.Input, this.Entity, this._value, oldVal).done();
    }

    Render() {
        this.SetDefaultVal();
        if (this.Entity != null) {
            const fieldVal = Utils.GetPropValue(this.Entity, this.FieldName);
            this._isString = typeof fieldVal === 'string';
            this._nullable = this.IsNullable();
            this._value = this.GetDecimalValue();
            this.Entity.SetComplexPropValue(this.FieldName, this._value);
        }
        if (this._input === null) {
            Html.Take(this.ParentElement).Input.Render();
            this.Element = this._input = Html.Context;
        } else {
            this.Element = this._input;
        }
        this._input.type = 'tel';
        this._input.setAttribute('autocorrect', 'off');
        this._input.setAttribute('spellcheck', 'false');
        this._input.addEventListener('input', this.SetValue.bind(this));
        this._input.addEventListener('change', this.ChangeSetValue.bind(this));
        this._input.autocomplete = 'off';
        this.Value = this._value; // set again to render in correct format
        let fn = Utils.IsFunction(this.Meta.Renderer);
        if (!this.Meta.ChildStyle?.IsNullOrWhiteSpace() && fn) {
            window.setTimeout(() => fn.call(this, this, this._input), 100);
        }
        this.DOMContentLoaded?.invoke();
    }

    IsNullable() {
        const val = this.Entity.GetComplexProp(this.FieldName);
        return val === null || val === undefined;
    }

    ChangeSetValue() {
        const oldVal = this._value;
        this.EmptyRow = false;
        if (this._input.value.IsNullOrWhiteSpace()) {
            this.Value = null;
            this.UserInput?.Invoke({ NewData: null, OldData: oldVal, EvType: EventType.Change });
            return;
        }
        this._input.value = this._input.value.trim();
        if (this._input.value.slice(-1) === '.') {
            this._input.value = this._input.value.substring(0, this._input.value.length - 1);
        }

        const text = this._input.value.replace(",", "");
        const [success, parsedResult] = Utils.TryParseDecimal(text);
        if (!success) {
            this.Value = this._value; // Set old value to avoid accept invalid value
            return;
        }
        this.Value = parsedResult;
        this.UserInput?.Invoke({ NewData: parsedResult, OldData: oldVal, EvType: EventType.Change });
        this.PopulateFields();
        this.DispatchEvent(this.Meta.Events, EventType.Change, this.Entity, parsedResult, oldVal).done();
    }

    SetValue() {
        const oldVal = this._value;
        this.EmptyRow = false;
        if (this._input.value.IsNullOrWhiteSpace()) {
            this.Value = null;
            return;
        }
        this._input.value = this._input.value.trim();
        if (this._input.value.slice(-1) === '.') {
            this._input.value = this._input.value.substring(0, this._input.value.length - 1);
        }

        const text = this._input.value.replace(",", "");
        const [success, parsedResult] = Utils.TryParseDecimal(text);
        if (!success) {
            this.Value = this._value; // Set old value to avoid accept invalid value
            return;
        }
        this._value = parsedResult;
        this.Value = this._value;
        this.UserInput?.Invoke({ NewData: this._value, OldData: oldVal, EvType: EventType.Input });
        this.DispatchEvent(this.Meta.Events, EventType.Input, this.Entity, this._value, oldVal).done();
    }

    GetDecimalValue() {
        if (this.Entity == null) {
            return null;
        }

        const value = Utils.GetPropValue(this.Entity, this.FieldName);
        if (value == null) {
            return null;
        }

        if (this._isString && value.toString().IsNullOrWhiteSpace()) {
            return null;
        }

        try {
            return NumberBox(value);
        } catch (e) {
            return null;
        }
    }

    UpdateView(force = false, dirty = null, ...componentNames) {
        this.Value = this.GetDecimalValue();
        if (!this.Dirty) {
            this.DOMContentLoaded?.invoke();
            this.OldValue = this._input.value;
        }
    }

    async ValidateAsync() {
        if (this.ValidationRules.Nothing()) {
            return true;
        }
        this.ValidationResult.clear();
        this.ValidateRequired(this._value);
        this.Validate(ValidationRule.GreaterThan, this._value, (value, ruleValue) => ruleValue == null || value != null && value > ruleValue);
        this.Validate(ValidationRule.LessThan, this._value, (value, ruleValue) => ruleValue == null || value != null && value < ruleValue);
        this.Validate(ValidationRule.GreaterThanOrEqual, this._value, (value, ruleValue) => ruleValue == null || value != null && value >= ruleValue);
        this.Validate(ValidationRule.LessThanOrEqual, this._value, (value, ruleValue) => ruleValue == null || value != null && value <= ruleValue);
        this.Validate(ValidationRule.Equal, this._value, (value, ruleValue) => value === ruleValue);
        this.Validate(ValidationRule.NotEqual, this._value, (value, ruleValue) => value !== ruleValue);
        return this.IsValid;
    }

    SetDisableUI(value) {
        this._input.readOnly = value;
    }
}
