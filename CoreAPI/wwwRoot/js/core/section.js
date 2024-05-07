import EditableComponent from "./editableComponent.js";
import { Str } from "./utils/ext.js";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { ElementType } from './models/elementType.js'
import { FeaturePolicy } from "./models/featurePolicy.js";
import { ComponentFactory } from "./utils/componentFactory.js";
import EventType from "./models/eventType.js";
import { PatchVM } from "./models/patch.js";
import { Client } from "./clients/client.js";
import { Component } from "./models/component.js";
import { ListView } from "./listView.js";

export class Section extends EditableComponent {
    /**
     * 
     * @param {ElementType | string | null | undefined} eleType - Element type of the section
     * @param {HTMLElement | null} ele 
     */
    constructor(eleType, ele = null) {
        super(null, ele);
        this.elementType = eleType;
        this.Element = ele;
        this.innerEle = null;
        this._chevron = null;
    }

    Render() {
        if (this.elementType == null && this.Element != null) {
            this.elementType = this.Element?.tagName?.toLowerCase();
        } else if (this.ParentElement != null && this.elementType != null) {
            Html.Take(this.ParentElement).Add(this.elementType);
            this.Element = Html.Context;
        } else {
            throw 'Element type must be not null and parent element or element must be supplied'
        }
        if (this.Meta === null) return;
        this.Element.id = this.Meta.Id;
        if (this.Meta.Html) {
            const cssContent = this.Meta.Css;
            const section = (this.Meta.FieldName?.toLowerCase() ?? Str.Empty) + this.Meta.Id;
            if (cssContent) {
                if (!document.head.querySelector("#" + section)) {
                    const style = document.createElement("style");
                    style.id = section;
                    style.appendChild(document.createTextNode(cssContent));
                    document.head.appendChild(style);
                }
            }
            if (this.Element != null) {
                const cellText = Utils.GetHtmlCode(this.Meta.Html, [this.Entity]) ?? Str.Empty;
                this.Element.innerHTML = cellText;
            }
            const allComPolicies = !this.Meta.Id && this.EditForm
                ? this.EditForm.GetElementPolicies(this.Meta.Children.map(x => x.Id).concat([...this.Meta.Id]), Utils.ComponentId)
                : [];
            // @ts-ignore
            this.SplitChild(this.Element.children, allComPolicies, section);
            if (this.Meta.Javascript) {
                try {
                    const fn = new Function(this.Meta.Javascript);
                    const obj = fn.call(null, this.EditForm);
                    for (const prop in obj) {
                        this[prop] = obj[prop].bind(this);
                    }
                } catch (e) {
                    console.log(e.message);
                }
            }
            this.RenderChildrenSection(this.Meta);
            return;
        }
        if (this.Meta.IsDropDown) {
            Html.Take(this.Element).ClassName("dd-wrap").Style("position: relative;").TabIndex(-1).Event(EventType.FocusOut, this.HideDetailIfButtonOnly)
                .Button.ClassName("btn ribbon").IText(this.Meta.Label).Event(EventType.Click, this.DropdownBtnClick)
                .Span.Text("▼").EndOf(ElementType.button).Div.ClassName("dropdown").TabIndex(-1).Render();
            if (this.Meta.IsCollapsible === true) {
                Html.Instance.Style("display: none;");
            }
            this.innerEle = Html.Context;
            this._chevron = this.innerEle?.previousElementSibling?.firstElementChild;
            this.Element = Html.Context;
        }
        if (this.Meta.Responsive && !this.Meta.IsTab || this.Meta.IsDropDown) {
            this.RenderComponentResponsive(this.Meta);
        } else {
            this.RenderComponent(this.Meta);
        }
        this.RenderChildrenSection(this.Meta);
    }

