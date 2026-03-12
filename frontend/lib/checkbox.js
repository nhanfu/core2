import { ObservableArgs, EventType, Component, keyCodeEnum } from "./models/";
import { EditableComponent } from "./editableComponent.js";
import { html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";

/**
 * represents a Checkbox component.
 */
export class Checkbox extends EditableComponent {
    /** @type {?boolean} */
    _value = null;

    /** @type {hTMLInputElement} */
    _input = null;

    /**
     * constructs a Checkbox component.
     * @param {Component} ui - the uI component associated with the checkbox.
     * @param {HTMLElement} [ele=null] - the HTML element to which the checkbox belongs.
     * @throws {error} if the uI component is not provided.
     */
    constructor(ui, ele = null) {
        super(ui);
        if (!ui) throw new error("ui is required");
        this.meta = ui;
        this.parentElement = ele;
        if (ele && ele.tagName.toLowerCase() === 'input') {
            this.element = ele;
            // @ts-ignore
            this._input = ele;
        }
        else this.parentElement = ele;
        this.defaultValue = false;
    }

    /**
     * renders the Checkbox into the dOM.
     */
    render() {

        if (this.parentElement != null && this.element == null) {
            html.take(this.parentElement).tabIndex(-1).smallCheckbox(this._value ?? false);
            this._input = html.context.previousElementSibling;
        }
        this.element = this._input.parentElement ?? this._input;
        html.take(this._input).event('input', this.userChange.bind(this));
        this.setDisableUI(!this.meta.editable);
        this.setDefaultVal();
        this.value = Utils.getPropValue(this.entity, this.name);
        this.entity[this.name] = this._value;
        if (!this.entity[this.name]) {
            this.entity[this.name] = false;
        }

        this.element.closest('td')?.addEventListener('keydown', this.userKeyDown.bind(this));
        this.dOMContentLoaded?.invoke();
    }

    /**
     * gets the value of the Checkbox as a string.
     * @returns {string} the string representation of the Checkbox's value.
     */
    getValueText() {
        return this._value === null ? "n/A" : (this._value ? "check" : "not check");
    }

    /**
     * handles user interactions with the Checkbox.
     * @param {event} e - the event object.
     */
    userChange(e) {
        if (this.disabled) {
            e.preventDefault();
            return;
        }
        const check = this._input.checked;
        this.dataChanged(check);
    }

    userKeyDown(e) {
        let code = e.keyCodeEnum();
        if (code == keyCodeEnum.space && !this.disabled) {
            e.preventDefault();
            this.value = !this.value;
            const check = this._input.checked;
            this.dataChanged(check);
        }
    }

    /**
     * handles data changes in the Checkbox.
     * @param {boolean} check - the new checked state of the Checkbox.
     */
    dataChanged(check) {
        const oldVal = this._value;
        this._value = check;
        if (this.entity) {
            this.entity[this.name] = check;
        }
        this.dirty = true;
        // @ts-ignore
        var arg = new ObservableArgs({ newData: this._value, oldData: oldVal, evType: EventType.change });
        /** @type {ObservableArgs} */
        // @ts-ignore
        var arg = { newData: this._value, oldData: oldVal, evType: EventType.change };
        this.userInput?.invoke(arg);
        this.populateFields();
        this.cascadeField();
        this.dispatchEvent(this.meta.events, EventType.change, this, this.entity).then();
    }

    get value() { return this._value; }
    set value(val) {
        if (val == undefined) {
            val = false;
        }
        this._value = val;
        this._input.checked = val;
    }
    /**
     * updates the view of the Checkbox based on the current state.
     * @param {boolean} [force=false] - force the update regardless of changes.
     * @param {?boolean} [dirty=null] - the new dirty state.
     * @param {...string} componentNames - additional component names to update.
     */
    updateView(force = false, dirty = null, ...componentNames) {
        this.value = this.entity[this.meta.fieldName];
        if (!this.dirty) {
            this.originalText = this._input.value;
            this.dOMContentLoaded?.invoke();
            this.oldValue = this.value;
        }
    }

    /**
     * sets the uI disabled state for the Checkbox.
     * @param {boolean} value - whether to disable the uI.
     */
    setDisableUI(value) {
        if (value) {
            this.element.setAttribute('disabled', 'disabled');
        } else {
            this.element.removeAttribute('disabled');
        }
        this._input.disabled = value;
    }
}