import { EditableComponent } from "./editableComponent.js";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { ElementType } from './models/elementType.js'
import { Component, PatchVM, EventType, FeaturePolicy } from "./models/";
import { ComponentFactory } from "./utils/componentFactory.js";
import { Client } from "./clients/client.js";
import { TabComponent } from "./tabComponent.js";
import { TabGroup } from "./tabGroup.js";
import Sortable, { Swap } from "sortablejs";
import { EditForm } from "./index.js";
export class Section extends EditableComponent {
    /**
     * 
     * @param {ElementType | string | null | undefined} eleType - Element type of the section
     * @param {HTMLElement | null} ele 
     */
    constructor(eleType, ele = null) {
        super(null, ele);
        this.elementType = eleType;
        this.Children = [];
        this.element = ele;
        this.innerEle = null;
        this._chevron = null;
    }

    render() {
        if (this.elementType == null) {
            this.elementType = this.element?.tagName?.toLowerCase();
        } else {
            Html.take(this.parentElement).add(this.elementType.tagName == null ? this.elementType : this.elementType?.tagName?.toLowerCase());
            this.element = Html.context;
        }
        if (this.meta === null) {
            return;
        }
        if (this.meta.className?.includes("ribbon") || this.meta.className?.includes("title")) {
            this.renderComponent2(this.meta);
        }
        else {
            this.renderComponent(this.meta);
        }
        this.renderChildrenSection(this.meta);
    }

    updateSection() {
        var parentElemet = this.element.parentElement;
        this.dispose();
        this.Children = [];
        this.updateGroupContent(parentElemet);
    }

    handleMeta() {
        if (!this.meta.html) {
            return;
        }

        const cssContent = this.meta.css;
        const hard = this.meta.id;
        const section = `${this.meta.fieldName.toLowerCase()}${hard}`;

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

        this.element.innerHTML = Utils.getHtmlCode(this.meta.html, [this.entity]);

        if (this.meta.javascript) {
            try {
                const fn = new Function('editForm', this.meta.javascript);
                fn.call(this, this.editForm);
            } catch (e) {
                console.error('Error executing javaScript:', e);
            }
        }
    }

    _chevron;
    /**
     * @type {HTMLElement}
     */
    get Chevron() { return this._chevron; }
    set Chevron(value) { this._chevron = value; }

    /**
     * Renders the dropdown elements and handles their interactions.
     */
    renderDropDown() {
        const button = document.createElement('button');
        button.className = 'btn ribbon';
        button.textContent = this.meta.label;
        button.addEventListener('click', this.dropdownBtnClick.bind(this));

        const chevron = document.createElement('span');
        chevron.textContent = '▼';
        button.appendChild(chevron);

        const dropdown = document.createElement('div');
        dropdown.className = 'dropdown';
        dropdown.style.display = 'none'; // Initially hidden
        dropdown.tabIndex = -1; // Make it focusable

        this.element.appendChild(button);
        this.element.appendChild(dropdown);

        this.innerEle = dropdown;
        this.Chevron = chevron;

        // Add a focus out listener to hide dropdown when focus is lost
        this.element.addEventListener('focusout', this.hideDetailIfButtonOnly.bind(this));
    }

    /**
     * Handles button click to toggle the visibility of the dropdown.
     */
    dropdownBtnClick() {
        const isVisible = this.innerEle.style.display !== 'none';
        this.innerEle.style.display = isVisible ? 'none' : 'block';
        this.Chevron.textContent = isVisible ? '▼' : '▲';
    }

    /**
     * Hides the dropdown if the focus is moved away and only buttons are present.
     */
    hideDetailIfButtonOnly() {
        // This checks if all children are buttons which could be customized based on actual use case
        if (this._isAllBtn === null) {
            this._isAllBtn = Array.from(this.innerEle.children).every(child => child.tagName === 'BUTTON');
        }

        if (this._isAllBtn) {
            this.innerEle.style.display = 'none';
            this.Chevron.textContent = '▼';
        }
    }

    static hasElementAndAll(source, predicate) {
        if (source === null || source.length === 0) {
            return false;
        }

        return source.every(predicate);
    }

