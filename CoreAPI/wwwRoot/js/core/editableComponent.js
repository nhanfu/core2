import { Action } from "./action.js";
import { Utils, GetPropValue, SetPropValue, isNoU, Nothing, HasElement, IsNullOrWhiteSpace } from "./utils.js";

/**
 * Represents an editable component in the application.
 * @class
 * The `EditableComponent` class provides functionality for managing and interacting with editable components in the application.
 * It handles the parent-child relationships, event handling, and disposal of the component.
 */
export default class EditableComponent {
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
    /** @type {any} Entity - The entity associated with this editable component. */
    Entity;
    /** @type {any} Meta - Represent meta data of the component. */
    Meta;
    DefaultValue;
    /** @type {string[]} Classes - Represent hierarchy class of the component instance. */
    Classes = [];
    /** @type {boolean} emptyRow - True if the component is in empty row or screen, otherwise false. */
    #emptyRow;
    /** @type {Action} Disposed - Handle after dispose event. */
    Disposed = new Action();
    /** @type {Action} Disposed - Handle DOM Content loaded event. */
    DOMContentLoaded;
    /** @type {Action} Handle toggle event. */
    OnToggle = new Action();
    /**
     * Create instance of component
     * @param {any} meta 
     * @param {HTMLElement} ele 
     */
    constructor(meta, ele) {
        this.Meta = meta;
        this.Element = ele;
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
        if (isNoU(this.#emptyRow)) {
            this.#emptyRow = this.FindClosest('ListViewItem')?.EmptyRow;
        }
        return this.#emptyRow;
    }
    set EmptyRow(val) {
        this.#emptyRow = val;
        this.FindClosest('ListViewItem').EmptyRow = val;
    }
    /** @type {string} QueueName - Return meta data queue name. */
    get QueueName() {
        return this.Meta?.QueueName;
    }
    SetDefaultVal() {
        if (this.Entity == null || this.EntityId == null) return;
        var fn = {};
        if (Utils.IsFunction(this.Meta.DefaultVal, fn)) {
            fn.call(this, this);
        }
        else if (GetPropValue(this.Entity, this.FieldName) == null) {
            SetPropValue(this.Entity, this.FieldName, this.DefaultValue);
        }
    }
    /**
     * 
     * @param {string} type - Class type name
     * @returns {EditableComponent} Returns the closeset EditableComponent of the specified type
     */
    FindClosest(type) {
        let found = this;
        while (found != null) {
            if (found.Classes?.includes(type) || found.Meta?.ComponentType === type) return found;
            found = found.Parent;
        }
    }
    ToggleShow() { }
    ToggleDisabled() { }
    Dispose() {
        this.SendQueueAction("Unsubscribe");
        this.DisposeChildren();
        this.RemoveDOM();
        this.Children = null;
        this.DOMContentLoaded = null;
        this.OnToggle = null;
        if (this.Parent != null && this.Parent.Children != null
            && this.Parent.Children.ToArray().HasElement()
            && this.Parent.Children.ToArray().Contains(this))
        {
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
        if (IsNullOrWhiteSpace(queueName)) return;
        const param = { QueueName: queueName, Action: action };
        this.EditForm.NotificationClient.Send(JSON.stringify(param));
        if (action == "Subscribe")
            window.addEventListener(queueName, this.QueueHandler);
        else
            window.removeEventListener(queueName, this.QueueHandler);
    }
    DisposeChildren() {
        if (this.Children.Nothing()) return;
        /** @type {EditableComponent[]} */
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
}