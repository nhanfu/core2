import { EditForm } from "../editForm.js";
import EditableComponent from "../editableComponent.js";
import { Component } from "../models/component.js";
import { Utils } from "./utils.js";

/**
 * Factory class for creating UI components based on specific configurations.
 */
export class ComponentFactory {
    /**
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
        switch (ui.ComponentType) {
            case 'GridView':
                childComponent = !ui.GroupBy ? new GridView(ui) : new GroupGridView(ui);
                break;
            case 'ListView':
                childComponent = !ui.GroupBy ? new ListView(ui) : new GroupListView(ui);
                break;
            default:
                const fullName = ui.ComponentType;
                const args = "a, b";
                const body = `return new ${fullName}(a, b)`;
                const typeConstructor = new Function(args, body);
                childComponent = typeConstructor.call(null, ui, ele);
                break;
        }
        childComponent[Utils.IdField] = `${ui.FieldName}${ui.Id.toString()}`;
        childComponent.FieldName = ui.FieldName;
        childComponent.ComponentType = ui.ComponentType;
        childComponent.EditForm = form;
        return childComponent;
    }
}