    /**
     * Renders a section based on the provided editable component and group information.
     * @param {EditableComponent} Parent - The parent component.
     * @param {Component} groupInfo - The group info component.
     * @param {Object} Entity - Optional entity parameter.
     * @param {EditForm} form - Optional edit form.
     * @returns {Section} - The rendered section, or null if not permitted.
     */
    Width = "";
    static renderSection(parent, groupInfo, entity = null, form = null) {
        form = form ?? parent.editForm;
        this.Width = groupInfo.Width;
        const outerColumn = form ? form.getOuterColumn(groupInfo) : 12;
        const parentColumn = form ? form.getInnerColumn(groupInfo.Parent) : 12;
        const hasOuterColumn = outerColumn > 0 && parentColumn > 0;
        if (hasOuterColumn) {
            const Per = (outerColumn / parentColumn * 99.9).toFixed(2);
            if (!groupInfo.itemInRow) {
                groupInfo.itemInRow = 2;
            }
            this.Width = outerColumn === parentColumn ? "100%" : `${Per}%`;
        }
        else {
            this.Width = "100%";
        }
        var section = this.renderGroupContent(parent, groupInfo, this.Width, entity, form);
        return section;
    }
    /**@param {EditForm} editForm */
    /**@param {EditForm} parent */
    static renderGroupContent(parent, groupInfo, width, entity, editForm) {
        /**@type {EditForm}*/
        var form = editForm ?? parent.editForm;
        if (groupInfo.className?.includes("ribbon")) {
            if (form.isChild) {
                Html.take(form.btnGroupConfig);
            }
            else {
                Html.take(form.popUpMenu);
            }
        }
        else if (groupInfo.className?.includes("title")) {
            Html.take(form.titleCenterElement);
        }
        else {
            Html.take(parent.element);
        }
        if (groupInfo.isDropDown) {
            Html.instance.details.summary.iText(groupInfo.Label, form.meta.label).end.render();
        }
        else {
            Html.instance.div.render();
        }
        if (!groupInfo.isSimple) {
            Html.instance.event(EventType.contextMenu, (e) => form.sysConfigMenu(e, null, groupInfo, null)).className("section-item card").width(width).div.className(groupInfo.className ?? "");
        }
        if (groupInfo.Label && !groupInfo.isDropDown && !groupInfo.isTab) {
            Html.instance.label.className("header").iText(groupInfo.Label, form.meta.label).end.render();
        }
        if (!groupInfo.className?.includes("ribbon") && !groupInfo.isSimple) {
            Html.instance.className("panel").className("group");
        }

        Html.instance.display(!groupInfo.Hidden).style(groupInfo.Style || "");
        const section = new Section(null, Html.context);
        if (groupInfo.componentType == "Section") {
            section.isSection = true;
            form.childSection.push(section);
        }
        section.editForm = form;
        section.id = groupInfo.fieldName + groupInfo.id;
        section.Name = groupInfo.fieldName;
        section.meta = groupInfo;
        section.disabled = parent.disabled || groupInfo.Disabled;
        // @ts-ignore
        parent.addChild(section, null, groupInfo.showExp, groupInfo.disabledExp);
        Html.take(parent.element);
        section.dOMContentLoaded?.invoke();
        return section;
    }

    updateGroupContent(parentElement) {
        /**@type {EditForm}*/
        var form = this.editForm;
        var groupInfo = this.meta;
        Html.take(parentElement);
        if (!groupInfo.isSimple) {
            Html.instance.event(EventType.contextMenu, (e) => form.sysConfigMenu(e, null, groupInfo, null)).className("section-item card").div.className(groupInfo.className ?? "");
        }
        if (groupInfo.Label && !groupInfo.isDropDown && !groupInfo.isTab) {
            Html.instance.label.className("header").iText(groupInfo.Label, form.meta.label).end.render();
        }
        if (!groupInfo.className?.includes("ribbon") && !groupInfo.isSimple) {
            Html.instance.className("panel").className("group");
        }
        Html.instance.display(!groupInfo.Hidden).style(groupInfo.Style || "");
        const section = new Section(null, Html.context);
        if (groupInfo.componentType == "Section") {
            section.isSection = true;
            this.editForm.childSection.push(section);
        }
        section.editForm = form;
        section.id = groupInfo.fieldName + groupInfo.id;
        section.Name = groupInfo.fieldName;
        section.meta = groupInfo;
        section.disabled = parent.Disabled || groupInfo.Disabled;
        this.parent.addChild(section, null, groupInfo.showExp, groupInfo.disabledExp);
        Html.take(this.parent.element);
        section.dOMContentLoaded?.invoke();
        return section;
    }