    /**
     * 
     * @param {HTMLElement[]} hTMLElements 
     * @param {FeaturePolicy[]} allComPolicies 
     * @param {string} section 
     * @returns 
     */
    SplitChild(hTMLElements, allComPolicies, section) {
        for (const eleChild of hTMLElements) {
            eleChild.setAttribute(section, "");
            if (eleChild.dataset.name !== undefined) {
                const com = new Section(ElementType.div, null);
                com.Element = eleChild;
                const ui = this.Meta.Children.find(x => x.FieldName === eleChild.dataset.name);
                eleChild.removeAttribute("data-name");
                if (!ui || ui.Hidden) {
                    continue;
                }

                const comPolicies = allComPolicies.filter(x => x.RecordId === ui.Id);
                const readPermission = !ui.IsPrivate || comPolicies.every(x => x.CanRead);
                const writePermission = !ui.IsPrivate || comPolicies.every(x => x.CanWrite);
                if (!readPermission) {
                    continue;
                }

                const component = ComponentFactory.GetComponent(ui, this.EditForm);
                if (component == null) return;
                // @ts-ignore
                if (component instanceof ListView) {
                    // @ts-ignore
                    this.EditForm.ListViews.push(component);
                }
                component.ParentElement = eleChild;
                this.AddChild(component);
                if (component instanceof EditableComponent) {
                    component.Disabled = ui.Disabled || this.Disabled || !writePermission || component.Disabled;
                }
                if (component.Element) {
                    if (ui.ChildStyle) {
                        const current = Html.Context;
                        Html.Take(component.Element).Style(ui.ChildStyle);
                        Html.Take(current);
                    }
                    if (ui.ClassName) {
                        component.Element.classList.add(ui.ClassName);
                    }

                    if (ui.Row === 1) {
                        component.ParentElement?.parentElement?.classList?.add("inline-label");
                    }

                    if (Client.SystemRole) {
                        component.Element.addEventListener(EventType.ContextMenu, (e) => this.EditForm.SysConfigMenu(e, ui, Meta, null));
                    }
                }
                if (ui.Focus) {
                    component.Focus();
                }
            }
            if (eleChild.dataset.click !== undefined) {
                const eventName = eleChild.dataset.click;
                eleChild.removeAttribute("data-click");
                eleChild.addEventListener(EventType.Click, async () => {
                    let method = null;
                    method = this[eventName];
                    let task = null;
                    try {
                        await method.apply(this.EditForm, [this]);
                    } catch (ex) {
                        console.log(ex.message);
                        console.log(ex.stack);
                        throw ex;
                    }
                });
            }
        }
    }

    HandleMeta() {
        if (!this.Meta.Html) {
            return;
        }

        const cssContent = this.Meta.Css;
        const hard = this.Meta.Id;
        const section = `${this.Meta.FieldName.toLowerCase()}${hard}`;

        if (cssContent) {
            const styleId = `${section}-style`;
            if (!document.getElementById(styleId)) {
                const style = document.createElement('style');
                style.id = styleId;
                style.textContent = cssContent.replace(/(?:^|[\s\r\n])\.([a-zA-Z0-9-_]+)/g, (match, p1) => {
                    return `.${section} ${p1}`;
                });
                document.head.appendChild(style);
            }
        }

        this.element.innerHTML = Utils.getHtmlCode(this.Meta.Html, [this.entity]);

        if (this.Meta.Javascript) {
            try {
                const fn = new Function('editForm', this.Meta.javascript);
                fn.call(this, this.editForm);
            } catch (e) {
                console.error('Error executing JavaScript:', e);
            }
        }
    }

    _chevron;
    get Chevron() { return this._chevron; }
    set Chevron(value) { this._chevron = value; }

    /**
     * Renders the dropdown elements and handles their interactions.
     */
    RenderDropDown() {
        const button = document.createElement('button');
        button.className = 'btn ribbon';
        button.textContent = this.Meta.Label;
        button.addEventListener('click', this.DropdownBtnClick.bind(this));

        const chevron = document.createElement('span');
        chevron.textContent = '▼';
        button.appendChild(chevron);

        const dropdown = document.createElement('div');
        dropdown.className = 'dropdown';
        dropdown.style.display = 'none'; // Initially hidden
        dropdown.tabIndex = -1; // Make it focusable

        this.Element.appendChild(button);
        this.Element.appendChild(dropdown);

        this.InnerEle = dropdown;
        this.Chevron = chevron;

        // Add a focus out listener to hide dropdown when focus is lost
        this.Element.addEventListener('focusout', this.HideDetailIfButtonOnly.bind(this));
    }

    /**
     * Handles button click to toggle the visibility of the dropdown.
     */
    DropdownBtnClick() {
        const isVisible = this.InnerEle.style.display !== 'none';
        this.InnerEle.style.display = isVisible ? 'none' : 'block';
        this.Chevron.textContent = isVisible ? '▼' : '▲';
    }

    /**
     * Hides the dropdown if the focus is moved away and only buttons are present.
     */
    HideDetailIfButtonOnly() {
        // This checks if all children are buttons which could be customized based on actual use case
        if (this._isAllBtn === null) {
            this._isAllBtn = Array.from(this.InnerEle.children).every(child => child.tagName === 'BUTTON');
        }

        if (this._isAllBtn) {
            this.InnerEle.style.display = 'none';
            this.Chevron.textContent = '▼';
        }
    }

