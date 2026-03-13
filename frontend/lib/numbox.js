import { EditableComponent } from './editableComponent.js';
import EventType from './models/eventType.js';
import { Utils } from './utils/utils.js';
import { ValidationRule } from './models/validationRule.js';
import { Html } from './utils/html.js';
import { Component } from './models/component.js';
import Decimal from 'decimal.js';
import { LangSelect } from './utils/langSelect.js';
import { keyCodeEnum } from './models/enum.js';

export class Numbox extends EditableComponent {
    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {hTMLInputElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui, ele);
        /** @type {hTMLInputElement} */
        if (ele && ele.tagName == "INPUT") {
            this._input = ele;
        }
        /** @type {Decimal} */
        this._value = null;
        this._isString = false;
        this._decimalSeparator = '.';
        this.setSelection = true;
        this.defaultValue = 0;
        this.meta.Precision = this.meta.groupTypeId ? parseInt(LangSelect._webConfig[this.meta.groupTypeId]) : parseInt(this.meta.Precision || 0);
    }

    /** @type {Decimal} */
    get Value() {
        return this._value;
    }

    set Value(value) {
        const oldValue = this._value;
        this._value = value;
        if (value === null || value == undefined) {
            this._input.value = '';
            this._value = null;
        }
        else {
            var [success, parsedVal] = Utils.tryParseDecimal(this._value?.toString());
            if (success) {
                this._value = parsedVal;
                var precision = parseInt(this.meta.Precision ?? 0);
                const dotCount = (this._input.value?.match(/,/g) || []).length;
                const selectionEnd = this._input.selectionEnd;
                var hasDot = false;
                if (this._input.value.split('').filter(x => x === '.').length > 1) {
                    hasDot = true;
                }
                var fixedValue = this._value.toFixed(precision);
                var parts = fixedValue.split('.');
                parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ',');
                var text = parts.join('.');
                this._input.value = text;
                const addedDot = (this._input.value?.match(/,/g) || []).length - dotCount;
                if (hasDot) {
                    var dotIndex = this._input.value.lastIndexOf('.');
                    this._input.selectionStart = dotIndex + 1;
                    this._input.selectionEnd = this._input.selectionStart + 1;
                }
                else {
                    this._input.selectionStart = selectionEnd + addedDot;
                    this._input.selectionEnd = selectionEnd + addedDot;
                }
                if (parsedVal.isNegative()) {
                    this._input.classList.add("negative");
                }
                else {
                    this._input.classList.remove("negative");
                }
            }
            else {
                var [success, parsedVal] = Utils.tryParseDecimal(oldValue?.toString());
                if (!success) {
                    this.parsedVal = new Decimal(0);
                }
                this._value = new Decimal(parsedVal);
                this._input.value = parsedVal.toFixed(parseInt(this.meta.Precision || 0));
                if (parsedVal.isNegative()) {
                    this._input.classList.add("negative");
                }
                else {
                    this._input.classList.remove("negative");
                }
            }
        }
        this.entity[this.Name] = this._value;
        Utils.isFunction(this.meta.Renderer, false, this);
    }

    setValue() {
        const oldVal = this._value;
        this.emptyRow = false;
        if (this._input.value == "-") {
            return;
        }
        if (this._input.value.startsWith("=")) {
            return;
        }
        if (Utils.isNullOrWhiteSpace(this._input.value)) {
            this.Value = null;
            this.dispatchEvent(this.meta.events, EventType.Input, this, this.entity, this._value, oldVal).then();
            return;
        }
        this._input.value = this._input.value.trim();
        if (this._input.value.slice(-1) === this._decimalSeparator) {
            this._input.value = this._input.value.substring(0, this._input.value.length - 1);
        }
        const text = this._input.value.replace(/,/g, "");
        const [success, parsedResult] = Utils.tryParseDecimal(text);
        if (!success) {
            this.Value = this._value;
            this.dispatchEvent(this.meta.events, EventType.Input, this, this.entity, this._value, oldVal).then();
            return;
        }
        this.Value = parsedResult;
        this.userInput?.invoke({ newData: this._value, oldData: oldVal, evType: EventType.Input });
        this.dispatchEvent(this.meta.events, EventType.Input, this, this.entity, this._value, oldVal).then();
    }

    render() {
        this.setDefaultVal();
        if (this.entity != null) {
            const fieldVal = this.entity[this.Name];
            if (fieldVal != null) {
                this._value = this.getDecimalValue();
            }
        }
        if (!this._input || this._input === null) {
            Html.take(this.parentElement).input.render();
            const inputElement = Html.context;
            if (inputElement instanceof hTMLInputElement) {
                this.element = this._input = inputElement;
            }
        } else {
            this.element = this._input;
        }
        this._input.type = 'tel';
        this._input.setAttribute('autocorrect', 'off');
        this._input.setAttribute('spellcheck', 'false');
        this._input.addEventListener('input', this.setValue.bind(this));
        this._input.addEventListener('keydown', this.keydownHandler.bind(this));
        this._input.addEventListener('change', this.changeSetValue.bind(this));
        this._input.autocomplete = 'off';
        this.oldValue = this._value;
        this.Value = this._value;
        window.setTimeout(() => Utils.isFunction(this.meta.Renderer), 100);
        this.dOMContentLoaded?.invoke();
    }

    keydownHandler(e) {
        let code = e.keyCodeEnum();
        switch (code) {
            case keyCodeEnum.enter:
                e.preventDefault();
                if (this._input.value.startsWith("=")) {
                    this._input.value = Utils.isFunction("return " + this._input.value.substring(1), false, this);
                    this.setValue();
                }
                else {
                    if (!this.parent.isListViewItem) {
                        if (!Utils.isNullOrWhiteSpace(this.meta.groupBy)) {
                            var groups = this.editForm.childCom.filter(x => x.meta.groupBy == this.meta.groupBy);
                            var index = groups.indexOf(this);
                            if (groups[index + 1]) {
                                groups[index + 1].Focus();
                            }
                            else {
                                var groupIndex = this.parent.Parent.Children.indexOf(this.parent);
                                if (this.parent.Parent.Children[groupIndex + 1] && this.parent.Parent.Children[groupIndex + 1].Children[0]) {
                                    this.parent.Parent.Children[groupIndex + 1].Children[0].Focus();
                                }
                            }
                        }
                        else {
                            var index = this.parent.children.indexOf(this);
                            if (this.parent.children[index + 1]) {
                                this.parent.children[index + 1].Focus();
                            }
                            else {
                                var groupIndex = this.parent.Parent.Children.indexOf(this.parent);
                                if (this.parent.Parent.Children[groupIndex + 1] && this.parent.Parent.Children[groupIndex + 1].Children[0]) {
                                    this.parent.Parent.Children[groupIndex + 1].Children[0].Focus();
                                }
                            }
                        }
                    }
                }
                break;
            default:
                break;
        }
    }

    isNullable() {
        const val = this.entity.getComplexProp(this.Name);
        return val === null || val === undefined;
    }

    changeSetValue() {
        const oldVal = this._value;
        this.emptyRow = false;
        if (Utils.isNullOrWhiteSpace(this._input.value)) {
            this.Value = null;
            this.Dirty = true;
            this.populateFields();
            this.dispatchEvent(this.meta.events, EventType.Change, this, this.entity).then(() => {
                this.userInput?.invoke({ newData: this._value, oldData: oldVal, evType: EventType.Change });
            });
            return;
        }
        this._input.value = this._input.value.trim();
        if (this._input.value.slice(-1) === '.') {
            this._input.value = this._input.value.substring(0, this._input.value.length - 1);
        }

        const text = this._input.value.replace(",", "");
        const [success, parsedResult] = Utils.tryParseDecimal(text);
        if (!success) {
            this.Dirty = true;
            this.Value = this._value; // Set old value to avoid accept invalid value
            this.dispatchEvent(this.meta.events, EventType.Change, this, this.entity).then(() => {
                this.userInput?.invoke({ newData: this._value, oldData: oldVal, evType: EventType.Change });
            });
            return;
        }
        this.Value = new Decimal(parsedResult);
        this.Dirty = true;
        this.populateFields();
        this.dispatchEvent(this.meta.events, EventType.Change, this, this.entity).then(() => {
            this.userInput?.invoke({ newData: this._value, oldData: oldVal, evType: EventType.Change });
        });
    }

    getDecimalValue() {
        if (this.entity == null) {
            return null;
        }
        const value = this.entity[this.meta.fieldName];
        if (value == null) {
            return null;
        }
        try {
            return new Decimal(value);
        } catch (e) {
            return null;
        }
    }

    updateView(force = false, dirty = null, ...componentNames) {
        var newValue = this.getDecimalValue();
        if ((this._value && newValue &&
            !newValue.equals(this._value))
            || (this._value && !newValue)
            || (!this._value && newValue)) {
            this.Value = newValue;
            this.setRequired();
            if (!this.Dirty) {
                this.originalText = this._input;
                this.dOMContentLoaded?.invoke();
                this.oldValue = this._input;
            }
        }
        else {
            if (newValue) {
                this._value = newValue;
                this.entity[this.meta.fieldName] = newValue;
            }
        }
    }

    async validateAsync() {
        if (this.validationRules.length == 0) {
            return true;
        }
        this.validationResult = [];
        this.validateRequired(this._value);
        this.Validate(ValidationRule.greaterThan, this._value, (value, ruleValue) => ruleValue == null || value != null && value > ruleValue);
        this.Validate(ValidationRule.lessThan, this._value, (value, ruleValue) => ruleValue == null || value != null && value < ruleValue);
        this.Validate(ValidationRule.greaterThanOrEqual, this._value, (value, ruleValue) => ruleValue == null || value != null && value >= ruleValue);
        this.Validate(ValidationRule.lessThanOrEqual, this._value, (value, ruleValue) => ruleValue == null || value != null && value <= ruleValue);
        this.Validate(ValidationRule.Equal, this._value, (value, ruleValue) => value === ruleValue);
        this.Validate(ValidationRule.notEqual, this._value, (value, ruleValue) => value !== ruleValue);
        return this.isValid;
    }

    setDisableUI(value) {
        this._input.readOnly = value;
    }
}

export { Numbox as numBox };