    /**
     * Renders a tab group within a parent editable component.
     * @param {EditableComponent} Parent - The parent component.
     * @param {Component} Group - The group of components to be rendered as tabs.
     */
    static renderTabGroup(Parent, Group, entity = null) {
        const disabled = Parent.disabled || Group.Disabled;
        if (!Parent.editForm.tabGroup) {
            Parent.editForm.tabGroup = [];
        }

        var tabG = Parent.editForm.tabGroup.find(x => x.Name === (Group.tabGroup || "Default"));
        if (!tabG) {
            var group = {
                tabGroup: Group.tabGroup,
                Label: Group.Label,
                Order: Group.Order,
                isTab: true,
            };
            tabG = new TabGroup(group);
            tabG.Name = group.tabGroup || "Default",
                tabG.Parent = Parent,
                tabG.parentElement = Parent.element,
                tabG.entity = entity ?? Parent.entity,
                tabG.meta = group,
                tabG.meta.disabledExp = null,
                tabG.meta.showExp = null,
                tabG.editForm = Parent.editForm,
                tabG.Children = [],
                tabG.disabled = disabled;
            var subTab = new TabComponent(Group)
            subTab.Parent = tabG,
                subTab.entity = Parent.entity,
                subTab.meta = Group,
                subTab.Name = Group.fieldName,
                subTab.editForm = Parent.editForm,
                subTab.disabled = disabled;
            // @ts-ignore
            tabG.Children.push(subTab);
            Parent.editForm.tabGroup.push(tabG);
            Parent.Children.push(tabG);
            if (Group.componentType == "Section") {
                subTab.isSection = true;
                tabG.isSection = true;
            }
            tabG.render();
            subTab.render();
            subTab.renderTabContent();
            subTab.focus();
        } else {
            var subTab = new TabComponent(Group)
            subTab.Parent = tabG,
                subTab.parentElement = tabG.element,
                subTab.entity = Parent.entity,
                subTab.meta = Group,
                subTab.Name = Group.Name,
                subTab.editForm = Parent.editForm;
            subTab.disabled = disabled;
            tabG.Children.push(subTab);
            if (Group.componentType == "Section") {
                subTab.isSection = true;
                tabG.isSection = true;
            }
            subTab.render();
        }
    }


    /**
     * Renders child components according to metadata.
     * @param {Component} group - The group of components to render.
     */
    renderChildrenSection(group) {
        if (!group.Children || group.Children.length === 0) {
            return;
        }

        group.Children.sort((a, b) => a.Order - b.Order).forEach(child => {
            if (child.isTab) {
                Section.renderTabGroup(this, child);
            } else {
                Section.renderSection(this, child);
            }
        });
    }

    /**
     * Handles dynamic updates to component labels.
     * @param {Event} event - The event that triggered the label change.
     * @param {Component} component - The component whose label is being changed.
     */
    changeLabel(event, component) {
        clearTimeout(this._imeout);
        this._imeout = setTimeout(() => {
            // @ts-ignore
            this.submitLabelChanged('Component', component.id, event?.target?.textContent);
        }, 1000);
    }

    /**
     * @param {string} table
     * @param {any} id
     * @param {any} label
     */
    submitLabelChanged(table, id, label) {
        var patch = new PatchVM();
        patch.table = table;
        patch.Changes = [
            // @ts-ignore
            { field: this.idField, value: id },
            // @ts-ignore
            { field: 'Label', value: label },
        ];
        Client.instance.patchAsync(patch).then(x => {
            console.log('patch success');
        });
    }

    static submitLabelChanged(table, id, label) {
        var patch = {
            Table: table,
            Changes: [
                { field: "idField", value: id },
                { field: "Component.Label", value: label }
            ]
        };
        // @ts-ignore
        Client.instance.patchAsync(patch).then();
    }

    static _imeout1;

    /**
     * Changes the label of a component group.
     * @static
     * @param {Event} e - The event object.
     * @param {Component} com - The component instance.
     */
    static changeComponentGroupLabel(e, com) {
        window.clearTimeout(Section._imeout1);
        Section._imeout1 = window.setTimeout(() => {
            this.submitLabelChanged('Meta', com.id, e.target instanceof HTMLElement && e.target.textContent);
        }, 1000);
    }