    /**
     * Renders a section based on the provided editable component and group information.
     * @param {EditableComponent} Parent - The parent component.
     * @param {Component} GroupInfo - The group info component.
     * @param {Object} Entity - Optional entity parameter.
     * @param {EditForm} form - Optional edit form.
     * @returns {Section} - The rendered section, or null if not permitted.
     */
    static RenderSection(Parent, GroupInfo, Entity = null, form = null) {
        const EditForm = form || Parent.EditForm;
        const UIPolicy = form.GetElementPolicies([GroupInfo.Id], Utils.ComponentGroupId);
        const ReadPermission = !GroupInfo.IsPrivate || UIPolicy.HasElementAndAll(x => x.CanRead);
        const WritePermission = !GroupInfo.IsPrivate || UIPolicy.HasElementAndAll(x => x.CanWrite);
        if (!ReadPermission) {
            return null;
        }

        let Width = GroupInfo.Width;
        const OuterColumn = form.GetOuterColumn(GroupInfo);
        const ParentColumn = form.GetInnerColumn(GroupInfo.Parent);
        const HasOuterColumn = OuterColumn > 0 && ParentColumn > 0;
        if (HasOuterColumn) {
            const Per = (OuterColumn / ParentColumn * 100).toFixed(2);
            const Padding = ((GroupInfo.ItemInRow - 1) / GroupInfo.ItemInRow).toFixed(2);
            Width = OuterColumn === ParentColumn ? "100%" : `calc(${Per}% - ${Padding}rem)`;
        }

        Html.Take(Parent.Element).Div.Render();
        if (GroupInfo.Label) {
            Html.Instance.Label.ClassName("header").IText(GroupInfo.Label);
            if (Client.SystemRole) {
                Html.Instance.Attr("contenteditable", "true");
                Html.Instance.Event("input", e => this.ChangeComponentGroupLabel(e, GroupInfo));
                Html.Instance.Event("dblclick", e => form.SectionProperties(GroupInfo));
            }
            Html.Instance.End.Render();
        }
        Html.Instance.ClassName(GroupInfo.ClassName).Event("contextmenu", e => form.SysConfigMenu(e, null, GroupInfo, null));
        if (!GroupInfo.ClassName.includes("ribbon")) {
            Html.Instance.ClassName("panel").ClassName("group");
        }
        Html.Instance.Display(!GroupInfo.Hidden).Style(GroupInfo.Style || "").Width(Width);
        const section = new section(Html.Context);
        section.Id = GroupInfo.FieldName + GroupInfo.Id,
        section.Name = GroupInfo.FieldName;
        section.Meta = GroupInfo;
        section.Disabled = Parent.Disabled || GroupInfo.Disabled || !WritePermission || form.IsLock || section.Disabled;
        Parent.AddChild(section, null, GroupInfo.ShowExp);
        Html.Take(Parent.Element);
        section.DOMContentLoaded?.Invoke();
        return section;
    }


    /**
     * Renders a tab group within a parent editable component.
     * @param {EditableComponent} Parent - The parent component.
     * @param {Component} Group - The group of components to be rendered as tabs.
     */
    static RenderTabGroup(Parent, Group) {
        const Disabled = Parent.Disabled || Group.Disabled;
        if (!Parent.EditForm.TabGroup) {
            Parent.EditForm.TabGroup = [];
        }

        let TabG = Parent.EditForm.TabGroup.find(x => x.Name === Group.TabGroup);
        if (!TabG) {
            TabG = {
                Name: Group.TabGroup,
                Parent: Parent,
                ParentElement: Parent.Element,
                Entity: Parent.Entity,
                Meta: Group,
                EditForm: Parent.EditForm,
                Children: [],
                Disabled: Disabled,
                Render() {
                    // Define render logic for TabGroup
                }
            };
            const SubTab = {
                Parent: TabG,
                Entity: Parent.Entity,
                Meta: Group,
                Name: Group.FieldName,
                EditForm: Parent.EditForm,
                Disabled: Disabled,
                Render() {
                    // Define render logic for SubTab
                },
                RenderTabContent() {
                    // Logic to render tab content
                },
                Focus() {
                    // Logic to focus the tab
                },
                ToggleShow(ShowExp) {
                    // Logic to toggle show based on expression
                },
                ToggleDisabled(DisabledExp) {
                    // Logic to toggle disable based on expression
                }
            };
            TabG.Children.push(SubTab);
            Parent.EditForm.TabGroup.push(TabG);
            Parent.Children.push(TabG);
            TabG.Render();
            SubTab.Render();
            SubTab.RenderTabContent();
            SubTab.Focus();
            SubTab.ToggleShow(Group.ShowExp);
            SubTab.ToggleDisabled(Group.DisabledExp);
        } else {
            const SubTab = {
                Parent: TabG,
                ParentElement: TabG.Element,
                Entity: Parent.Entity,
                Meta: Group,
                Name: Group.FieldName,
                Disabled: Disabled,
                Render() {
                    // Define render logic for SubTab
                },
                ToggleDisabled(DisabledExp) {
                    // Logic to toggle disable based on expression
                }
            };
            TabG.Children.push(SubTab);
            SubTab.Render();
            //SubTab.RenderTabContent();
            SubTab.ToggleDisabled(Group.DisabledExp);
            TabG.Children[0].Focus(); // Assuming FirstChild is at index 0
        }
    }


