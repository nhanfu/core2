import { Action } from "./models/action.js";
import { string } from "./utils/ext.js";
import { Utils } from "./utils/utils.js";
import { ValidationRule } from "./models/validationRule.js";
import EventType from "./models/eventType.js";
import { ComponentType } from "./models/componentType.js";
import { Uuid7 } from "./models/uuidv7.js";

/**
 * @typedef {import('./models/action.js').Action} Action
 * @typedef {import('./models/component.js').Component} Component
 * @typedef {import('./models/observable.js').default} ObservableArgs
 * @typedef {{ [key: string] : (ValidationRule) }} Validation
 */

/**
 * Represents an editable component in the application.
 * @class
 * The `EditableComponent` class provides functionality for managing and interacting with editable components in the application.
 * It handles the parent-child relationships, event handling, and disposal of the component.
 */
export default class EditableComponent {
    /**
     * Create instance of component
     * @param {Component} meta 
     * @param {HTMLElement} ele 
     */
    constructor(meta, ele) {
        this.Meta = meta;
        this.Element = ele;

        if (meta?.Validation != null) {
            /** @type {Validation[]} */
            var rules = typeof meta.Validation === string.Type ? JSON.parse(meta.Validation) : meta.Validation;
            if (rules.HasElement()) {
                this.ValidationRules = rules.ToDictionary(x => x.Rule, x => x);
            }
        }
        this.DOMContentLoaded.add(() => {
            this.SetRequired();
            this.SendQueueAction("Subscribe");
            if (meta != null && meta.Events?.HasAnyChar()) {
                this.DispatchEvent(meta.Events, EventType.DOMContentLoaded, this.Entity).Done();
            }
        });
    }

    DispatchEvent(events, eventType, ...parameters) {
        if (!events) {
            return Promise.resolve(true);
        }
        return this.InvokeEvent(events, eventType, ...parameters);
    }

    InvokeEvent(events, eventTypeName, ...parameters) {
        let eventObj;
        try {
            eventObj = JSON.parse(events);
        } catch {
            return Promise.resolve(false);
        }
        const eventName = eventObj[eventTypeName];
        if (!eventName) {
            return Promise.resolve(false);
        }
        const func = Utils.IsFunction(eventName);
        if (func) {
            func.call(null, this, this.EditForm);
            return Promise.resolve(true);
        }

        let form = this.EditForm;
        if (!form) {
            return Promise.resolve(false);
        }
        const method = form[eventName];
        if (!method) {
            form = this.FindComponentEvent(form, eventName);
            if (!form) {
                return Promise.resolve(false);
            }
            method = form[eventName];
        }
        if (!method) {
            return Promise.resolve(false);
        }

        const tcs = new Promise((resolve, reject) => {
            let task = method.apply(form, parameters);
            if (!task || task.isCompleted == null) {
                resolve(false);
            } else {
                task.then(() => resolve(true)).catch(e => reject(e));
            }
        });
        return tcs;
    }

    DispatchCustomEvent(events, eventType, ...parameters) {
        if (!events) {
            return Promise.resolve(true);
        }
        const eventTypeName = eventType.toString();
        return this.InvokeEvent(events, eventTypeName, ...parameters);
    }