    /**
     * 
     * @param {Component} ui 
     * @param {Number} column
     * @param {FeaturePolicy[]} allComPolicies
     * @returns 
     */
    renderCom(ui, column) {
        if (ui.Hidden) {
            return;
        }
        var innerCol = this.editForm.getInnerColumn(ui);
        if (!ui.canRead) {
            return;
        }

        Html.take(this.element);
        const colSpan = innerCol || 2;
        ui.Label = ui.Label || '';

        let label = null;
        if (ui.showLabel) {
            Html.div.iText(ui.Label, this.editForm.meta.label).textAlign(column === 0 ? 'left' : 'right').render();
            label = Html.context;
            Html.end.render();
        }

        const childCom = ComponentFactory.getComponent(ui, this.editForm);
        if (childCom === null) return;

        if (childCom.isListView) {
            // @ts-ignore
            this.editForm.listViews.push(childCom);
        }
        this.addChild(childCom);
        if (childCom instanceof EditableComponent) {
            childCom.disabled = ui.Disabled || this.disabled || !ui.canWrite || this.editForm.isLock || childCom.disabled;
        }

        if (childCom.element) {
            if (ui.childStyle && ui.componentType != "GridView") {
                const current = Html.context;
                Html.take(childCom.element).style(ui.childStyle);
                Html.take(current);
            }
            if (ui.className) {
                childCom.element.classList.add(ui.className);
            }

            if (ui.Row === 1) {
                childCom.parentElement.parentElement.classList.add('inline-label');
            }
            if (Client.systemRole) {
                childCom.element.addEventListener('contextmenu', e => this.editForm.sysConfigMenu(e, ui, ui, childCom));
            }
            if (Client.bodRole && ui.componentType == "Pdf") {
                childCom.element.addEventListener('contextmenu', e => this.editForm.sysConfigMenu(e, ui, ui, childCom));
            }
        }
        if (ui.Focus) {
            childCom.Focus();
        }

        if (colSpan <= innerCol) {
            if (label && label.nextElementSibling && colSpan !== 2) {
                if (label.nextElementSibling instanceof HTMLElement) {
                    label.nextElementSibling.style.gridColumn = `${column + 2}/${column + colSpan + 1}`;
                }
            } else if (childCom.element) {
                childCom.element.style.gridColumn = `${column + 2}/${column + colSpan + 1}`;
            }
            column += colSpan;
        } else {
            column = 0;
        }
        if (column === innerCol) {
            column = 0;
        }
    }

    async componentProperties(component) {
        const { componentBL } = await import('./forms/componentEditor.js');
        const { EditForm } = await import('./editForm.js');
        // @ts-ignore
        var editor = new componentBL({
            Entity: component,
            parentElement: this.element,
            openFrom: this.findClosest(editForm => editForm instanceof EditForm),
        });
        this.addChild(editor);
    }

