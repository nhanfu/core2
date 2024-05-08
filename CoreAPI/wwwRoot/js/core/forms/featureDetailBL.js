import { Component } from "../models/component.js";
import { TabEditor } from "../tabEditor.js";
import { ComponentBL } from "./componentBL.js";

export class FeatureDetailBL extends TabEditor {
    /**
     * Initializes a new instance of the FeatureDetailBL class.
     */
    constructor() {
        super("Feature");  // Assuming the base class needs the name of the model
        this.Name = "FeatureEditor";
        this.Title = "Feature";
        this.PopulateDirty = false;
        this.Entity = new Component();
    }

    /**
     * Gets the Feature entity as a property, safely checking its existence.
     * @returns {Component} The feature entity if it exists.
     */
    get FeatureEntity() {
        return this.Entity instanceof Component ? this.Entity : null;
    }

    /**
     * Edits a grid column by initializing a component business logic handler.
     * @param {Object} arg - The component expected to be a grid column header.
     */
    EditGridColumn(arg) {
        if (arg instanceof Component) {
            const editor = new ComponentBL();
            editor.Entity = arg;
            editor.ParentElement = this.Element  // Assuming TabEditor has an 'Element' property
            this.TabEditor?.AddChild(editor);
        } else {
            throw new Error("Argument must be an instance of Component");
        }
    }
}
