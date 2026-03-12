import { TabEditor } from "../tabEditor.js";

export class HeaderManageBL extends TabEditor {
    /**
     * @type {import("../models/component.js").Component}
     */
    featureComponent;
    /**
     * Creates an instance of HeaderManageBL with predefined settings.
     */
    constructor() {
        super('Component');
        this.Name = "Header Manage";
        this.Title = "Header Manage Editor";
    }
}
