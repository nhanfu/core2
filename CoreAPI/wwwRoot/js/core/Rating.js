import EditableComponent from './editableComponent.js';
import { Utils } from "./utils/utils.js";
import ObservableArgs from './models/observable.js';
import EditableComponent from "./editableComponent.js";
import { Utils } from "./utils/utils.js";
import "../fix.js";


class Rating extends EditableComponent {
     /**
     * @param {Component} ui
     * @param {HTMLElement} [ele=null] 
     */
    
    constructor(ui, ele = null) {
        super(ui,ele);
        DefaultValue = 0;
        if (!ui) throw new Error("UI component is required");
        this.ParentElement = ele;
        this.InputList = [];
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
        this.Entity.setComplexPropValue(this.fieldName, this._value);
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
        if (value === null || value <= 0 || value > this.Meta.Precision) {
            return;
        }
        this.InputList[this.Meta.Precision - value].checked = true;
    }

    render() {
        const container = document.createElement('div');
        container.className = 'rate';
        this.ParentElement.appendChild(container);
        this.Element = container;
    
        const radioGroup = `${this.fieldName}_${this.meta.id}_${this.hashCode()}`;
        for (let item = this.Meta.Precision; item >= 1; item--) {
            const radioId = `${radioGroup}_${item}`;
            const input = document.createElement('input');
            input.setAttribute('type', 'radio');
            input.id = radioId;
            input.name = radioGroup;
            input.value = item.toString();
            input.style = this.meta.style; 
            input.addEventListener('change', this.dispatchChange.bind(this));
    
            this.inputList.push(input);
            this.element.appendChild(input);
    
            const label = document.createElement('label');
            label.setAttribute('for', radioId);
            label.textContent = `${item} stars`;
            this.Element.appendChild(label);
        }
    
        this._value = Utils.GetPropValue(this.Entity, this.FieldName);
        this.setSelected(this._value);
    
        this.DOMContentLoaded?.Invoke();
    }

    dispatchChange(event) {
        if (this.Disabled) return;

        if (!inputList.length) return;
        
        const checkedInput = this.InputList.find(input => input.checked);
        if (!checkedInput) return;

        const oldValue = this.value;
        this.value = parseInt(checkedInput.value);
        if (this.UserInput) {
            this.UserInput.Invoke(new ObservableArgs ({ newData: this.value, oldData: oldValue }));
        }
        setTimeout(() => {
            this.DispatchEvent(this.Meta.Events, 'click', this.Entity);
        }, 0);
    }

    updateView(force = false, dirty = null, ...componentNames) {
        this.value = Utils.GetPropValue(this.Entity, this.FieldName);
        this.value = (this.value !== undefined && this.value !== null) ? parseInt(this.value) : null;
    }

    getValueText() {
        return this._value === null ? "Không đánh giá" : `${this._value} sao`;
    }

    async validateAsync() {
        this.ValidationResult.clear();
        if (this.value === null) return false;
        const isValid = this.value !== undefined && this.ValidateRequired(this.value);
        return isValid;
    }

    hashCode() {
        return JSON.stringify(this.Meta).split("").reduce((a, b) => {
            a = ((a << 5) - a) + b.charCodeAt(0);
            return a & a;
        }, 0);
    }
}

window.Core2 = window.Core2 || {};
window.Core2.Rating = Rating;

export default Rating;