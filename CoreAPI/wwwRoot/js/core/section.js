import EditableComponent from "./editableComponent.js";
import { string } from "./utils/ext.js";
import { Html } from "./utils/html";
import { Utils } from "./utils/utils.js";
import { ElementType } from './models/elementType.js'
import { FeaturePolicy } from "./models/featurePolicy.js";
import { ComponentFactory } from "./utils/componentFactory.js";
import EventType from "./models/eventType.js";
import { PatchDetail, PatchVM } from "./models/patch.js";
import { Client } from "./clients/client.js";

export class Section extends EditableComponent {
    /**
     * 
     * @param {string} eleType - Element type of the section
     * @param {HTMLElement} ele 
     */
    constructor(eleType, ele) {
        this.elementType = eleType;
        this.Element = ele;
        this.innerEle = null;
        this._chevron = null;
    }

    Render() {
        if (this.elementType === null) {
            this.elementType = this.Element.tagName.toLowerCase();
        } else {
            Html.Take(this.ParentElement).Add(this.elementType);
            this.Element = Html.Context;
        }
        this.Element.id = this.Id;
        if (this.Meta === null) {
            return;
        }
        if (this.Meta.Html.trim() !== "") {
            const cssContent = this.Meta.Css;
            const hard = this.Meta.Id;
            const section = (this.Meta.FieldName.toLowerCase() + hard).toLowerCase();
            if (cssContent.trim() !== "") {
                if (!document.head.querySelector("#" + section)) {
                    const style = document.createElement("style");
                    style.id = section;
                    style.appendChild(document.createTextNode(cssContent));
                    document.head.appendChild(style);
                }
            }
            const cellText = Utils.GetHtmlCode(this.Meta.Html, [this.Entity]);
            this.Element.innerHTML = cellText;
            const allComPolicies = this.Meta.Id.trim() === string.Empty
                ? this.EditForm.GetElementPolicies(this.Meta.Children.map(x => x.Id), Utils.ComponentId)
                : [];
            this.SplitChild(this.Element.children, allComPolicies, section);
            if (this.Meta.Javascript.trim() !== string.Empty) {
                try {
                    const fn = new Function(this.Meta.Javascript);
                    const obj = fn.call(null, EditForm);
                    for (const prop in obj) {
                        this[prop] = obj[prop].bind(this);
                    }
                    if (this.useEffect !== null) {
                        this.useEffect();
                    }
                } catch (e) {
                    console.log(e.message);
                }
            }
            this.RenderChildrenSection(this.Meta);
            return;
        }
        if (this.Meta.IsDropDown) {
            Html.Take(this.Element).ClassName("dd-wrap").Style("position: relative;").TabIndex(-1).Event(EventType.FocusOut, hideDetailIfButtonOnly)
                .Button.ClassName("btn ribbon").IText(this.Meta.Label).Event(EventType.Click, dropdownBtnClick)
                .Span.Text("▼").EndOf(ElementType.button).Div.ClassName("dropdown").TabIndex(-1).Render();
            if (this.Meta.IsCollapsible === true) {
                Html.Instance.Style("display: none;");
            }
            this.innerEle = Html.Context;
            this._chevron = this.innerEle.previousElementSibling.firstElementChild;
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
     * @param {Section} section 
     * @returns 
     */
    SplitChild(hTMLElements, allComPolicies, section) {
        for (const eleChild of hTMLElements) {
            eleChild.setAttribute(section, "");
            if (eleChild.dataset.name !== undefined) {
                const com = new Section(ElementType.div);
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

                const component = ComponentFactory.GetComponent(ui, EditForm);
                if (!component) return;
                const childComponent = component instanceof EditableComponent ? component : null;
                if (childComponent instanceof ListView) {
                    this.EditForm.ListViews.push(childComponent);
                }
                childComponent.ParentElement = eleChild;
                this.AddChild(childComponent);
                if (childComponent instanceof EditableComponent) {
                    childComponent.Disabled = ui.Disabled || Disabled || !writePermission || EditForm.IsLock || childComponent.Disabled;
                }
                if (childComponent.Element) {
                    if (ui.ChildStyle) {
                        const current = Html.Context;
                        Html.Take(childComponent.Element).Style(ui.ChildStyle);
                        Html.Take(current);
                    }
                    if (ui.ClassName) {
                        childComponent.Element.classList.add(ui.ClassName);
                    }

                    if (ui.Row === 1) {
                        childComponent.ParentElement.parentElement.classList.add("inline-label");
                    }

                    if (Client.SystemRole) {
                        childComponent.Element.addEventListener(EventType.ContextMenu, (e) => this.EditForm.SysConfigMenu(e, ui, Meta, null));
                    }
                }
                if (ui.Focus) {
                    childComponent.Focus();
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
     * Applies styles and classes to child components based on their configuration.
     * @param {EditableComponent} component - The component to style.
     * @param {Component} ui - The UI configuration object for the component.
     */
    ApplyStyles(component, ui) {
        if (ui.ChildStyle) {
            component.Element.style.cssText += ui.ChildStyle;
        }
        if (ui.ClassName) {
            component.Element.classList.add(ui.ClassName);
        }

        if (ui.Row === 1) {
            component.ParentElement.parentElement.classList.add('inline-label');
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

        group.Children.sort((a, b) => a.order - b.order).forEach(child => {
            if (child.isTab) {
                Section.RenderTabGroup(this, child);
            } else {
                Section.RenderSection(this, child);
            }
        });
    }

    /**
     * Updates the display and enable state of the section based on expressions.
     * @param {boolean} force - Force the update even if conditions haven't changed.
     * @param {boolean} [dirty] - Flag indicating if the update is due to a dirty state.
     */
    PrepareUpdateView(force, dirty = false) {
        if (force || dirty) {
            this.ToggleShow(this.Meta?.ShowExp);
            this.ToggleDisabled(this.Meta?.DisabledExp);
        }
    }

    /**
     * Toggles the display of the section based on a given expression.
     * @param {Function} showExp - A function returning a boolean to indicate visibility.
     */
    ToggleShow(showExp) {
        if (showExp && typeof showExp === 'function') {
            this.Element.style.display = showExp() ? '' : 'none';
        }
    }

    /**
     * Toggles the enabled state of the section based on a given expression.
     * @param {Function} disabledExp - A function returning a boolean to indicate disabled state.
     */
    ToggleDisabled(disabledExp) {
        if (disabledExp && typeof disabledExp === 'function') {
            this.Element.disabled = disabledExp();
        }
    }

    /**
     * Handles dynamic updates to component labels.
     * @param {Event} event - The event that triggered the label change.
     * @param {Component} component - The component whose label is being changed.
     */
    ChangeLabel(event, component) {
        clearTimeout(this._imeout);
        this._imeout = setTimeout(() => {
            this.SubmitLabelChanged('Component', component.Id, event.target.textContent);
        }, 1000);
    }

    SubmitLabelChanged(table, id, label) {
        var patch = new PatchVM();
        patch.Table = table;
        patch.Changes = [
            { Field: this.IdField, Value: id },
            { Field: 'Label', Value: label },
        ];
        Client.Instance.PatchAsync(patch).Done();
    }



}