    /**
     * @param {boolean} [disabled]
     */
    setDisableUI(disabled) {
        const ele = this.element;
        if (ele == null) {
            return;
        }

        if (disabled) {
            ele.setAttribute("disabled", "disabled");
        }
        else {
            ele.removeAttribute("disabled");
        }
    }
    intAwait = 0;
    /**
     * Renders a component within a group, setting up the necessary HTML structure.
     * @param {Component} group - The component group to render.
     */
    async renderIndex2(group) {
        window.clearTimeout(this.intAwait);
        this.intAwait = window.setTimeout(async () => {
            if (this.Children.length === 0) {
                return;
            }

            var chidlds = this.element.querySelectorAll(".layout-item");
            for (let rowIndex = 0; rowIndex < chidlds.length; rowIndex++) {
                var item = this.Children.find(x => x.element.closest(".layout-item") == chidlds[rowIndex]);
                if (item != null) {
                    item.Meta.Order = rowIndex;
                }
            }
            const columns = this.Children.map(x => x.Meta).map(header => {
                const dirtyPatch = [
                    { field: "Id", value: header.id },
                    { field: "Order", value: header.order },
                    { field: "featureId", value: header.featureId }
                ];
                return {
                    Changes: dirtyPatch,
                    notMessage: true,
                    Table: "Component",
                };
            }).filter(x => x != null);
            await Client.instance.patchAsync2(columns);
            if (this.editForm && this.editForm.devTools) {
                await this.editForm.devTools.loadMeta;
                Html.take(".components").clear();
                this.editForm.renderElements(this.editForm.groupTree, true);
                if (this.editForm.configEditor) {
                    var com = this.Children.find(x => x.Meta.id == this.editForm.configEditor.entity.id);
                    if (com) {
                        this.editForm.configEditor.entity = com.Meta;
                        this.editForm.configEditor.updateView(true, true);
                    }
                }
            }
        }, 1000);
    }
    /**
     * Renders a component within a group, setting up the necessary HTML structure.
     * @param {Component} group - The component group to render.
     */
    async renderIndex(group) {
        window.clearTimeout(this.intAwait);
        this.intAwait = window.setTimeout(async () => {
            if (this.Children.length === 0) {
                return;
            }

            var chidlds = this.element.querySelectorAll(".layout-item");
            for (let rowIndex = 0; rowIndex < chidlds.length; rowIndex++) {
                var item = this.Children.find(x => x.element.closest(".layout-item") == chidlds[rowIndex]);
                if (item != null) {
                    item.Meta.Order = rowIndex;
                }
            }
            const columns = this.Children.map(x => x.Meta).map(header => {
                const dirtyPatch = [
                    { field: "Id", value: header.id },
                    { field: "Order", value: header.order },
                    { field: "featureId", value: header.featureId }
                ];
                return {
                    Changes: dirtyPatch,
                    notMessage: true,
                    Table: "Component",
                };
            }).filter(x => x != null);
            await Client.instance.patchAsync2(columns);
            if (this.editForm && this.editForm.devToolsElement) {
                if (this.editForm.configEditor) {
                    await this.editForm.loadMeta();
                    var com = this.Children.find(x => x.Meta.id == this.editForm.configEditor.entity.id);
                    if (com) {
                        this.editForm.configEditor.entity = com.Meta;
                        this.editForm.configEditor.updateView(true, true);
                    }
                }
            }
        }, 1000);
    }
    /**
     * Renders a component within a group, setting up the necessary HTML structure.
     * @param {Component} group - The component group to render.
     */
    renderComponent(group) {
        if (!group.Components || group.Components.length === 0) {
            return;
        }
        var colgroup = this.editForm.getInnerColumn(group);
        // Create a wrapper div for the layout
        Html.div.className("ui-layout").div.className("ui-row").style(`grid-template-columns: repeat(${colgroup}, 1fr);`).render();
        let column = 0;
        if ((group.Components && group.Components.length > 1) || (group.Components && !group.Components[0].canReadAll)) {
            group.Components = this.editForm.getComPolicies(group.Components);
        }
        var lastElementButtonGroup = [];
        var seft = this;
        if (Client.systemRole && Client.token.tenantCode === "forwardx" && this.Token.userId == "1") {
            if (!group.isConfig) {
                new Sortable(Html.context, {
                    animation: 500,
                    ghostClass: "blue-background-class",
                    handle: ".header-label",
                    swap: false,
                    forceFallback: true,
                    delay: 300,
                    delayOnTouchOnly: true,
                    easing: "cubic-bezier(0.2, 0.8, 0.2, 1)",
                    onStart: function (evt) {
                        let parentGroup = evt.from;
                        parentGroup.querySelectorAll(".layout-item").forEach(item => {
                            item.classList.add("same-group");
                        });
                        evt.item.classList.add("dragging");
                    },
                    group: {
                        name: group.id,
                        pull: true,
                        put: true
                    },
                    onEnd: async function (evt) {
                        let parentGroup = evt.from;
                        parentGroup.querySelectorAll(".layout-item").forEach(item => {
                            item.classList.remove("same-group");
                        });
                        evt.item.classList.remove("dragging");
                        await seft.renderIndex2(Html.context);
                    }
                });
            }
            else {
                new Sortable(Html.context, {
                    animation: 500,
                    ghostClass: "blue-background-class",
                    handle: ".header-label",
                    swap: false,
                    forceFallback: true,
                    delay: 300,
                    delayOnTouchOnly: true,
                    easing: "cubic-bezier(0.2, 0.8, 0.2, 1)",
                    group: {
                        name: group.id,
                        pull: "clone",
                        put: false
                    },
                    onEnd: async function (evt) {
                        var com = seft.editForm.childCom.find(x => x.parentElement.parentElement == evt.item);
                        var sec = seft.editForm.childSection.find(x => x.element == evt.to.parentElement.parentElement);
                        com.meta.componentGroupId = sec.meta.id;
                        com.meta.featureId = sec.meta.featureId;
                        sec.Children.push(com);
                        await sec.renderIndex2(sec.element);
                        var patchModel = seft.editForm.getObjectPatchVM(com.meta, "Component");
                        const rs = await Client.instance.patchAsync(patchModel);
                        com.meta = rs.updatedItem[0];
                        if (seft.editForm.openFrom.configEditor) {
                            seft.editForm.openFrom.configEditor.entity = com.meta;
                            seft.editForm.openFrom.configEditor.updateView(true);
                        }
                        seft.editForm.updateConfig();
                    },
                    sort: false
                });
            }
        }
        group.Components.sort((a, b) => a.Order - b.Order).forEach((ui, index) => {
            if (ui.Hidden) {
                return;
            }
            if (!ui.canRead) {
                return;
            }
            var inner = this.editForm.getInnerColumn(ui);
            const colSpan = inner || 1;
            const rowSpan = ui.rowSpan || 1;
            ui.Label = ui.Label || '';
            Html.div.className("layout-item").style(`grid-column: span ${colSpan};grid-row: span ${rowSpan}`).visibility(ui.Visibility);
            if (ui.showLabel) {
                var required = "";
                if (!Utils.isNullOrWhiteSpace(ui.Validation)) {
                    required = ui.Validation.includes("required") ? " (*)" : "";
                }
                Html.instance.div.className("group-control").style(ui.childStyle)
                    .div.className('header-label');
                if (Client.systemRole) {
                    Html.instance.className("moved");
                }
                Html.instance.iText(ui.Label, this.editForm.meta.label)
                    .span.text(required).end.end.render();
            }
            if (ui.Style && ui.componentType !== "Word") {
                Html.style(ui.Style);
            }
            if (ui.Width) {
                Html.width(ui.Width);
            }
            if (!Utils.isNullOrWhiteSpace(ui.groupFormat) && ["Button", "Pdf", "Excel"].some(x => x == ui.componentType)) {
                if (!lastElementButtonGroup.find(x => x.Com.groupFormat == ui.groupFormat)) {
                    Html.instance.div.className("dropdown-btn")
                        .button.className(ui.className).icon("mr-1 " + ui.Icon).end.iText(ui.groupFormat, this.editForm.meta.label)
                        .end
                        .div.className("dropdown-content dropdown-top");
                    lastElementButtonGroup.push({ Com: ui, Ele: Html.context })
                }
            }
            const childCom = ComponentFactory.getComponent(ui, this.editForm);
            if (!Utils.isNullOrWhiteSpace(ui.groupFormat) && ["Button", "Pdf", "Excel"].some(x => x == ui.componentType)) {
                childCom.parentElement = lastElementButtonGroup.find(x => x.Com.groupFormat == ui.groupFormat).Ele;
            }
            if (childCom === null) return;
            this.addChild(childCom);
            this.editForm.childCom.push(childCom);
            if (childCom) {
                childCom.Disabled = ui.Disabled || ui.Write || childCom.Disabled;
            }
            if (childCom.element) {
                if (ui.childStyle && ui.componentType != "GridView") {
                    const Current = Html.context;
                    Html.take(childCom.element).style(ui.childStyle);
                    Html.take(Current);
                }
                if (Client.systemRole) {
                    Html.take(childCom.element).event(EventType.Click, (e) => {
                        if (this.editForm.devToolsElement) {
                            this.editForm.updateMetaData(childCom.Meta);
                        }
                    })
                }

                if (ui.Row === 1) {
                    childCom.parentElement.parentElement.classList.add("inline-label");
                }

                if (["Input", "Dropdown", "Word", "Number", "Textarea"].some(x => x == ui.componentType)) {
                    if (ui.componentType == "Word") {
                        childCom.element.parentElement.addEventListener("contextmenu", e => this.editForm.sysConfigMenu(e, ui, group, childCom));
                    }
                    else {
                        childCom.element.addEventListener("contextmenu", e => this.editForm.sysConfigMenu(e, ui, group, childCom));
                    }
                }
                else {
                    if (Client.systemRole && ui.componentType != "CodeEditor" || Client.bodRole && ui.componentType == "Pdf") {
                        childCom.element.addEventListener("contextmenu", e => this.editForm.sysConfigMenu(e, ui, group, childCom));
                    }
                }
            }
            if (ui.Focus) {
                childCom.Focus();
            }
            Html.endOf(".layout-item");
            if (ui.Offset != null && ui.Offset > 0) {
                Html.div.className("layout-item").style(`grid-column: span ${ui.Offset}`).end.render();
                column += ui.Offset;
            }
            column += colSpan;
        });
    }

