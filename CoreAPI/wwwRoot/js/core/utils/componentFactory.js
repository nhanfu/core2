import { Component } from "../models/component.js";
import { Utils } from "./utils.js";

/**
 * Factory class for creating UI components based on specific configurations.
 */
export class ComponentFactory {
    /**
     * @typedef {import('../editableComponent.js').default} EditableComponent
     * @typedef {import('../editForm.js').EditForm} EditForm
     * Creates a component based on the UI configuration and edit form context.
     * @param {Component} ui - The UI configuration for the component.
     * @param {EditForm} form - The form in which the component will be used.
     * @param {HTMLElement} [ele=null] - Optional HTML element to associate with the component.
     * @returns {EditableComponent} The created component instance or null if the type is not specified.
     */
    static GetComponent(ui, form, ele = null) {
        if (ui === null) {
            throw new Error('ui is required');
        }

        if (!ui.ComponentType) {
            return null;
        }

        ui.ComponentType = ui.ComponentType.trim();
        /** @type {EditableComponent} */
        let childComponent;
        const fullName = ui.ComponentType;
        const args = "a, b";
        const body = `return new ${fullName}(a, b)`;
        const typeConstructor = new Function(args, body);
        childComponent = typeConstructor.call(null, ui, ele);
        childComponent[Utils.IdField] = `${ui.FieldName}${ui.Id.toString()}`;
        childComponent.Name = ui.FieldName;
        childComponent.ComponentType = ui.ComponentType;
        childComponent.EditForm = form;
        return childComponent;
    }
}
