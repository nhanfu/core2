import { EditableComponent } from './editableComponent.js';
import { Component, EventType, ValidationRule, keyCodeEnum } from "./models/";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { LangSelect } from "./utils/langSelect.js";
import { Client } from "./clients/client.js";
import { Str } from './utils/ext.js';
import { searchMethodEnum } from './models/enum.js';


export class Textbox extends EditableComponent {
    /**
     * @param {Component} ui
     * @param {HTMLElement} ele
     */
    constructor(ui, ele) {
        super(ui, ele);
        this.defaultValue = "";
        if (ele && ele.tagName == "INPUT") {
            this.Input = ele;
        } else if (ele && ele.tagName == "TEXTAREA") {
            this.textArea = ele;
        }
        this._value = null;
        this.Password = this.meta.className && this.meta.className.toLowerCase().includes("password");
        this._text = "";
        this._oldText = "";
        this.searchMethod = searchMethodEnum.contain;
        this.searchIcon = "fas fa-check";
        this.isInput = true;
        /**
         * @type {HTMLElement}
         */
        this.searchIconElement = null;
    }
    /** @type {String} */
    get Text() {
        return this._text;
    }

    set Text(value) {
        this._text = value;
        if (this.Input != null) {
            this.Input.value = this._text;
        }

        if (this.textArea != null) {
            this.textArea.value = this._text;
        }
    }

    get Value() { return this._value; }

    set Value(newValue) {
        if (newValue == undefined) {
            newValue = null;
        }
        this._value = newValue;
        if (this._value !== null && typeof this._value === Str.Type) {
            if (this.editForm && this.editForm.meta && !this.editForm.meta.ignoreEncode) {
                this.entity[this.Name] = this._value;
            }
        }
        if (this.entity) {
            this.entity[this.Name] = this._value;
        }
        let text = this._value;
        if (this.meta.formatData) {
            text = Utils.formatEntity(this.meta.formatData, this.entity[this.Name]);
        }

        if (this.meta.formatEntity) {
            text = Utils.formatEntity2(this.meta.formatEntity, null, this.entity, Utils.emptyFormat, Utils.emptyFormat);
        }
        this.Text = text;
        this.populateFields();
    }

    render() {
        this.setDefaultVal();
        var val = this.entity && this.entity[this.Name] || null;
        var text = val;
        if (this.meta.formatData) {
            text = Utils.formatEntity(this.meta.formatData, val);
        }
        if (this.meta.formatEntity) {
            text = Utils.formatEntity(this.meta.formatEntity, this.entity);
        }
        this._text = text || "";
        this.oldValue = this._text;
        this._value = this._text;
        if (this.componentType == "Textarea" || this.textArea != null) {
            if (this.textArea == null) {
                Html.take(this.parentElement).textArea.value(this._text).placeHolder(this.meta.plainText);
                // @ts-ignore
                this.element = this.textArea = Html.context;
            } else if (this.textArea) {
                Html.take(this.element);
                this.element = this.textArea;
                this.textArea.value = this._text;
            }
            if (this.meta.row > 0) {
                Html.instance.attr("rows", this.meta.row ?? 1);
            }
            this.textArea.addEventListener("input", (e) => this.populateUIChange(EventType.Input));
            this.textArea.addEventListener("change", (e) => this.populateUIChange(EventType.Change));
        }
        else {
            if (this.Input == null) {
                Html.take(this.parentElement).input.value(this._text)?.placeHolder(this.meta.plainText);
                // @ts-ignore
                this.element = this.Input = Html.context;
            } else {
                Html.take(this.element);
                this.element = this.Input;
                this.Input.value = this._text;
            }
            this.Input.addEventListener('keydown', this.keydownHandler.bind(this));
            this.Input.addEventListener("input", (e) => this.populateUIChange(EventType.Input));
            this.Input.addEventListener("change", (e) => this.populateUIChange(EventType.Change));
        }
        if (this.Password) {
            Html.instance.style("text-security: disc;-webkit-text-security: disc;-moz-text-security: disc;");
        }
        if (!this.meta.showLabel) {
            Html.instance.placeHolder(this.meta.plainText);
        }
        if (this.element && this.element.closest("td")) {
            this.element.closest("td").addEventListener("keydown", this.listViewItemTab.bind(this));
        }
        this.Validate(ValidationRule.regEx, this._text, this.validateRegEx);
        this.Validate(ValidationRule.Replace, this._text, this.validateReplace);
        this.dOMContentLoaded?.invoke();
    }

