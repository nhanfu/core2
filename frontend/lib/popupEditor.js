import { TabEditor } from "./tabEditor.js";

export class PopupEditor extends TabEditor {
    constructor(entity) {
        super(entity);
        this.Popup = true;
        this.isTab = false;
        this.parentElement = this.tabEditor ? this.tabEditor.element : null;
        this.shouldLoadEntity = false;
    }
}