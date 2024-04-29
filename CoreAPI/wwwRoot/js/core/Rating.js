import EditableComponent from './editableComponent.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { ValidationRule } from "./models/validationRule.js";
import { LangSelect } from "./utils/langSelect.js";
import { Client } from "./clients/client.js";
import EventType from './models/eventType.js';
import { ComponentType } from './models/componentType.js';
import { string } from './utils/ext.js';
import ObservableArgs from './models/observable.js';
import { Action } from "./models/action.js";
import "../fix.js";
import { Client } from "./clients/client.js";
import EditableComponent from "./editableComponent.js";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";


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

    setSelected(value) {
        if (value === null || value <= 0 || value > this.Meta.Precision) {
            return;
        }
        // Adjust for zero-based index in JavaScript
        this.InputList[this.Meta.Precision - value].checked = true;
    }

    render() {
        const container = document.createElement("div");
        container.className = "rate";
        this.ParentElement.appendChild(container);

        const radioGroupName = `${this.fieldName}_${this.Meta.Id}_${this.hashCode()}`;
        for (let i = this.Meta.Precision; i >= 1; i--) {
            const radioId = `${radioGroupName}_${i}`;
            const input = document.createElement("input");
            input.type = "radio";
            input.id = radioId;
            input.name = radioGroupName;
            input.value = i;
            container.appendChild(input);
            this.InputList.push(input);

            const label = document.createElement("label");
            label.setAttribute("for", radioId);
            label.textContent = `${i} stars`;
            container.appendChild(label);

            input.addEventListener("change", this.dispatchChange.bind(this));
        }
        this.Element = container;

        this.value = this.getValueFromEntity();
        this.setSelected(this.value);
        this.DOMContentLoaded?.Invoke();
    }

    dispatchChange(event) {
        if (this.Disabled) return;

        const checkedInput = this.InputList.find(input => input.checked);
        if (!checkedInput) return;

        const oldValue = this.value;
        this.value = parseInt(checkedInput.value);
        if (this.UserInput) {
            this.UserInput.Invoke(new ObservableArgs ({ newData: this.value, oldData: oldValue }));
        }
        // Assuming dispatchEvent is defined
        setTimeout(() => {
            this.DispatchEvent(this.Meta.Events, 'click', this.Entity);
        }, 0);
    }

    getValueFromEntity() {
        return Utils.GetPropValue(this.entity, this.fieldName);
    }

    hashCode() {
        return JSON.stringify(this.meta).split("").reduce((a, b) => {
            a = ((a << 5) - a) + b.charCodeAt(0);
            return a & a;
        }, 0);
    }
}

window.Core2 = window.Core2 || {};
window.Core2.Rating = Rating;

export default Rating;