    /**
     * Renders child components according to metadata.
     * @param {Component} group - The group of components to render.
     */
    RenderChildrenSection(group) {
        if (!group.Children || group.Children.length === 0) {
            return;
        }

        group.Children.sort((a, b) => a.Order - b.Order).forEach(child => {
            if (child.IsTab) {
                Section.RenderTabGroup(this, child);
            } else {
                Section.RenderSection(this, child);
            }
        });
    }

    /**
     * Handles dynamic updates to component labels.
     * @param {Event} event - The event that triggered the label change.
     * @param {Component} component - The component whose label is being changed.
     */
    ChangeLabel(event, component) {
        clearTimeout(this._imeout);
        this._imeout = setTimeout(() => {
            // @ts-ignore
            this.SubmitLabelChanged('Component', component.Id, event?.target?.textContent);
        }, 1000);
    }

    /**
     * @param {string} table
     * @param {any} id
     * @param {any} label
     */
    SubmitLabelChanged(table, id, label) {
        var patch = new PatchVM();
        patch.Table = table;
        patch.Changes = [
            // @ts-ignore
            { Field: this.IdField, Value: id },
            // @ts-ignore
            { Field: 'Label', Value: label },
        ];
        Client.Instance.PatchAsync(patch).then(x => {
            console.log('patch success');
        });
    }

    static _imeout1;

    /**
     * Changes the label of a component group.
     * @static
     * @param {Event} e - The event object.
     * @param {Component} com - The component instance.
     */
    static ChangeComponentGroupLabel(e, com) {
        window.clearTimeout(Section._imeout1);
        Section._imeout1 = window.setTimeout(() => {
            SubmitLabelChanged('Meta', com.id, e.target.textContent.decodeSpecialChar());
        }, 1000);
    }

    /**
     * Renders components responsive to the current view.
     * @param {Component} group - The component group to render.
     */
    RenderComponentResponsive(group) {
        if (!group.Children) {
            return;
        }

        const allComPolicies = this.EditForm.GetElementPolicies(group.Children.map(x => x.Id), Utils.ComponentId);
        const innerCol = this.EditForm.GetInnerColumn(group);

        if (innerCol > 0) {
            Html.Take(this.Element).ClassName('grid').Style(`grid-template-columns: repeat(${innerCol}, 1fr)`);
        }

        let column = 0;
        group.Children.sort((a, b) => a.Order - b.Order).forEach(ui => column = this.RenderCom(ui, column, allComPolicies));
    }

    /**
     * 
     * @param {Component} ui 
     * @param {Number} column
     * @param {FeaturePolicy[]} allComPolicies
     * @returns 
     */
    RenderCom(ui, column, allComPolicies) {
        if (ui.Hidden) {
            return;
        }

        const comPolicies = allComPolicies.filter(x => x.RecordId === ui.Id);
        const readPermission = !ui.IsPrivate || comPolicies.every(x => x.CanRead);
        const writePermission = !ui.IsPrivate || comPolicies.every(x => x.CanWrite);

        if (!readPermission) {
            return;
        }

        Html.Take(this.Element);
        const colSpan = ui.Column || 2;
        ui.Label = ui.Label || '';

        let label = null;
        if (ui.ShowLabel) {
            Html.Div.IText(ui.Label).TextAlign(column === 0 ? 'left' : 'right').Render();
            label = Html.Context;
            Html.End.Render();
        }

        const childCom = ComponentFactory.GetComponent(ui, this.EditForm);
        if (childCom === null) return;

        if (childCom instanceof ListView) {
            this.EditForm.ListViews.push(childCom);
        }
        this.AddChild(childCom);
        if (childCom instanceof EditableComponent) {
            childCom.Disabled = ui.Disabled || this.Disabled || !writePermission || this.EditForm.IsLock || childCom.Disabled;
        }

        if (childCom.Element) {
            if (ui.ChildStyle) {
                const current = Html.Context;
                Html.Take(childCom.Element).Style(ui.ChildStyle);
                Html.Take(current);
            }
            if (ui.ClassName) {
                childComponent.element.classList.add(ui.ClassName);
            }

            if (ui.Row === 1) {
                childComponent.parentElement.parentElement.classList.add('inline-label');
            }

            if (Client.systemRole) {
                childComponent.element.addEventListener('contextmenu', e => EditForm.sysConfigMenu(e, ui, group, childComponent));
            }
        }
        if (ui.Focus) {
            childComponent.focus();
        }

        if (colSpan <= innerCol) {
            if (label && label.nextElementSibling && colSpan !== 2) {
                label.nextElementSibling.style.gridColumn = `${column + 2}/${column + colSpan + 1}`;
            } else if (childComponent.element) {
                childComponent.element.style.gridColumn = `${column + 2}/${column + colSpan + 1}`;
            }
            column += colSpan;
        } else {
            column = 0;
        }
        if (column === innerCol) {
            column = 0;
        }
    }

