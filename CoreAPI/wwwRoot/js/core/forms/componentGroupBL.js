import { EditForm } from "../editForm.js";
import { Component } from "../models/component.js";

/**
* ComponentBL class
* @class
* @extends EditForm
*/
export class ComponentGroupBL extends EditForm {
    /**
    * Constructor
    */
    constructor() {
        super('Component');
        this.Name = "ComponentEditor";
        this.Title = "Component properties";
        this.Icon = "fa fa-wrench";
        this.Id = "EditComponent_" + this.Id;
        this.Entity = new Component();
        this.PopulateDirty = false;
        this.DOMContentLoaded += this.AlterPosition.bind(this);
    }

    /**
    * Alter position
    */
    AlterPosition() {
        this.Element.parentElement.ToggleClass("properties");
    }
}