    renderComponent2(group) {
        if (!group.Components || group.Components.length == 0) {
            return;
        }
        Html.table.className("ui-layout").tBody.tRow.render();
        let column = 0;
        group.Components = this.editForm.getComPolicies(group.Components);
        var lastElementButtonGroup = [];
        group.Components.sort((a, b) => a.Order - b.Order).forEach(ui => {
            if (ui.Hidden) {
                return;
            }
            if (!ui.canRead) {
                return;
            }
            var inner = this.editForm.getInnerColumn(ui);
            const colSpan = inner || 1;
            ui.Label = ui.Label || '';
            Html.tData.colSpan(colSpan).visibility(ui.Visibility);
            if (ui.showLabel) {
                Html.instance.div.className("group-control").style(ui.childStyle).div.className('header-label').iText(ui.Label, this.editForm.meta.label).end.render();
            }
            if (ui.Style && ui.componentType != "Word") {
                Html.style(ui.Style);
            }
            if (ui.Width) {
                Html.width(ui.Width);
            }
            if (!Utils.isNullOrWhiteSpace(ui.groupFormat) && ["Button", "Pdf", "Excel", "Email"].some(x => x == ui.componentType)) {
                if (!lastElementButtonGroup.find(x => x.Com.groupFormat == ui.groupFormat)) {
                    Html.instance.div.className("dropdown-btn")
                        .button.className(ui.className).icon("mr-1 " + ui.Icon).end.iText(ui.groupFormat, this.editForm.meta.label)
                        .end
                        .div.className("dropdown-content dropdown-top");
                    lastElementButtonGroup.push({ Com: ui, Ele: Html.context })
                }
            }
            const childCom = ComponentFactory.getComponent(ui, this.editForm);
            if (!Utils.isNullOrWhiteSpace(ui.groupFormat) && ["Button", "Pdf", "Excel", "Email"].some(x => x == ui.componentType)) {
                childCom.parentElement = lastElementButtonGroup.find(x => x.Com.groupFormat == ui.groupFormat).Ele;
            }
            if (childCom === null) return;
            this.addChild(childCom);
            this.editForm.childCom.push(childCom);
            if (childCom) {
                childCom.Disabled = ui.Disabled || ui.Write || childCom.Disabled;
            }
            if (childCom.element) {
                if (ui.childStyle && ui.componentType != "GridView") {
                    const Current = Html.context;
                    Html.take(childCom.element).style(ui.childStyle);
                    Html.take(Current);
                }

                if (ui.Row === 1) {
                    childCom.parentElement.parentElement.classList.add("inline-label");
                }
                if (["Input", "Dropdown", "Word", "Number"].some(x => x == ui.componentType)) {
                    if (ui.componentType == "Word") {
                        childCom.element.parentElement.addEventListener("contextmenu", e => this.editForm.sysConfigMenu(e, ui, group, childCom));
                    }
                    else {
                        childCom.element.addEventListener("contextmenu", e => this.editForm.sysConfigMenu(e, ui, group, childCom));
                    }
                }
                else {
                    if (Client.systemRole && ui.componentType != "CodeEditor" || Client.bodRole && ui.componentType == "Pdf") {
                        childCom.element.addEventListener("contextmenu", e => this.editForm.sysConfigMenu(e, ui, group, childCom));
                    }
                }
            }
            if (ui.Focus) {
                childCom.Focus();
            }

            Html.endOf("td");
            if (ui.Offset != null && ui.Offset > 0) {
                Html.tData.colSpan(ui.Offset).end.render();
                column += ui.Offset;
            }
            column += colSpan;
            if (column === this.editForm.getInnerColumn(group)) {
                column = 0;
                Html.endOf("tr").tRow.render();
            }
        });
    }

    setShow(show, ...field) {
        var childs = this.Children.filter(x => x.isSection && field.includes(x.Meta.fieldName));
        if (childs.length == 0) {
            childs = this.Children.filter(x => x.isSection);
            childs.forEach(item => {
                item.setShow(show, ...field);
            })
        }
        else {
            childs.forEach(item => {
                item.show = show;
            })
        }
    }
}