    /**
 * Renders a component within a group, setting up the necessary HTML structure.
 * @param {Component} group - The component group to render.
 */
    RenderComponent(group) {
        if (!group.Children) {
            return;
        }
        Html.Table.ClassName("ui-layout").TBody.TRow.Render();
        let column = 0;
        const AllComPolicies = EditForm.GetElementPolicies(group.Children.map(x => x.Id), Utils.ComponentId);
        group.Children.sort((a, b) => a.Order - b.Order).forEach(ui => {
            if (ui.Hidden) {
                return;
            }

            const ComPolicies = AllComPolicies.filter(x => x.RecordId === ui.Id);
            const ReadPermission = !ui.IsPrivate || ComPolicies.every(x => x.CanRead);
            const WritePermission = !ui.IsPrivate || ComPolicies.every(x => x.CanWrite);
            if (!ReadPermission) {
                return;
            }

            const ColSpan = ui.Column || 2;
            ui.Label = ui.Label || '';
            if (ui.ShowLabel) {
                Html.TData.Visibility(ui.Visibility).Div.IText(ui.Label)
                    .TextAlign(column === 0 ? 'left' : 'right');
                if (Client.SystemRole) {
                    Html.Attr("contenteditable", "true");
                    Html.Event("input", e => this.ChangeLabel(e, ui));
                    Html.Event("dblclick", e => this.ComponentProperties(ui));
                }
                Html.EndOf("td").TData.Visibility(ui.Visibility).ColSpan(ColSpan - 1).Render();
            } else {
                Html.TData.Visibility(ui.Visibility).ColSpan(ColSpan).ClassName("text-left")
                    .Style("padding-left: 0;").Render();
            }

            if (ui.Style) {
                Html.Style(ui.Style);
            }

            if (ui.Width) {
                Html.Width(ui.Width);
            }
            const childCom = ComponentFactory.GetComponent(ui, this.EditForm);
            if (childCom === null) return;

            if (childCom instanceof ListView) {
                this.EditForm.ListViews.push(childCom);
            }
            this.AddChild(childCom);
            if (childCom instanceof EditableComponent) {
                childCom.Disabled = ui.Disabled || this.Disabled || !WritePermission || EditForm.IsLock || childCom.Disabled;
            }
            if (childCom.Element) {
                if (ui.ChildStyle) {
                    const Current = Html.Context;
                    Html.Take(childCom.Element).Style(ui.ChildStyle);
                    Html.Take(Current);
                }
                if (ui.ClassName) {
                    childCom.Element.classList.add(ui.ClassName);
                }

                if (ui.Row === 1) {
                    childCom.ParentElement.parentElement.classList.add("inline-label");
                }

                if (Client.SystemRole) {
                    childCom.Element.addEventListener("contextmenu", e => this.EditForm.SysConfigMenu(e, ui, group, childCom));
                }
            }
            if (ui.Focus) {
                childCom.Focus();
            }

            Html.EndOf("td");
            if (ui.Offset != null && ui.Offset > 0) {
                Html.TData.ColSpan(ui.Offset).End.Render();
                column += ui.Offset;
            }
            column += ColSpan;
            if (column === this.EditForm.GetInnerColumn(group)) {
                column = 0;
                Html.EndOf("tr").TRow.Render();
            }
        });
    }
}

export class ListViewSection extends Section {
    /** @type {ListView} */
    ListView;
    Render() {
        // @ts-ignore
        this.ListView = this.Parent;
        super.Render();
    }
}