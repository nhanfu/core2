import EditableComponent from "./editableComponent";
import { Section } from "./section";
import { Html } from "./utils/html";
import { Utils } from "./utils/utils";

export class TabGroup extends EditableComponent {
    constructor() {
        super(null);
        this.ListViewType = ["ListView", "GroupListView", "GridView", "GroupGridView"];
        /** @type {HTMLUListElement} */
        this.Ul = null;
        /** @type {HTMLDivElement} */
        this.TabContent = null;
        this.ShouldCountBage = false;
        this.HasRendered = false;
    }

    Render() {
        Html.Take(this.ParentElement).Div.ClassName("tab-group")
            .ClassName(this.Meta.IsVertialTab ? "tab-vertical" : "tab-horizontal")
            .Div.ClassName("headers-wrapper").Ul.ClassName("nav-config nav nav-tabs nav-tabs-bottom mb-0");
        this.Ul = Html.Context instanceof HTMLUListElement && Html.Context 
        this.Element = this.Ul.parentElement;
        Html.Instance.End.End.Div.ClassName("tabs-content");
        this.TabContent = Html.Context instanceof HTMLDivElement && Html.Context;
        this.ShouldCountBage = this.Meta.Children.every(x => x.Children.HasElement() && this.ListViewType.includes(x.Children[0].ComponentType));
    }
}

export class TabComponent extends Section {
    static ActiveClass = "active";

    constructor(group) {
        super(null);
        this.Meta = group;
        this.Name = group.FieldName;
        this._li = null;
        this.HasRendered = false;
        this._badge = "";
        this.BadgeElement = null;
        this._displayBadge = false;
    }

    get NextTab() {
        const count = this.Parent.Children.length;
        const thisIndex = this.Parent.Children.indexOf(this);
        return thisIndex === count - 1 ? this.Parent.Children[0] : this.Parent.Children[thisIndex + 1];
    }

    get Badge() {
        return this._badge;
    }

    set Badge(value) {
        this._badge = value;
        if (this.BadgeElement) {
            this.BadgeElement.textContent = value;
        }
    }

    get DisplayBadge() {
        return this._displayBadge;
    }

    set DisplayBadge(value) {
        this._displayBadge = value;
        if (value) {
            this.BadgeElement.style.display = 'block';
        } else {
            this.BadgeElement.style.display = 'none';
        }
    }

    Render() {
        const policies = this.EditForm.GetElementPolicies([this.Meta.Id], Utils.ComponentGroupId);
        const readPermission = !this.Meta.IsPrivate || policies.every(x => x.CanRead);
        if (!readPermission) {
            return;
        }
        const parentTabGroup = this.Parent instanceof TabGroup ? this.Parent : null;
        if (parentTabGroup) {
        Html.Take(parentTabGroup.Ul).Li
            .A.ClassName("nav-link tab-default")
            .I.ClassName(this.Meta.Icon || "").End
            .IText(this.Meta.Label || this.Meta.FieldName)
            .Span.ClassName("ml-1 badge badge-warning");
        this.BadgeElement = Html.Context;
        if (this.DisplayBadge) {
            Html.Instance.Text(this.Badge || "");
        } else {
            this.BadgeElement.style.display = 'none';
        }

        this._li = Html.Context.parentElement.parentElement;
        Html.Instance.End.Div.ClassName("desc").IText(this.Meta.Description || "").End.Render();
        this._li.addEventListener("click", () => {
            if (this.HasRendered) {
                this.Focus();
            } else {
                this.Focus();
                this.RenderTabContent();
            }
        });
    }
    }

    RenderTabContent() {
        const parentTabGroup = this.Parent instanceof TabGroup ? this.Parent : null;
        Html.Take(parentTabGroup.TabContent).Div.ClassName("tab-content");
        this.Element = Html.Context;
        const editForm = this.FindClosest(x => x.IsEditForm);
        Section.RenderSection(this, this.Meta);
        this.HasRendered = true;
        this.DispatchEvent(this.Meta.Events, "DOMContentLoaded", this.Entity).Done();
    }

    Focus() {
        const parentTabGroup = this.Parent instanceof TabGroup ? this.Parent : null;
        parentTabGroup.Children
            .filter(x => x !== this)
            .forEach(x => x.Show = false);
        this.Show = true;
        this.EditForm.ResizeListView();
        this.DispatchEvent(this.Meta.Events, "FocusIn", this.Entity).Done();
    }

    get Show() {
        return super.Show;
    }

    set Show(value) {
        if (!this._li) {
            return;
        }

        super.Show = value;
        if (value) {
            this._li.classList.add(TabComponent.ActiveClass);
            this._li.querySelector("a").classList.add(TabComponent.ActiveClass);
            this.DispatchEvent(this.Meta.Events, "FocusIn", this.Entity).Done();
        } else {
            this._li.classList.remove(TabComponent.ActiveClass);
            this._li.querySelector("a").classList.remove(TabComponent.ActiveClass);
            this.DispatchEvent(this.Meta.Events, "FocusOut", this.Entity).Done();
        }
    }

    get Disabled() {
        return super.Disabled;
    }

    set Disabled(value) {
        if (!this._li) {
            return;
        }

        if (value) {
            this._li.setAttribute("disabled", "");
        } else {
            this._li.removeAttribute("disabled");
        }
    }

    get Hidden() {
        return super.Disabled;
    }

    set Hidden(value) {
        this._li.hidden = value;
        if (this.Show) {
            this.Show = false;
        }
        if(this.NextTab instanceof TabComponent) {
            if (this.NextTab.HasRendered) {
                this.NextTab.Focus();
            } else {
                this.NextTab.RenderTabContent();
                this.NextTab.Focus();
            }
        }
    }
}
