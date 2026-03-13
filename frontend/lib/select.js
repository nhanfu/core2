import { EditableComponent } from './editableComponent.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { positionEnum, keyCodeEnum, ObservableList, Component, EventType, ValidationRule } from "./models/index.js";
import { LangSelect } from "./utils/langSelect.js";
import { ComponentExt } from './utils/componentExt.js';
import { GridView } from './gridView.js';
import { Client } from './clients/client.js';
import slimSelect from 'slim-select';

export class Select extends EditableComponent {
    isSearchEntry = true;
    isMultiple = false;
    /**
     * @type {slimSelect}
     */
    SS;
    Data;
    /**
     * Create instance of component
     * @param {Component | null} ui 
     * @param {HTMLElement | null} ele 
     */
    constructor(ui, ele = null) {
        super(ui);
        this.defaultValue = '';
        this.sEntryClass = "search-entry"
        this.meta.componentGroup = null;
        this.meta.row = this.meta.row ?? 50;
        this.rowData = new ObservableList();
        /** @type {hTMLDivElement} */
        this._input = null;
        /** @type {HTMLElement} */
        this._rootResult = null;
        /** @type {HTMLElement} */
        this._parentInput = null;
        /** @type {HTMLElement} */
        this._backdrop = null;
        this._waitForInput = null;
        this._waitForDispose = null;
        this._contextMenu = false;
        this.searchResultEle = null;
        this._gv = null;
        let containId = Utils.isNullOrWhiteSpace(this.meta.tabGroup) ? this.meta.fieldName.substr(this.meta.fieldName.length - 2) === this.idField : this.meta.tabGroup.substr(this.meta.tabGroup.length - 2) === this.idField;
        if (containId) {
            this.displayField = Utils.isNullOrWhiteSpace(this.meta.tabGroup) ? this.meta.fieldName.substr(0, this.meta.fieldName.length - 2) : this.meta.tabGroup.substr(0, this.meta.tabGroup.length - 2);
        }
        else {
            this.displayField = this.meta.fieldName + "masterData";
        }
        if (this.meta.fieldName == "currencyId") {
            this.isCurrency = true;
        }
    }

    render() {
        this.setDefaultVal();
        this._value = this.entity[this.Name] == null ? null : this.entity[this.Name].toString();
        this.renderInputAndEvents();
        if (this.meta.showHotKey) {
            this.renderIcons();
        }
        this.Data = Utils.isFunction(this.meta.Query, false, this);
        this.Data.unshift({
            Id: null,
            Name: 'Selected Option',
            Description: 'Selected Option'
        });
        this.SS = new slimSelect({
            select: this.element.firstElementChild,
            data: this.Data.map(x => ({ text: x.Name, value: x.id == null ? null : x.id.toString(), html: x.Description || x.Name })),
            settings: {
                disabled: this.meta.Disabled,
                showSearch: this.Data.length >= 5
            },
            events: {
                afterChange: (newVal) => {
                    var mapEntity = this.Data.find(x => {
                        const xId = x.id != null ? x.id.toString() : null;
                        const entityValue = newVal[0].value != null ? newVal[0].value.toString() : null;
                        return xId === entityValue;
                    });
                    if (mapEntity && (mapEntity.id == null ? null : mapEntity.id.toString()) != (this.entity[this.Name] == null ? null : this.entity[this.Name].toString())) {
                        this.entrySelected(mapEntity);
                    }
                }
            }
        });
        if (this.entity[this.Name] != null) {
            this.SS.setSelected(this.entity[this.Name] == null ? null : this.entity[this.Name].toString());
        }
        else {
            this.SS.setSelected(null);
        }
        this.findMatchText();
    }

    renderInputAndEvents() {
        if (this.element == null) {
            this._input = Html.take(this.parentElement).textAlign("left").div.position(positionEnum.relative).tabIndex(-1).className(this.sEntryClass).select.tabIndex(-1).getContext();
            this._parentInput = this._input.parentElement;
            this.element = this._input.parentElement;
        }
        else {
            this._input = this.element.firstElementChild;
        }
        if (this.parent.isListViewItem) {
            Html.take(this.element.parentElement).event(EventType.keyDown, (e) => this.sEKeydownHandler(e));
        }
        else {
            Html.take(this.element).event(EventType.keyDown, (e) => this.sEKeydownHandler(e));
        }
    }