    /** @type {EditableComponent} EditForm - The root component of all tree node.*/
    EditForm;
    /** @type {EditableComponent} Parent - The parent component of this editable component.*/
    Parent;
    /** @type {EditableComponent[]} children - The child components of this editable component.*/
    Children = [];
    /** @type {HTMLElement} ParentElement - The parent element of this editable component. */
    ParentElement;
    /** @type {HTMLElement} Element - The HTML element representing this editable component. */
    Element;
    /** @type {Obj} Entity - The entity associated with this editable component. */
    Entity = {};
    /** @type {Component} */
    Meta;
    DefaultValue;
    /** @type {string[]} Classes - Represent hierarchy class of the component instance. */
    Classes = [];
    /** @type {boolean} emptyRow - True if the component is in empty row or screen, otherwise false. */
    #emptyRow;
    /** @type {Action} Disposed - Handle after dispose event. */
    Disposed = new Action();
    /** @type {Action} Disposed - Handle DOM Content loaded event. */
    DOMContentLoaded = new Action();
    /** @type {Action} Handle toggle event. */
    OnToggle = new Action();
    #rootTab;
    /** @type {EditableComponent} Handle toggle event. */
    get TabEditor() {
        if (this.#rootTab != null) return this.#rootTab;
        this.#rootTab = this.FindClosest('TabEditor', x => !x.Popup);
    }
    set TabEditor(editor) {
        this.#rootTab = editor;
    }
    #editForm;
    /** @type {EditableComponent} Handle toggle event. */
    get EditForm() {
        if (this.#editForm != null) return this.#editForm;
        this.#editForm = this.FindClosest('EditForm', x => !x.Popup);
    }
    /**
     * @param {EditableComponent} editor
     */
    set EditForm(editor) {
        this.#editForm = editor;

        /** @type {Action} */
    }
    UserInput = new Action();
    get IsSmallUp() { return document.clientWidth > 768 }
    get IsMediumUp() { return document.clientWidth > 992 }
    get IsLargeUp() { return document.clientWidth > 1200 }
    OldValue;
    /** @type {{[key: string]: string}} */
    ValidationResult = {};
    get ClassName() { return this.Element.className; }
    set ClassName(value) { this.Element.className = value; }
    /** @type {Validation} */
    ValidationRules = {};
    #disabled;
    /** @type {boolean} */
    get Disabled() {
        return this.#disabled;
    }
    set Disabled(value) {
        this.#disabled = value;
        this.SetDisableUI(value);
        this.Children?.forEach(x => {
            editable.Disabled = value;
        });
    }
    get Dirty() {
        return this._dirty && !this.AlwaysValid || this.FilterChildren(x => x._dirty, x => !x.PopulateDirty || x.AlwaysValid).Any();
    }
    set Dirty(value) {
        this.UpdateDirty(value);
    }
    /** @type {boolean} */
    AlwaysValid;
    get IsValid() {
        return this.ValidationResult?.Count === 0 || Object.keys(this.ValidationResult).length === 0;
    }
    PopulateDirty = true;
    get CacheName() {
        var exp = Meta?.CacheName;
        if (exp.IsNullOrWhiteSpace()) return null;
        var fn = Utils.IsFunction(exp);
        if (fn) {
            return fn.call(null, this);
        }
        return exp;
    }
    get QueueName() {
        return this.Meta?.QueueName;
    }
    /** @returns {string} Entity's Id */
    get EntityId() {
        return this.Entity?.Id;
    }
    /** @returns {string} Meta fieldname */
    get FieldName() {
        return this.Meta?.FieldName;
    }
    /** @type {boolean} emptyRow - True if the component is in empty row or screen, otherwise false. */
    get EmptyRow() {
        if (this.#emptyRow == null) {
            this.#emptyRow = this.FindClosest('ListViewItem')?.EmptyRow;
        }
        return this.#emptyRow;
    }
    set EmptyRow(val) {
        this.#emptyRow = val;
        let row = this.FindClosest('ListViewItem');
        if (row != null) row.EmptyRow = val;
    }
    /** @type {string} QueueName - Return meta data queue name. */
    get QueueName() {
        return this.Meta?.QueueName;
    }
    get MetaConn() {
        return this.Meta?.MetaConn;
    }
    get DataConn() {
        return this.Meta?.DataConn;
    }

    SetRequired() {
        const ele = this.Element;
        if (ele == null) return;
        if (this.ValidationRules?.hasOwnProperty(ValidationRule.Required)) {
            ele.setAttribute(ValidationRule.Required, true);
        }
        else {
            ele.removeAttribute(ValidationRule.Required);
        }
    }
    /**
     * 
     * @param {string} ruleType - Example 'required' or 'unique'
     * @param {any} value - Value to validate
     * @param {(item: any, rule: any) => boolean} validPredicate 
     * @returns 
     */
    Validate(ruleType, value, validPredicate) {
        if (!this.ValidationRules.hasOwnProperty(ruleType)) {
            return true;
        }
        let rule = this.ValidationRules[ruleType];
        if (rule === null || rule.Value1 === null) {
            return true;
        }
        let field = rule.Value1.toString();
        if (field === "") {
            return true;
        }
        let ruleValue = rule.Value1;
        let label = ruleValue;
        let [hasField, fieldVal] = this.Entity.GetComplexProp(field);
        if (hasField) {
            label = this.Parent.find(x => x.Name === field)?.Meta?.Label;
            ruleValue = fieldVal;
        }
        if (!validPredicate(value, ruleValue)) {
            this.ValidationResult[ruleType] = string.Format(rule.Message, this.Meta.Label, label);
            return true;
        }
        else {
            delete this.ValidationResult[ruleType];
        }
        return false;
    }
    SetDisableUI(disabled) {
        const ele = this.Element;
        if (ele == null) {
            return;
        }

        if (disabled) {
            ele.setAttribute("disabled", "disabled");
        }
        else {
            ele.removeAttribute("disabled");
            ele.setAttribute("enable", "true");
        }
    }
    _dirty;
    _setDirty;
    UpdateDirty(dirty) {
        if (dirty) {
            this.SetDirtyInternal();
        }
        else {
            this.ClearDirtyInternal();
            this.FilterChildren(x => x._dirty).SelectForEach(x => x.ClearDirtyInternal());
        }
    }
    SetDirtyInternal() {
        this._dirty = this._setDirty;
        if (!this._setDirty) {
            this._setDirty = true;
        }
    }
    ClearDirtyInternal() {
        this._dirty = false;
    }
    SetDefaultVal() {
        if (this.Entity == null || this.EntityId == null) return;
        var fn = Utils.IsFunction(this.Meta.DefaultVal);
        if (fn) {
            fn.call(this, this);
        }
        else if (this.Entity.GetComplexProp(this.FieldName) == null) {
            this.Entity.SetComplexPropValue(this.FieldName, this.DefaultValue);
        }
    }
    ValidateRequired(value) {
        if (this.Element === null || Object.keys(this.ValidationRules).length === 0 || this.EmptyRow || this.AlwaysValid) {
            return true;
        }
    
        if (!this.ValidationRules.hasOwnProperty(ValidationRule.Required)) {
            this.Element.removeAttribute(ValidationRule.Required);
            return true;
        }
    
        const requiredRule = this.ValidationRules[ValidationRule.Required];
        this.Element.setAttribute(ValidationRule.Required, true.toString());
    
        if (value === null || value === undefined || value.toString().trim() === "") {
            this.Element.removeAttribute("readonly");
            this.ValidationResult[ValidationRule.Required] = requiredRule.Message.replace("{0}", LangSelect.Get(this.Meta.Label)).replace("{1}", this.Entity);
            return false;
        } else {
            delete this.ValidationResult[ValidationRule.Required];
            return true;
        }
    }
    
    AddRule(rule) {
        this.ValidationRules[rule.Rule] = rule;
        if (rule.Rule === ValidationRule.Required) {
            this.Element.setAttribute(ValidationRule.Required, true.toString());
        }
    }
    
    RemoveRule(ruleName) {
        delete this.ValidationRules[ruleName];
        if (!Object.keys(this.ValidationRules).includes(ValidationRule.Required)) {
            this.Element.removeAttribute(ValidationRule.Required);
        }
    }
    
    CascadeField() {
        if (this.Meta.CascadeField === null || this.Meta.CascadeField.trim() === "") {
            return;
        }
    
        const root = this.FindClosest(ComponentType.ListViewItem) ?? this.EditForm;
        const cascadeFields = this.Meta.CascadeField.split(",").map(field => field.trim()).filter(x => x !== "");
        if (cascadeFields.length === 0) {
            return;
        }
    
        cascadeFields.forEach(field => {
            root.FilterChildren(x => x.Name === field).forEach(target => {
                if (target instanceof SearchEntry && target !== null) {
                    target.Value = null;
                    target.Meta.LocalData = null;
                } else {
                    target.UpdateView();
                }
            });
        });
    }

    GetInvalid() {
        return this.Children.Flattern(x => x.AlwaysValid ? null : x.Children).Where(x => !x.IsValid);
    }

    PopulateFields(entity = null) {
        if (this.Entity == null || this.Meta.PopulateField == null) {
            return;
        }

        const gridRow = this.FindClosest(ComponentType.ListViewItem) ?? this.FindClosest(ComponentType.EditForm);
        const root = gridRow !== null ? gridRow : this.EditForm;

        const fn = Utils.IsFunction(this.Meta.PopulateField);
        if (fn) {
            try {
                fn.call(null, this, entity);
            } catch (error) {
                console.error(error);
            }
            root.UpdateViewAwait(true);
            return;
        }

        const populatedFields = this.Meta.PopulateField.split(",").map(field => field.trim()).filter(x => x !== "");
        if (entity === null || populatedFields.length === 0) {
            return;
        }

        populatedFields.forEach(field => {
            root.FilterChildren(x => x.Name === field).forEach(target => {
                const value = Utils.GetPropValue(entity, field);
                const oldVal = Utils.GetPropValue(this.Entity, field);
                const targetType = this.Entity.constructor.GetComplexPropType(field);
                if (value === oldVal || targetType === null || new targetType() !== oldVal) {
                    return;
                }
                this.Entity.SetComplexPropValue(field, value);
                target.UpdateView(true, false);
            });
        });
    }

    GetValueTextAct() {
        return this.Element.textContent;
    }

    AddIdToPatch(details) {
        const idFieldIndex = details.findIndex(x => x.Field === Utils.IdField);
        if (idFieldIndex !== -1) details.splice(idFieldIndex, 1);
        if (this.EntityId === null) {
            details.push({ Field: Utils.IdField, Value: Uuid7.Id25() });
        } else {
            details.push({ Field: Utils.IdField, Value: this.EntityId, OldVal: this.EntityId });
        }
    }

    addEventListener(name, handler) {
        if (handler === null) throw new Error("Handler cannot be null");
        const handlers = this._events[name] || null;
        if (handlers === null) {
            this._events[name] = handler;
        } else {
            this._events[name] += handler;
        }
    }

    removeEventListener(name, handler) {
        if (handler === null) throw new Error("Handler cannot be null");
        const handlers = this._events[name] || null;
        if (handlers !== null) {
            delete this._events[name];
        }
    }

    FindComponentByName(name, type) {
        return this.FirstOrDefault(x => x.Name === name && (type == null || x.Classes.includes(type)));
    }

    /**
     * Find closeset component
     * @param {string} type - Class type name
     * @param {(value: EditableComponent, index: number) => boolean} filter - Filter component
     * @returns {EditableComponent} Returns the closeset EditableComponent of the specified type
     */
    FindClosest(type, filter) {
        let found = this;
        while (found != null) {
            if (found.Classes?.includes(type) || found.Meta?.ComponentType === type) {
                if (filter == null || filter(found)) return found;
            }
            found = found.Parent;
        }
        return null;
    }
    #show;
    get Show() {
        return this.#show;
    }
    set Show(val) {
        this.#show = val;
        const ele = this.Element;
        const meta = this.Meta;
        if (!val) {
            ele.Style.Display = "none";
            if (meta != null && meta.ShowLabel && FieldName != null) {
                ele.ParentElement.style.display = "none";
                ele.ParentElement.previousElementSibling.style.display = "none";
            }
        }
        else {
            ele.style.display = string.Empty;
            if (meta != null && meta.ShowLabel && FieldName != null) {
                ele.ParentElement.Style.Display = "";
                ele.ParentElement.PreviousElementSibling.Style.Display = "";
            }
        }

        this.OnToggle?.Invoke(this.#show);
    }
    /**
     * Show / hide the component
     * @param {string} showExp 
     */
    ToggleShow(showExp) {
        var fn = Utils.IsFunction(showExp);
        if (showExp?.HasAnyChar() && fn) {
            var shown = fn.Call(null, this);
            Show = shown ?? false;
        }
    }
    ToggleDisabled(disabled) {
        var disabledFn = Utils.IsFunction(disabled);
        if (disabled !== null) {
            let shouldDisabled = disabledFn(null, this) || false;
            this.Disabled = shouldDisabled;
        }
    }
    Dispose() {
        this.SendQueueAction("Unsubscribe");
        this.DisposeChildren();
        this.RemoveDOM();
        this.Children = null;
        this.DOMContentLoaded = null;
        this.OnToggle = null;
        if (this.Parent != null && this.Parent.Children != null
            && this.Parent.Children.ToArray().HasElement()
            && this.Parent.Children.ToArray().Contains(this)) {
            this.Parent.Children.ToArray().Remove(this);
        }
        this.Disposed?.Invoke();
    }
    RemoveDOM() {
        if (this.Element != null) {
            this.Element.remove();
            this.Element = null;
        }
    }
    SendQueueAction(action) {
        var queueName = this.QueueName;
        if (queueName?.IsNullOrWhiteSpace()) return;
        const param = { QueueName: queueName, Action: action };
        this.EditForm?.NotificationClient?.Send(JSON.stringify(param));
        if (action == "Subscribe")
            window.addEventListener(queueName, this.QueueHandler);
        else
            window.removeEventListener(queueName, this.QueueHandler);
    }
    DisposeChildren() {
        if (this.Children.Nothing()) return;
        var leaves = this.Children.Flattern(x => x.Children)
            .Where(x => x.Element != null && x.Parent != null && x.Children.Nothing());
        while (leaves.HasElement()) {
            leaves.forEach(x => {
                if (x == null) return;
                x.Dispose();
                if (x.Parent != null && x.Parent.Children != null) {
                    x.Parent.Children.Remove(x);
                }
            });
            leaves = this.Children.Flattern()?.Where(x => x.Element != null && x.Parent != null && x.Children.Nothing()).ToArray();
        }
    }
    GetValueText() {
        if (this.Element === null) {
            return "";
        }
        if (this.Element instanceof HTMLInputElement) {
            return this.Element.value;
        }
        if (this.Element instanceof HTMLTextAreaElement) {
            return this.Element.value;
        }
        return "";
    }

    ValidateAsync() {
        return Promise.resolve(true);
    }
    /**
     * 
     * @param {(item: EditableComponent) => boolean} filter 
     * @param {(item: EditableComponent) => boolean} ignore 
     * @returns 
     */
    FilterChildren(filter, ignore) {
        return this.Children.Flattern(x => x.Children.Where(child => {
            return ignore?.call(this, child) !== true && filter?.call(this, child) === true;
        })).Where(filter);
    }
    /**
     * Returns the first element of the collection that satisfies the specified condition, or null if no such element is found.
     * @param {(item: EditableComponent) => boolean} filter - The condition to check for each element.
     * @returns {EditableComponent|null} The first element that satisfies the condition, or null if no such element is found.
     */
    FirstOrDefault(filter) {
        return this.Children.Flattern(x => x.Children).FirstOrDefault(filter);
    }
}