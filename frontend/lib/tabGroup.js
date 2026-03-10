import { EditableComponent } from "./editableComponent.js";
import { Section } from "./section.js";
import { Html } from "./utils/html.js";

export class TabGroup extends EditableComponent {
    ListViewType;
    Ul;
    TabContent;
    ShouldCountBage;
    HasRendered;
    TabGroupElement;
    constructor(ui, ele = null) {
        super(ui);
        this.ListViewType = ["ListView", "GroupListView", "GridView", "GroupGridView"];
        /** @type {HTMLUListElement} */
        this.Ul = null;
        /** @type {HTMLDivElement} */
        this.TabContent = null;
        this.ShouldCountBage = false;
        this.HasRendered = false;
        this.TabGroup = true;
    }

    Render() {
        Html.take(this.ParentElement).div.className("tab-group")
            .className("tab-horizontal");
        this.TabGroupElement = Html.Context;
        Html.Instance.div.className("headers-wrapper").ul.className("nav-config  nav nav-tabs nav-tabs-bottom mb-0");
        this.Ul = Html.Context;
        this.Element = this.Ul.parentElement;
        Html.Instance.end.end.render();
        if (this.EditForm.ButtonFrozen != null && !this.EditForm.IsLoadButtonFrozen) {
            Section.RenderGroupContent(this.Parent, this.EditForm.ButtonFrozen, this.EditForm.width);
            this.EditForm.IsLoadButtonFrozen = true;
        }
        Html.Instance.div.className("tabs-content");
        this.TabContent = Html.Context;
    }
}