import { EditableComponent } from './editableComponent.js';
import { Utils } from "./utils/utils.js";
import ObservableArgs from './models/observable.js';
import "./utils/fix.js";


export class Rating extends EditableComponent {
    /**
    * @param {import('./models/component.js').Component} ui
    * @param {HTMLElement} [ele=null] 
    */

    constructor(ui, ele = null) {
        super(ui, ele);
        this.defaultValue = 0;
        if (!ui) throw new Error("UI component is required");
        this.parentElement = ele;
        this.inputList = [];
        this._value = null;

        this.render();
    }

    get value() {
        return this._value;
    }

    set value(val) {
        if (this._value === val) {
            return;
        }
        this._value = val;
        this.setSelected(this._value);
        this.Entity[this.Name] = this._value;
        this.dirty = true;
    }

    get Disabled() {
        return super.Disabled;
    }

    set Disabled(value) {
        super.Disabled = value;
        this.inputList.forEach(input => {
            input.Disabled = value;
        });
    }

    setSelected(value) {
        if (value === null || value <= 0 || value > this.meta.Precision) {
            return;
        }
        this.inputList[this.meta.Precision - value].checked = true;
    }

    render() {
        const container = document.createElement('div');
        container.className = 'rate';
        this.parentElement.appendChild(container);
        this.element = container;

        const radioGroup = `${this.Name}_${this.meta.Id}_${this.hashCode()}`;
        for (let item = this.meta.Precision; item >= 1; item--) {
            const radioId = `${radioGroup}_${item}`;
            const input = document.createElement('input');
            input.setAttribute('type', 'radio');
            input.id = radioId;
            input.name = radioGroup;
            input.value = item.toString();
            // @ts-ignore
            input.style = this.meta.Style;
            input.addEventListener('change', this.dispatchChange.bind(this));

            this.inputList.push(input);
            this.element.appendChild(input);

            const label = document.createElement('label');
            label.setAttribute('for', radioId);
            label.textContent = `${item} stars`;
            this.element.appendChild(label);
        }

        this._value = Utils.getPropValue(this.Entity, this.Name);
        this.setSelected(this._value);

        this.dOMContentLoaded?.invoke();
    }

    dispatchChange(event) {
        if (this.Disabled) return;

        if (!this.inputList.length) return;

        const checkedInput = this.inputList.find(input => input.checked);
        if (!checkedInput) return;

        const oldValue = this.value;
        this.value = parseInt(checkedInput.value);
        if (this.userInput) {
            // @ts-ignore
            this.userInput.invoke(new ObservableArgs({ newData: this.value, oldData: oldValue }));
        }
        setTimeout(() => {
            this.dispatchEvent(this.meta.events, 'click', this.Entity).then();
        }, 0);
    }

    updateView(force = false, dirty = null, ...componentNames) {
        this.value = Utils.getPropValue(this.Entity, this.Name);
        this.value = (this.value !== undefined && this.value !== null) ? parseInt(this.value) : null;
    }

    getValueText() {
        return this._value === null ? "Không đánh giá" : `${this._value} sao`;
    }

    async validateAsync() {
        this.validationResult = [];
        if (this.value === null) return false;
        const isValid = this.value !== undefined && this.validateRequired(this.value);
        return isValid;
    }

    hashCode() {
        return JSON.stringify(this.meta).split("").reduce((a, b) => {
            a = ((a << 5) - a) + b.charCodeAt(0);
            return a & a;
        }, 0);
    }
}