    populateUIChange(type, shouldTrim = false) {
        if (this.disabled) {
            return;
        }
        this._oldText = this._text;
        this._text = this.Input ? this.Input.value : this.textArea.value;
        this._text = this.Password ? this._text : (shouldTrim ? this._text?.trim() : this._text);
        if (this.meta.upperCase && this._text != null) {
            this.Text = this._text.toLocaleUpperCase();
        }
        this._value = this._text;
        this.entity[this.Name] = this._value;
        this.Dirty = true;
        this.userInput?.invoke({ newData: this._text, oldData: this._oldText, evType: type });
        this.populateFields();
        if (type == EventType.Input) {
            this.Validate(ValidationRule.Replace, this._text, this.validateReplace);
        }
        if (type == EventType.Change) {
            this.Validate(ValidationRule.regEx, this._text, this.validateRegEx);
        }
        this.dispatchEvent(this.meta.events, type, this, this.entity).then();
    }
    updateView(force = false, dirty = null, ...componentNames) {
        var newValue = this.entity[this.meta.fieldName];
        if (newValue != this._value) {
            this.value = newValue;
            this.setRequired();
        }
        if (!this.Dirty) {
            this.originalText = this._text;
            this.dOMContentLoaded?.invoke();
            this.oldValue = this._text;
        }
    }

    validateAsync() {
        if (this.validationRules.length == 0) {
            return Promise.resolve(true);
        }
        const tcs = new Promise((resolve, reject) => {
            this.validationResult = [];
            this.Validate(ValidationRule.minLength, this._text, (value, minLength) => this._text != null && this._text.length >= minLength);
            this.Validate(ValidationRule.checkLength, this._text, (text, checkLength) => this._text == null || this._text == "" || this._text.length == checkLength);
            this.Validate(ValidationRule.maxLength, this._text, (text, maxLength) => this._text == null || this._text.length <= maxLength);
            this.validateRequired(this.Text);
            this.validateUnique().then(() => {
                resolve(this.isValid);
            });
        });

        return tcs;
    }

    /**
     * @param {string} value
     * @param {string | regExp} regText
     */
    validateRegEx(value, regText) {
        if (!this.validationRules.hasOwnProperty(ValidationRule.regEx)) {
            return Promise.resolve(true);
        }
        if (value === null) {
            return true;
        }
        var regEx = new regExp(regText);
        var res = regEx.test(value);
        var rule = this.validationRules[ValidationRule.regEx];
        if (rule && !res && rule.rejectInvalid) {
            var end = this.Input.selectionEnd;
            this.Text = this._oldText;
            this._value = this._oldText;
            this.Input.selectionStart = end;
            this.Input.selectionEnd = end;
            var rs1 = regEx.test(this._oldText);
            if (rs1) {
                this.element.classList.add("reg-text");
            }
            return rs1;
        }
        if (!res) {
            this.element.classList.add("reg-text");
        }
        else {
            this.element.classList.remove("reg-text");
        }
        return res;
    }

    /**
     * @param {string} value
     * @param {string } regText
     * @param {string} format
     */
    validateReplace(value, regText, format) {
        if (!this.validationRules.hasOwnProperty(ValidationRule.Replace)) {
            return Promise.resolve(true);
        }
        if (value === null) {
            return true;
        }
        let input = value;
        input = input.replace(/[^a-zA-Z0-9]/g, '');
        let formattedInput = '';
        let formatIndex = 0;
        let inputIndex = 0;
        while (formatIndex < format.length && inputIndex < input.length) {
            if (format[formatIndex] === '#') {
                formattedInput += input[inputIndex];
                inputIndex++;
            } else {
                formattedInput += format[formatIndex];
            }
            formatIndex++;
        }
        const isValid = formattedInput.length == format.length;
        this.element.value = formattedInput;
        this._value = formattedInput;
        this.entity[this.Name] = formattedInput;
        if (isValid) {
            this.element.classList.remove("reg-text");
        } else {
            this.element.classList.add("reg-text");
        }
        return isValid;
    }

    validateUnique() {
        if (!this.validationRules.hasOwnProperty(ValidationRule.Unique)) {
            return Promise.resolve(true);
        }
        var rule = this.validationRules[ValidationRule.Unique];

        if (rule === null || this._text.trim() === "") {
            return Promise.resolve(true);
        }
        if (!this.validationResult) {
            this.validationResult = {};
        }
        const params = Utils.isFunction(this.meta.preQuery, false, this);
        var table = !this.meta.refName ? this.meta.refName : this.editForm.meta.entityName;
        const submit = {
            comId: this.meta.id,
            params: params,
            metaConn: this.metaConn,
            dataConn: this.dataConn,
        };
        var tcs = new Promise((resolve, reject) => {
            Client.instance.comQuery(submit)
                .then(ds => {
                    var exists = ds.length > 0 && ds[0].length > 0;
                    return exists;
                })
                .then(exists => {
                    if (exists) {
                        this.validationResult[ValidationRule.Unique] = `${rule.Message} ${LangSelect.get(this.meta.label)} ${this._text}`;
                    } else {
                        delete this.validationResult[ValidationRule.Unique];
                    }
                    resolve(true);
                })
                .catch(error => {
                    console.error('Query error:', error);
                    reject(error);
                });
        });

        return tcs;
    }

    setDisableUI(value) {
        if (this.Input != null) {
            this.Input.readOnly = value;
        }

        if (this.textArea != null) {
            this.textArea.readOnly = value;
        }
    }
}