    sEKeydownHandler(e) {
        if (this.disabled || e === null) {
            return;
        }
        let code = e.keyCodeEnum();
        switch (code) {
            case keyCodeEnum.enter:
                this.SS.open();
                break;
            default:
                break;
        }
    }

    Dispose() {
        super.dispose();
    }

    findMatchText() {
        if (this.entity[this.meta.fieldName] != null && this.entity[this.meta.fieldName] !== undefined) {
            this.Matched = this.Data.find(x => {
                const xId = x.id != null ? x.id.toString() : null;
                const entityValue = this.entity?.[this.meta.fieldName] != null ? this.entity[this.meta.fieldName].toString() : null;
                return xId === entityValue;
            });
            this.entity[this.displayField] = this.Matched;
        }
        else {
            this.entity[this.meta.fieldName] = null;
            this.entity[this.displayField] = null;
        }
        this.updateValue();
    }

    /**
     * Gets the value text from the button component.
     * @returns {string} The text value of the component.
     */
    getValueText() {
        const selected = this.SS.getSelected()[0];
        if (selected) {
            this.Matched = this.Data.find(x => {
                const xId = x.id != null ? x.id.toString() : null;
                const entityValue = selected != null ? selected.toString() : null;
                return xId === entityValue;
            });
            return this.meta.formatData ? Utils.formatEntity(this.meta.formatData, this.Matched) : this.Matched[this.idField];
        }
        else {
            this.Matched = null;
            return '';
        }
    }

    updateValue() {
        if (!this.Dirty) {
            this.dOMContentLoaded?.invoke();
        }
        if (!this.Dirty && !Utils.isNullOrWhiteSpace(this.meta.formatData) && this.entity[this.displayField]) {
            let res = Utils.formatEntity(this.meta.formatData, this.entity[this.displayField]);
            this.originalText = res;
            this.oldValue = this.entity[this.meta.fieldName];
        }

    }

    entrySelected(rowData) {
        this.emptyRow = false;
        if (rowData === null || this.disabled) {
            return;
        }
        this.Dirty = true;
        let oldMatch = this.Matched;
        if (rowData.id) {
            this.Matched = rowData;
            this.entity[this.displayField] = this.Matched;
        }
        else {
            this.Matched = null;
            this.entity[this.displayField] = null;
        }
        let oldValue = this._value;
        this._value = rowData.id;
        this.entity[this.Name] = this._value;
        this.Matched = rowData;
        if (this._gv !== null) {
            this._gv.Show = false;
        }
        this.populateFields(this.Matched);
        this.dispatchEvent(this.meta.events, EventType.Change, this, this.entity, rowData, oldMatch).then(() => {
            this.userInput?.invoke({ newData: this._value, oldData: oldValue, evType: EventType.Change });
        });
        window.setTimeout(() => {
            if (this.parent.isListViewItem) {
                this.element.parentElement.focus()
            }
            else {
                this.element.focus()
            }
        }, 100);
    }

    updateView(force = false, dirty = null, ...componentNames) {
        this.Data = Utils.isFunction(this.meta.Query, false, this);
        this.Data.unshift({
            Id: null,
            Name: 'Selected Option',
            Description: 'Selected Option'
        });
        this.SS.setData(this.Data.map(x => ({ text: x.Name, value: x.id == null ? null : x.id.toString(), html: x.Description || x.Name })));
        this._value = this.entity[this.meta.fieldName] == null ? null : this.entity[this.meta.fieldName].toString();
        if (this._value === null) {
            this.Matched = null;
            this.entity[this.displayField] = null;
            this.SS.setSelected(this.Data[0].id);
            this.findMatchText();
            return;
        }
        else {
            this.SS.setSelected(this._value);
            this.findMatchText();
        }
    }

    async validateAsync() {
        if (this.validationRules.length == 0) {
            return true;
        }
        this.validationResult = [];
        this.validateRequired(this._value);
        this.Validate(ValidationRule.Equal, this._value, (value, ruleValue) => value === ruleValue);
        this.Validate(ValidationRule.notEqual, this._value, (value, ruleValue) => value !== ruleValue);
        return this.isValid;
    }

    setDisableUI(value) {
        if (this.SS !== null) {
            if (value) {
                this.SS.disable();
            }
            else {
                this.SS.enable();
            }
        }
    }

    removeDOM() {
        if (this._input !== null && this._input.parentElement !== null) {
            this._input.parentElement.remove();
        }
    }
}
