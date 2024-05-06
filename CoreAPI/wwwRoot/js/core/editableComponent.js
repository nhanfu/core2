import { Str } from "./utils/ext.js";
import { Utils } from "./utils/utils.js";
import { ValidationRule } from "./models/validationRule.js";
import EventType from "./models/eventType.js";
import { ComponentType } from "./models/componentType.js";
import { Uuid7 } from "./structs/uuidv7.js";
import { Html } from "./utils/html.js";
import './utils/ext.js';
import { Action } from "./models/action.js";
import { LangSelect } from "./utils/langSelect.js";
import { KeyCodeEnum } from "./models/enum.js";

/**
 * @typedef {import('./editForm.js').EditForm} EditForm
 * @typedef {import('./tabEditor.js').TabEditor} TabEditor
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
     * @param {Component | null} meta 
     * @param {HTMLElement | null} ele 
     */
    constructor(meta, ele = null) {
        this.Meta = meta;
        this.Element = ele;

        if (meta?.Validation != null) {
            /** @type {Validation[]} */
            var rules = typeof meta.Validation === Str.Type ? JSON.parse(meta.Validation.toString()) : meta.Validation;
            if (rules.HasElement()) {
                this.ValidationRules = rules.ToDictionary(x => x.Rule, x => x);
            }
        }
        this.DOMContentLoaded.add(() => {
            this.SetRequired();
            this.SendQueueAction("Subscribe");
            if (meta != null && meta.Events) {
                this.DispatchEvent(meta.Events, EventType.DOMContentLoaded, this.Entity).Done();
            }
        });
    }

    /**
     * Small screen size.
     * @type {number}
     */
    static SmallScreen = 768;

    /**
     * Extra small screen size.
     * @type {number}
     */
    static ExSmallScreen = 567;

    /**
     * Medium screen size.
     * @type {number}
     */
    static MediumScreen = 992;

    /**
     * Large screen size.
     * @type {number}
     */
    static LargeScreen = 1200;

    /**
     * Extra large screen size.
     * @type {number}
     */
    static ExLargeScreen = 1452;

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

    /**
     * @param {string} events
     * @param {string} eventType
     * @param {(any[] | EditableComponent)[]} parameters
     */
    DispatchCustomEvent(events, eventType, ...parameters) {
        if (!events) {
            return Promise.resolve(true);
        }
        const eventTypeName = eventType.toString();
        return this.InvokeEvent(events, eventTypeName, ...parameters);
    }

    /** @type {EditableComponent} Parent - The parent component of this editable component.*/
    Parent;
    /** @type {EditableComponent[]} children - The child components of this editable component.*/
    Children = [];
    /** @type {HTMLElement} ParentElement - The parent element of this editable component. */
    ParentElement;
    /** @type {HTMLElement | null} Element - The HTML element representing this editable component. */
    Element;
    /** @type {Object} Entity - The entity associated with this editable component. */
    Entity = {};
    /** @type {Component} */
    Meta;
    /** @type {any} */
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
    /** @type {TabEditor} Handle toggle event. */
    #rootTab;
    /** @type {TabEditor} Handle toggle event. */
    get TabEditor() {
        if (this.#rootTab != null) return this.#rootTab;
        // @ts-ignore
        this.#rootTab = this.FindClosest(TabEditor, x => !x.Popup);
    }
    set TabEditor(editor) {
        this.#rootTab = editor;
    }
    /** @type {EditForm} */
    #editForm;
    /** @type {EditForm} */
    get EditForm() {
        if (this.#editForm != null) return this.#editForm;
        // @ts-ignore
        this.#editForm = this.FindClosest(EditForm.prototype, x => !x.Popup);
    }
    /**
     * @param {EditForm} editor
     */
    set EditForm(editor) {
        this.#editForm = editor;

        /** @type {Action} */
    }
    UserInput = new Action();
    get IsSmallUp() { return document.body.clientWidth > 768 }
    get IsMediumUp() { return document.body.clientWidth > 992 }
    get IsLargeUp() { return document.body.clientWidth > 1200 }
    /** @type {any} */
    OldValue;
    /** @type {{[key: string]: string}} */
    ValidationResult = {};
    get ClassName() { return this.Element.className; }
    set ClassName(value) { this.Element.className = value; }
    /** @type {Validation} */
    ValidationRules = {};
    /** @type {boolean} */
    #disabled;
    /** @type {boolean} */
    get Disabled() {
        return this.#disabled;
    }
    set Disabled(value) {
        this.#disabled = value;
        this.SetDisableUI(value);
        this.Children.Flattern(x => x.Children).forEach(x => {
            x.#disabled = value;
            x.SetDisableUI(value);
        });
    }
    /** @type {boolean} */
    _dirty;
    get Dirty() {
        return this._dirty && !this.AlwaysValid || this.FilterChildren(x => x._dirty, x => !x.PopulateDirty || x.AlwaysValid).Any();
    }
    set Dirty(value) {
        this._dirty = value;
        if (!value) {
            this.Children.Flattern(x => x.Children).Where(x => x._dirty).forEach(x => x._dirty = false);
        }
    }
    /** @type {boolean} */
    AlwaysValid;
    get IsValid() {
        return Object.keys(this.ValidationResult).length === 0;
    }
    PopulateDirty = true;
    get CacheName() {
        var exp = this.Meta?.CacheName;
        if (!exp) return null;
        var fn = Utils.IsFunction(exp);
        return fn ? fn.call(null, this) : exp;
    }
    get QueueName() {
        return this.Meta?.QueueName;
    }
    /** @returns {string} Entity's Id */
    get EntityId() {
        return this.Entity?.Id;
    }
    set EntityId(value) {
        if (this.Entity == null) return;
        this.Entity.Id = value;
    }
    /** @returns {string} Meta Label */
    get Label() {
        return this.Meta?.Label;
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
    get MetaConn() {
        return this.Meta?.MetaConn;
    }
    get DataConn() {
        return this.Meta?.DataConn;
    }
    IdField = 'Id';
    get FieldVal() { return !this.Entity || !this.FieldName ? null : this.Entity.GetComplexProp(this.FieldName); }
    set FieldVal(val) {
        if (this.Entity == null || this.FieldName == null) return;
        this.Entity.SetComplexPropValue(this.FieldName, val);
    }

    SetRequired() {
        const ele = this.Element;
        if (ele == null) return;
        if (this.ValidationRules?.hasOwnProperty(ValidationRule.Required)) {
            ele.setAttribute(ValidationRule.Required, true .toString());
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
            label = this.Parent.FirstOrDefault(x => x.FieldName === field)?.Meta?.Label;
            ruleValue = fieldVal;
        }
        if (!validPredicate(value, ruleValue)) {
            this.ValidationResult[ruleType] = Str.Format(rule.Message, this.Meta.Label, label);
            return true;
        }
        else {
            delete this.ValidationResult[ruleType];
        }
        return false;
    }
    /**
     * @param {boolean} [disabled]
     */
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
        if (!this.Meta.CascadeField) {
            return;
        }

        const root = this.FindClosest(ComponentType.ListViewItem) ?? this.EditForm;
        const cascadeFields = this.Meta.CascadeField.split(",").map(field => field.trim()).filter(x => x !== "");
        if (cascadeFields.length === 0) {
            return;
        }

        cascadeFields.forEach(field => {
            root.FilterChildren(x => x.FieldName === field).forEach(target => {
                // @ts-ignore
                if (target instanceof SearchEntry && target !== null) {
                    // @ts-ignore
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

        // @ts-ignore
        const gridRow = this.FindClosest(ListViewItem.prototype) ?? this.FindClosest(ComponentType.EditForm);
        const root = gridRow !== null ? gridRow : this.EditForm;

        const fn = Utils.IsFunction(this.Meta.PopulateField);
        if (fn) {
            try {
                fn.call(null, this, entity);
            } catch (error) {
                console.error(error);
            }
            root.UpdateView(true);
            return;
        }

        const populatedFields = this.Meta.PopulateField.split(",").map(field => field.trim()).filter(x => x !== "");
        if (entity === null || populatedFields.length === 0) {
            return;
        }

        populatedFields.forEach(field => {
            root.FilterChildren(x => x.FieldName === field).forEach(target => {
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

    _events = {};
    addEventListener(name, handler) {
        if (handler === null) throw new Error("Handler cannot be null");
        const handlers = this._events[name] || null;
        if (handlers == null) {
            this._events[name] = [handler];
        } else {
            handlers.push(handler);
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
        return this.FirstOrDefault(x => x.FieldName === name && (type == null || x.Classes.includes(type)));
    }

    /**
     * Find closeset component
     * @param {any} proto - Class prototype
     * @param {(value: EditableComponent) => boolean} filter - Filter component
     * @returns {EditableComponent} Returns the closeset EditableComponent of the specified type
     */
    FindClosest(proto, filter = null) {
        /** @type {EditableComponent} */
        let found = this;
        while (found != null) {
            if (found.constructor.prototype === proto || found.Meta?.ComponentType === proto) {
                if (filter == null || filter(found)) return found;
            }
            found = found.Parent;
        }
        return null;
    }
    /** @type {boolean} */
    #show;
    get Show() {
        return this.#show;
    }
    set Show(val) {
        this.#show = val;
        const ele = this.Element;
        const meta = this.Meta;
        if (!val) {
            ele.style.display = "none";
            if (meta != null && meta.ShowLabel && this.FieldName != null) {
                ele.parentElement.style.display = "none";
                // @ts-ignore
                ele.parentElement.previousElementSibling.style.display = "none";
            }
        }
        else {
            ele.style.display = Str.Empty;
            if (meta != null && meta.ShowLabel && this.FieldName != null) {
                ele.parentElement.style.display = "";
                // @ts-ignore
                ele.parentElement.previousElementSibling.style.display = "";
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
            var shown = fn.call(null, this);
            this.Show = shown ?? false;
        }
    }

    ToggleDisabled(disabled) {
        var disabledFn = Utils.IsFunction(disabled);
        if (disabled !== null) {
            let shouldDisabled = disabledFn(null, this) || false;
            this.Disabled = shouldDisabled;
        }
    }

    ListViewItemTab(e) {
        const code = e.keyCode;
        // @ts-ignore
        const listViewItem = this.FindClosest(ListViewItem.prototype);
        if (!listViewItem) {
            return;
        }
    
        switch (code) {
            case KeyCodeEnum.Tab:
                const td = e.target.closest('td');
                if (td && !td.nextElementSibling) {
                    e.preventDefault();
                    let nextElement = listViewItem.Children.find(x => x.Meta.Editable) ||
                                      listViewItem.Children.find(x => x.Meta.Id != null);
                    nextElement?.Focus();
                    return;
                }
                if (e.shiftKey) {
                    // @ts-ignore
                    if (this.constructor.prototype === Label.prototype && this.Meta.ComponentType && !this.Meta.Editable && td && td.previousElementSibling) {
                        e.preventDefault();
                        let nextElement = listViewItem.Children.find(x => x.Element.closest('td') === td.previousElementSibling);
                        nextElement?.Focus();
                        return;
                    }
                } else {
                    // @ts-ignore
                    if (this instanceof Label && this.Meta.ComponentType && !this.Meta.Editable && td && td.nextElementSibling) {
                        e.preventDefault();
                        let nextElement = listViewItem.Children.find(x => x.Element.closest('td') === td.nextElementSibling);
                        nextElement?.Focus();
                        return;
                    }
                }
                break;
    
            case KeyCodeEnum.Enter:
                if (listViewItem && this.EditForm.Feature.CustomNextCell) {
                    // @ts-ignore
                    if (this instanceof SearchEntry && this._gv && this._gv.Show) {
                        return;
                    }
                    const td = this.Element.closest('td');
                    if (this.Meta.ComponentType && td) {
                        if (e.shiftKey && td.previousElementSibling) {
                            // @ts-ignore
                            let nextElement = listViewItem.filterChildren(x => x.Element.closest('td') === td.previousElementSibling).find(x => true);
                            if (nextElement) {
                                // @ts-ignore
                                this.focusElement(nextElement);
                            }
                        } else if (td.nextElementSibling) {
                            // @ts-ignore
                            let nextElement = listViewItem.filterChildren(x => x.Element.closest('td') === td.nextElementSibling).find(x => true) ||
                                              listViewItem.Children[0];
                            // @ts-ignore
                            this.focusElement(nextElement);
                        }
                    }
                }
                break;
    
            default:
                break;
        }
    }

    UpdateView(force = false, dirty = null, ...componentNames) {
        this.PrepareUpdateView(force, dirty);
        if (!this.Children || this.Children.length === 0) {
            return;
        }
    
        if (componentNames.length > 0) {
            // @ts-ignore
            const coms = this.FilterChildren(x => x instanceof Section && componentNames.includes(x.FieldName))
                // @ts-ignore
                .flatMap(x => x.FilterChildren(com => !(com instanceof Section)));
            // @ts-ignore
            const coms2 = this.FilterChildren(x => componentNames.includes(x.FieldName) && !(x instanceof Section));
            // @ts-ignore
            const shouldUpdate = [...new Set([...coms, ...coms2].filter(x => !(x instanceof Section)))];
    
            shouldUpdate.forEach(child => {
                child.PrepareUpdateView(force, dirty);
                child.UpdateView(force, dirty, ...componentNames);
            });
        } else {
            // @ts-ignore
            const shouldUpdate = this.FilterChildren(x => !(x instanceof Section));
    
            shouldUpdate.forEach(child => {
                child.PrepareUpdateView(force, dirty);
                child.UpdateView(force, dirty, ...componentNames);
            });
        }
    }

    /**
     * @param {boolean} force
     * @param {boolean} dirty
     */
    PrepareUpdateView(force, dirty) {
        if (force) {
            this.EmptyRow = false;
        }
        this.ToggleShow(this.Meta?.ShowExp);
        this.ToggleDisabled(this.Meta?.DisabledExp);
        if (dirty !== undefined) {
            this._setDirty = dirty;
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
        // @ts-ignore
        this.EditForm?.NotificationClient?.Send(JSON.stringify(param));
        if (action == "Subscribe")
            // @ts-ignore
            window.addEventListener(queueName, this.QueueHandler);
        else
            // @ts-ignore
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
            // @ts-ignore
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
     * @returns {EditableComponent[] | null}
     */
    FilterChildren(filter, ignore = null) {
        return this.Children.Flattern(x => x.Children.Where(child => {
            return ignore?.call(this, child) !== true && filter?.call(this, child) === true;
        })).Where(filter);
    }
    /**
     * 
     * @param {(ele: HTMLElement) => boolean} predicate 
     * @returns {EditableComponent[]}
     */
    FindActiveComponent(predicate) {
        const showPredicate = (/** @type {HTMLElement} */ e) => {
            return !e.Hidden() && predicate(e);
        }
        // @ts-ignore
        return this.Children.Where(showPredicate).Flattern(x => showPredicate(x) ? x.Children : null);
    }
    /**
     * Returns the first element of the collection that satisfies the specified condition, or null if no such element is found.
     * @param {(item: EditableComponent) => boolean} filter - The condition to check for each element.
     * @returns {EditableComponent|null} The first element that satisfies the condition, or null if no such element is found.
     */
    FirstOrDefault(filter) {
        return this.Children.Flattern(x => x.Children).FirstOrDefault(filter);
    }

    static TabContainer = document.getElementById("tab-content");
    Popup = false;
    /**
     * 
     * @param {EditableComponent} child 
     * @param {Number} index 
     * @param {(e: EditableComponent) => boolean | string} showExp 
     * @param {(e: EditableComponent) => boolean | string} disabledExp 
     * @returns 
     */
    AddChild(child, index = null, showExp = null, disabledExp = null) {
        // @ts-ignore
        if (child.IsSingleton) {
            // @ts-ignore
            child.Render();
            return;
        }
        if (!child.ParentElement) {
            if (child.Popup) {
                child.ParentElement = this.Element || EditableComponent.TabContainer;
            } else if (!child.Popup) {
                child.ParentElement = EditableComponent.TabContainer;
            } else {
                child.ParentElement = Html.Context;
            }
        }

        if (!child.Entity) {
            child.Entity = this.Entity;
        }

        if (index === null || index >= this.Children.length || index < 0) {
            this.Children.push(child);
        } else {
            this.Children.splice(index, 1, child);
        }

        if (!child.Parent) {
            child.Parent = this;
        }

        Html.Take(child.ParentElement);
        // @ts-ignore
        child.Render();
        // @ts-ignore
        child.ToggleShow(showExp || (child.Meta ? child.Meta.ShowExp : ""));
        child.ToggleDisabled(disabledExp || (child.Meta ? child.Meta.DisabledExp : ""));
    }

    RemoveChild(child) {
        const index = this.Children.indexOf(child);
        if (index > -1) {
            this.Children.splice(index, 1);
        }
    }

    Focus() {
        // @ts-ignore
        this.Element?.focus();
    }

    /**
     * @param {boolean} disabled
     * @param {string[]} name
     */
    SetDisabled(disabled, ...name) {
        if (name == null || name.length == 0) this.Disabled = disabled;
        else {
            this.FilterChildren(x => name.includes(x.FieldName)).forEach(x => x.Disabled = disabled);
        }
    }
}