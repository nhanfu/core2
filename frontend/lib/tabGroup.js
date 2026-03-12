import { EditableComponent } from "./editableComponent.js";
import { Section } from "./section.js";
import { Html } from "./utils/html.js";

export class TabGroup extends EditableComponent {
    listViewType;
    Ul;
    tabContent;
    shouldCountBage;
    hasRendered;
    tabGroupElement;
    constructor(ui, ele = null) {
        super(ui);
        this.listViewType = ["ListView", "GroupListView", "GridView", "GroupGridView"];
        /** @type {hTMLUListElement} */
        this.Ul = null;
        /** @type {hTMLDivElement} */
        this.tabContent = null;
        this.shouldCountBage = false;
        this.hasRendered = false;
        this.tabGroup = true;
    }

    Render() {
        Html.take(this.parentElement).div.className("tab-group")
            .className("tab-horizontal");
        this.tabGroupElement = Html.context;
        Html.instance.div.className("headers-wrapper").ul.className("nav-config  nav nav-tabs nav-tabs-bottom mb-0");
        this.Ul = Html.context;
        this.element = this.Ul.parentElement;
        Html.instance.end.end.render();
        if (this.editForm.buttonFrozen != null && !this.editForm.isLoadButtonFrozen) {
            Section.renderGroupContent(this.Parent, this.editForm.buttonFrozen, this.editForm.width);
            this.editForm.isLoadButtonFrozen = true;
        }
        Html.instance.div.className("tabs-content");
        this.tabContent = Html.context;
    }
}