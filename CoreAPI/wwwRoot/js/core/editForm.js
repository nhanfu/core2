import { Utils } from "./utils/utils.js";
import EditableComponent from "./editableComponent.js";
import { ComponentExt } from './utils/componentExt.js';
import EventType from "./models/eventType.js";
import { Html } from "./utils/html.js";
import { Client } from "./clients/client.js";
import { Str } from "./utils/ext.js";
import { Component } from "./models/component.js";
import { PatchDetail, PatchVM } from "./models/patch.js";
import { Message } from "./utils/message.js";
import { Action } from "./models/action.js";
import { StringBuilder } from "./utils/stringBuilder.js";
import { FeaturePolicy } from "./models/featurePolicy.js";
import { ComponentFactory } from "./utils/componentFactory.js";
import { Toast } from "./toast.js";
import { Section } from "./section.js";

/**
 * Represents an editable form component.
 * @typedef {import('./listView.js').ListView} ListView
 * @typedef {import('./label.js').Label} Label
 * @typedef {import('./button.js').Button} Button
 */
export class EditForm extends EditableComponent {
    /** @type {EditForm} */
    static LayoutForm;
    /** @type {ListView[]} */
    ListViews = [];
    static ExpiredDate = "ExpiredDate";
    static BtnExpired = "btnExpired";
    static BtnSave = "btnSave";
    static BtnSend = "btnSend";
    static BtnApprove = "btnApprove";
    static BtnReject = "btnReject";
    static StatusIdField = "StatusId";
    static BtnCancel = "btnCancel";
    static BtnPrint = "btnPrint";
    static BtnPreview = "btnPreview";
    static SpecialEntryPoint = 'entry';
    Portal = true;
    /** @type {Component[]} */
    _allCom = [];
    get AllCom() {
        if (this._allCom !== null) return this._allCom;
        if (this.LayoutForm === null) {
            this._allCom = this.Feature.Component.slice(); // Assuming Feature.Component is an array
        } else {
            this._allCom = this.Feature.Component.concat(this.LayoutForm.Feature.Component);
        }
        return this._allCom;
    }

    get FeatureName() {
        return this.FieldName || this.Feature?.Name;
    }

    // Standard property getter and setter for Href
    get Href() {
        return this.href;
    }

    set Href(value) {
        this.href = value;
    }


    /**
     * Constructor for EditForm.
     * @param {string} entity - The entity associated with this form.
     */
    constructor(entity) {
        super(null);
        this.urlSearch = new URLSearchParams(window.location.search);
        this.entity = entity;
        if (entity != null) {
            window.addEventListener(EventType.Resize, this.ResizeHandler.bind(this));
        }
    }

    /**
     * Gets patch data from the form entity.
     * @returns {PatchVM} The patch view model.
     */
    GetPatchEntity() {
        const shouldGetAll = this.EntityId == null;
        const details = this.FilterChildren(child => {
            return !(child instanceof Button)
                && (shouldGetAll || child.Dirty) && child.Meta != null
                && child.FieldName != null;
        }, x => x instanceof ListView || x.AlwaysValid || !x.PopulateDirty);
        const patches = details
            .DistinctBy(x => x.Meta.Id)
            .SelectMany(child => {
                if (typeof child['GetPatchDetail'] === 'function') {
                    return child['GetPatchDetail']();
                }
                const value = Utils.GetPropValue(child.Entity, child.FieldName);
                /**
                 * @type {PatchDetail}
                 */
                // @ts-ignore
                const patch = {
                    Label: child.Label,
                    Field: child.FieldName,
                    OldVal: (child.OldValue != null) ? child.OldValue.toString() : child.OldValue?.toString(),
                    Value: (value != null) ? value.toString() : !this.EditForm.Meta.IgnoreEncode ? Utils.EncodeSpecialChar(value?.toString().trim()) : value?.toString().trim(),
                };
                return [patch];
            })
            .DistinctBy(x => x.Field);
        this.AddIdToPatch(details);
        /** @type {PatchVM} */
        // @ts-ignore
        const patchVM = { Changes: patches, Table: this.Meta.EntityName, QueueName: this.QueueName, CacheName: this.CacheName };
        return patchVM;
    }

    /**
     * Validates and saves the patch if the form is dirty.
     * @param {object} entity - The entity to save.
     * @returns {Promise<boolean>} A promise that resolves with the success status.
     */
    async SavePatch(entity = null) {
        if (!this.Dirty) {
            Toast.Warning(Message.NotDirty);
            return false;
        }
        try {
            const valid = await this.IsFormValid();
            if (valid) {
                const success = await this.ValidSavePatch();
                return success;
            } else {
                return false;
            }
        } catch (error) {
            Toast.Warning(error.Message);
            return false;
        }
    }

    /**
     * Handles resizing of form components based on the viewport.
     */
    ResizeHandler() {
        this.ResizeTabGroup();
        this.ResizeListView();
    }

    /**
   * Resizes the ListView based on visibility and responsiveness.
   */
    ResizeListView() {
        const VisibleListView = this.ListViews.find(lv => !lv.Element.Hidden());
        if (!VisibleListView) {
            return;
        }

        const allListView = VisibleListView.Parent.Children.filter(x => x instanceof ListView);
        const responsive = allListView.some(x => x.FieldName.includes("Mobile"));
        allListView.forEach(lv => {
            if (responsive) {
                lv.Show = this.IsSmallUp ? !lv.FieldName.includes("Mobile") : lv.FieldName.includes("Mobile");
                if (lv.Show) {
                    this._CurrentListView = lv;
                }
            } else {
                this._CurrentListView = lv;
            }
        });
    }

    /** @type {EditForm[]} */
    TabGroup = [];
    /**
     * Resizes the TabGroup based on the screen size and responsiveness.
     */
    ResizeTabGroup() {
        if (this.Element && (this.Element.HasClass("mobile") || this.TabGroup.Nothing())) {
            return;
        }

        this.TabGroup.ForEach(tg => {
            if (!tg || !tg.Element) {
                return;
            }

            if (this.IsLargeUp && tg.Meta.Responsive && tg.Element.parentElement.HasClass("tab-horizontal")) {
                tg.Element.parentElement.ReplaceClass("tab-horizontal", "tab-vertical");
            } else if (!this.IsLargeUp && tg.Meta.Responsive && tg.Element.parentElement.HasClass("tab-vertical")) {
                tg.Element.parentElement.ReplaceClass("tab-vertical", "tab-horizontal");
            }
        });
    }

    /**
     * Loads and renders features based on the current entity setup.
     * @param {Function} callback - Optional callback to run after loading and rendering.
     */
    LoadFeatureAndRender(callback = null) {
        const featureTask = this.Meta ? Promise.resolve(this.Meta) : ComponentExt.LoadFeature(this.Meta.Name ?? this.Meta.FieldName);
        this.LoadEntity().then(entity => {
            featureTask.then(feature => {
                this.LayoutLoaded(feature, entity, callback);
            });
        });
    }

    Render() {
        if (this.Portal) {
            this.ParentForm = this.ParentForm || this.LastForm;
        }
        this.LoadFeatureAndRender();
        this.LastForm = this;
    }

    /**
     * Handles the loaded layout and setups the form with loaded features.
     * @param {Component} feature - The loaded feature.
     * @param {object} entity - The entity data.
     * @param {Function} loadedCallback - Callback function to execute after loading.
     */
    LayoutLoaded(feature, entity, loadedCallback = null) {
        this.Entity.CopyPropFrom(entity);
        this.SetCurrentUserProperties();
        this.SetFeatureProperties(feature);
        const groupTree = this.BuildTree(feature.ComponentGroup.sort((a, b) => a.Order - b.Order));
        this.Element = this.RenderTemplate(feature);
        this.SetFeatureStyleSheet(feature.StyleSheet);
        this.RenderTabOrSection(groupTree);
        this.ResizeHandler();
        this.LockUpdate();
        this.InitDOMEvents();
        loadedCallback?.call(null);
        this.DispatchFeatureEvent(feature.Events, EventType.DOMContentLoaded);
    }

    /**
     * Initializes DOM events for the form.
     */
    InitDOMEvents() {
        Html.Take(this.Element).TabIndex(-1).Trigger('focus')
            .Event(EventType.FocusIn, () => this.DispatchFeatureEvent(this.Meta.Events, EventType.FocusIn))
            .Event(EventType.KeyDown, (e) => this.KeyDownIntro(e))
            .Event(EventType.FocusOut, () => this.DispatchFeatureEvent(this.Meta.Events, EventType.FocusOut));
    }

    /**
     * Sets the current user properties from the token.
     */
    SetCurrentUserProperties() {
        const token = Client.Token;
        this.currentUserId = token?.UserId;
        this.regionId = token?.RegionId;
        this.centerIds = token?.CenterIds ? token.CenterIds.join(Str.Comma) : Str.Empty;
        this.roleIds = token?.RoleIds ? token.RoleIds.join(Str.Comma) : Str.Empty;
        this.costCenterId = token?.CostCenterId;
        this.roleNames = token?.RoleNames ? token.RoleNames.join(Str.Comma) : Str.Empty;
    }

    BeforeSaved = new Action();
    AfterSaved = new Action();
    /**
     * Validates the form before saving patches.
     * @returns {Promise<boolean>} A promise that resolves to the validation result.
     */
    async ValidSavePatch() {
        const pathModel = this.GetPatchEntity();
        this.EntityId = pathModel.EntityId;
        const details = this.UpdateIndependantGridView() || [];
        details.unshift(pathModel);
        this.BeforeSaved?.invoke();
        try {
            const rs = await Client.Instance.PatchAsync(details);
            if (rs === 0) {
                Toast.Warning('Your data has not been saved, please try again!');
                this.AfterSaved?.invoke(false);
                return false;
            }
            if (this.Meta.DeleteTemp) {
                this.DeleteGridView();
            }
            Toast.Success('The data was saved');
            this.Dirty = false;
            this.AfterSaved?.invoke(true);
            return true;
        } catch (e) {
            Toast.Warning(e.message);
            return false;
        }
    }

    /**
     * Updates grids that are independent of the main form's entity.
     * @returns {PatchVM[]} List of Patch View Models.
     */
    UpdateIndependantGridView() {
        const dirtyGrid = this.GetDirtyGrid();
        if (!dirtyGrid.length) {
            return null;
        }
        return dirtyGrid.flatMap(grid => grid.GetPatches());
    }

    /**
     * Gets the list of grids that have unsaved changes.
     * @returns {ListView[]} Array of dirty list views.
     */
    GetDirtyGrid() {
        return this.ListViews
            .filter(grid => grid.Meta.Id && grid.Meta.CanAdd)
            .filter(grid => grid.FilterChildren(child => child.Dirty, child => !child.PopulateDirty).length > 0);
    }

    /**
     * Deletes data from temporary grids.
     */
    DeleteGridView() {
        const dirtyGrid = this.GetDeleteGrid();
        dirtyGrid.forEach(grid => {
            Client.Instance.HardDeleteAsync(grid.DeleteTempIds, grid.Meta.RefName, grid.DataConn, grid.MetaConn)
                .then(deleteSuccess => {
                    if (!deleteSuccess) {
                        Toast.Warning('Error deleting details, please check again');
                        return;
                    }
                    grid.RowAction(row => {
                        if (grid.DeleteTempIds.includes(row.EntityId)) {
                            row.Dispose();
                        }
                    });
                    grid.DeleteTempIds.Clear();
                });
        });
    }

    GetDeleteGrid() {
        return this.ListViews
            .filter(grid => grid.Meta.Id)
            .filter(grid => grid.DeleteTempIds.length > 0);
    }

    /**
     * Builds a tree structure from a list of components.
     * @param {Component[]} componentGroup - The list of components to build the tree from.
     * @returns {Component[]} - The root components of the built tree.
     */
    BuildTree(componentGroup) {
        const componentGroupMap = new Map(componentGroup.map(x => [x.Id, x]));
        let parent;

        for (const item of componentGroup) {
            if (item.IsVerticalTab && this.Element.clientWidth < EditableComponent.SmallScreen) {
                item.IsVerticalTab = false;
            }

            if (!item.ParentId) {
                continue;
            }

            if (!componentGroupMap.has(item.ParentId)) {
                console.log(`The parent key ${item.ParentId} of ${item.FieldName} doesn't exist`);
                continue;
            }

            parent = componentGroupMap.get(item.ParentId);

            if (!parent.Children) {
                parent.Children = [];
            }

            if (!parent.Children.includes(item)) {
                parent.Children.push(item);
            }

            item.Parent = parent;
        }

        for (const item of componentGroup) {
            if (!item.Children || !item.Children.length) {
                continue;
            }

            for (const ui of item.Children) {
                ui.ComponentGroup = item;
            }

            if (item.Children) {
                item.Children = item.Children.sort((a, b) => a.Order - b.Order);
            }
        }

        componentGroup.forEach(x => this.CalcItemInRow(x.Children.slice()));
        const res = componentGroup.filter(x => !x.ParentId);

        if (!res.length) {
            console.log("No component group is root component. Wrong feature name or the configuration is wrong");
        }

        return res;
    }

    /**
     * Calculates the number of items in each row of a component group.
     * @param {Component[]} componentGroup - The list of components in the group.
     */
    CalcItemInRow(componentGroup) {
        let cumulativeColumn = 0;
        let itemInRow = 0;
        let startRowIndex = 0;

        for (let i = 0; i < componentGroup.length; i++) {
            const group = componentGroup[i];
            const parentInnerCol = this.GetInnerColumn(group.Parent);
            const outerCol = this.GetOuterColumn(group);

            if (parentInnerCol <= 0) {
                continue;
            }

            itemInRow++;
            cumulativeColumn += outerCol;

            if (cumulativeColumn % parentInnerCol === 0) {
                let sameRow = i;
                while (sameRow >= startRowIndex) {
                    componentGroup[sameRow].ItemInRow = itemInRow;
                    sameRow--;
                }
                itemInRow = 0;
                startRowIndex = i;
            }
        }
    }

    /**
     * Calculates the appropriate column width based on the component group and screen width.
     * @param {Component} group - The component group to evaluate.
     * @returns {number} The number of columns the component should span.
     */
    GetInnerColumn(group) {
        if (!group) return 0;

        const screenWidth = this.Element.clientWidth;
        let res;

        if (screenWidth < EditableComponent.ExSmallScreen && group.XsCol > 0) {
            res = group.XsCol;
        } else if (screenWidth < EditableComponent.SmallScreen && group.SmCol > 0) {
            res = group.SmCol;
        } else if (screenWidth < EditableComponent.MediumScreen && group.Column > 0) {
            res = group.Column;
        } else if (screenWidth < EditableComponent.LargeScreen && group.LgCol > 0) {
            res = group.LgCol;
        } else if (screenWidth < EditableComponent.ExLargeScreen && group.XlCol > 0) {
            res = group.XlCol;
        } else {
            res = group.XxlCol || group.Column;
        }

        return res || 0;
    }

    /**
     * Calculates the appropriate outer column width based on the component group and screen width.
     * @param {Component} group - The component group to evaluate.
     * @returns {number} The number of columns including the outer margin/padding.
     */
    GetOuterColumn(group) {
        if (!group) return 0;

        const screenWidth = this.Element.clientWidth;
        let res;

        if (screenWidth < EditableComponent.ExSmallScreen && group.XsOuterColumn > 0) {
            res = group.XsOuterColumn;
        } else if (screenWidth < EditableComponent.SmallScreen && group.SmOuterColumn > 0) {
            res = group.SmOuterColumn;
        } else if (screenWidth < EditableComponent.MediumScreen && group.OuterColumn > 0) {
            res = group.OuterColumn;
        } else if (screenWidth < EditableComponent.LargeScreen && group.LgOuterColumn > 0) {
            res = group.LgOuterColumn;
        } else if (screenWidth < EditableComponent.ExLargeScreen && group.XlOuterColumn > 0) {
            res = group.XlOuterColumn;
        } else {
            res = group.XxlOuterColumn || group.OuterColumn;
        }

        return res || 0;
    }

    /**
     * Binds the template with components.
     * @param {HTMLElement} ele - The HTML element to bind.
     * @param {EditableComponent} parent - The parent component.
     * @param {object} entity - The entity object.
     * @param {Function} [factory] - The factory function to create components.
     * @param {Set<HTMLElement>} [visited] - The set of visited elements.
     */
    BindingTemplate(ele, parent, entity = null, factory = null, visited = new Set()) {
        if (!ele || visited.has(ele)) {
            return;
        }
        visited.add(ele);
        if (ele.children.length === 0 && this.RenderCellText(ele, entity) !== null) {
            return;
        }
        const meta = this.ResolveMeta(ele);
        const newCom = factory ? factory(ele, meta, parent, entity) : this.BindingCom(ele, meta, parent, entity);
        parent = newCom instanceof Section ? newCom : parent;
        // @ts-ignore
        ele.children.forEach(child => this.BindingTemplate(child, parent, entity, factory, visited));
    }

    /**
     * Resolves meta information for an HTML element.
     * @param {HTMLElement} ele - The HTML element.
     * @returns {Component} - The resolved component.
     */
    ResolveMeta(ele) {
        /** @type {Component} */
        let component = new Component();
        const id = ele.dataset[this.IdField.toLowerCase()];
        if (id) {
            component = this.AllCom.find(x => x.Id === id);
        }
        for (const prop of Object.getOwnPropertyNames(Component.prototype)) {
            const value = ele.dataset[prop.toLowerCase()];
            if (!value) {
                continue;
            }
            let propVal = null;
            try {
                propVal = typeof component[prop] === 'string' ? value : JSON.parse(value);
                component = component || new Component();
                component.SetPropValue(prop, propVal);
            } catch {
                continue;
            }
        }
        return component;
    }

    /**
     * Renders the text content of a cell.
     * @param {HTMLElement} ele - The HTML element.
     * @param {object} entity - The entity object.
     * @returns {Label} - The rendered label if applicable, otherwise null.
     */
    RenderCellText(ele, entity) {
        const text = ele.textContent.trim();
        if (text && text.startsWith("{") && text.endsWith("}")) {
            /** @type {Component} */
            // @ts-ignore
            const meta = {
                FieldName: text.slice(1, -1)
            };
            const cellText = new Label(meta, ele);
            cellText.Entity = entity;
            if (EditableComponent.LayoutForm) {
                this.LayoutForm.AddChild(cellText);
            } else {
                cellText.Render();
            }
            return cellText;
        }
        return null;
    }

    static GetFeatureNameFromUrl() {
        let builder = new StringBuilder();
        let feature = window.location.pathname.toLowerCase().replace(Client.BaseUri.toLowerCase(), "");
        if (feature.startsWith(Utils.Slash)) {
            feature = feature.substring(1);
        }
        if (!feature.trim()) {
            return null;
        }
        for (let i = 0; i < feature.length; i++) {
            if (feature[i] === '?' || feature[i] === '#') break;
            builder.Append(feature[i]);
        }
        return builder.toString();
    }

    ShouldLoadEntity = false;
    /**
     * Loads the entity based on the URL or the given entity ID.
     * @returns {Promise<object>} A promise that resolves to the loaded entity object.
     */
    async LoadEntity() {
        const urlFeature = EditForm.GetFeatureNameFromUrl();
        const urlId = urlFeature === this.FeatureName ? Utils.GetUrlParam(Utils.IdField) : this.EntityId;
        if (!this.ShouldLoadEntity || urlId.IsNullOrWhiteSpace()) {
            return null;
        }
        try {
            const ds = await Client.Instance.GetByIdAsync(this.EntityName, this.DataConn, urlId);
            if (!ds) {
                return null;
            }
            this.Entity = ds[0];
            return ds[0];
        } catch (error) {
            console.error("Failed to load entity:", error);
            return null;
        }
    }

    /**
     * Locks updates if the user does not have permission.
     */
    LockUpdate() {
        const generalRule = this.Feature.FeaturePolicy.filter(x => x.RecordId);
        const noPermission = (!this.Feature.IsPublic || !Utils.IsOwner(this.Entity)) && generalRule.every(x => !x.CanWrite && !x.CanWriteAll);
        if (noPermission) {
            this.LockUpdateButCancel();
        }
    }

    /**
     * Locks all updates except for the cancel operation.
     */
    LockUpdateButCancel() {
        this.Disabled = true;
        this.SetDisabled(false, EditForm.BtnCancel);
    }

    /** @type {HTMLElement} */
    IconElement = null;
    /** @type {HTMLElement} */
    TitleElement = null;

    get Icon() {
        return this._icon;
    }

    set Icon(value) {
        this._icon = value;
        if (this.IconElement !== null) {
            Html.Take(this.IconElement).IconForSpan(value);
        }
    }

    get Title() {
        return this._title;
    }

    set Title(value) {
        this._title = value;
        if (this.TitleElement !== null) {
            this.TitleElement.innerHTML = ''; // clear inner HTML
            Html.Take(this.TitleElement).IText(value);
        }
    }

    /**
     * Sets feature properties such as title and icon based on the provided feature object.
     * @param {Component} feature - The feature to set properties from.
     */
    SetFeatureProperties(feature) {
        if (!feature) return;

        this.Feature = feature;
        this.Meta = feature;
        this.Element.classList.add(feature.ClassName);
        Html.Take(this.Element).Style(feature.Style);
        if (!this.Icon) {
            this.Icon = feature.Icon;
        }
        if (!this.Title) {
            this.Title = feature.Label;
        }
    }

    /**
     * Sets the stylesheet for the feature if provided.
     * @param {string} styleSheet - The stylesheet to apply.
     */
    SetFeatureStyleSheet(styleSheet) {
        if (!styleSheet) return;
        const style = document.createElement('style');
        style.appendChild(document.createTextNode(styleSheet));
        style.setAttribute('source', 'feature');
        this.Element.appendChild(style);
    }

    /**
     * Renders tabs or sections based on the component group structure.
     * @param {Component[]} componentGroup - The components to render.
     */
    RenderTabOrSection(componentGroup) {
        componentGroup.sort((a, b) => a.Order - b.Order).forEach(group => {
            group.Disabled = this.Disabled || group.Disabled;
            if (group.IsTab) {
                Section.RenderTabGroup(this, group);
            } else {
                Section.RenderSection(this, group);
            }
        });
    }

    /**
     * Ensures the feature's events are dispatched to the DOM.
     * @param {object} events - Events to be dispatched.
     * @param {string} eventType - Type of the event.
     */
    DispatchFeatureEvent(events, eventType) {
        // Example dispatch, needs specific implementation
        if (events && events[eventType]) {
            const event = new CustomEvent(eventType, { detail: this.Entity });
            this.Element.dispatchEvent(event);
        }
    }

    /**
     * Handles keyboard shortcuts for introspection.
     * @param {Event} evt - The keyboard event.
     */
    async KeyDownIntro(evt) {
        if (evt.keyCode === 72 && evt.ctrlKey || evt.metaKey) { // Ctrl+H or Cmd+H
            evt.preventDefault();
            const scriptUrl = 'https://unpkg.com/intro.js/intro.js';
            await LoadScript(scriptUrl); // Assuming LoadScript is a defined function to load scripts
            const sql = {
                ComId: 'Intro',
                Action: 'GetByFeatureId',
                Params: JSON.stringify({ Id: this.Feature.Id }),
                MetaConn: this.MetaConn,
                DataConn: this.DataConn
            };
            try {
                const intro = await Client.Instance.UserSvc(sql);
                let script = `(x) => {
                    introJs().setOptions({
                        steps: [{ intro: "Tutorial start" }]`;
                intro.forEach(item => {
                    script += `, {
                        element: document.querySelector('#${item.FieldName}'),
                        intro: "${item.Label}"
                    }`;
                });
                script += `]).start();}`;
                const fn = new Function('return ' + script)();
                fn(this);
            } catch (e) {
                console.error('Error setting up intro.js tutorial:', e);
            }
        }
    }

    /**
     * Renders a template based on the feature configuration.
     * @param {Feature} feature - The feature configuration.
     * @returns {HTMLElement} The rendered template element.
     */
    RenderTemplate(feature) {
        let entryPoint = document.getElementById(EditForm.SpecialEntryPoint) || document.getElementById("template") || this.Element;
        if (this.ParentForm && this.Portal && !this.Popup) {
            this.ParentForm.Element = null;
            this.ParentForm.Dispose();
            this.ParentForm = null;
        }
        entryPoint.innerHTML = Str.Empty;
        if (feature.Template.HasAnyChar()) {
            entryPoint.innerHTML = feature.Template;
            this.BindingTemplate(entryPoint, this);
            const innerEntry = Array.from(entryPoint.querySelectorAll("[id='inner-entry']")).shift();
            this.ResetEntryPoint(innerEntry);
            entryPoint = innerEntry || entryPoint;
            if (entryPoint.style.display === 'none') {
                entryPoint.style.display = Str.Empty;
            }
        }
        return entryPoint;
    }

    /**
     * Resets the entry point for rendering.
     * @param {HTMLElement} entryPoint - The entry point to reset.
     */
    ResetEntryPoint(entryPoint) {
        if (entryPoint) {
            entryPoint.innerHTML = Str.Empty;
        }
    }

    /**
     * Renders the cell text for a given element if it matches the pattern.
     * @param {HTMLElement} ele - The element to check for cell text.
     * @param {object} entity - The entity to bind to the label.
     * @returns {Label|undefined} The label component if rendered, undefined otherwise.
     */
    RenderCellText(ele, entity) {
        const text = ele.textContent.trim();
        if (text.startsWith("{") && text.endsWith("}")) {
            const fieldName = text.substring(1, text.length - 1);
            /** @type {Component} */
            // @ts-ignore
            const com = { FieldName: fieldName };
            const label = new Label(com, ele);
            label.Entity = entity;
            label.Render();
            return label;
        }
        return null;
    }

    /**
     * Binds a component to an HTML element.
     * @param {HTMLElement} ele - The element to bind to.
     * @param {Component} com - The component metadata.
     * @param {EditableComponent} parent - The parent component.
     * @param {object} entity - The entity to bind to.
     * @returns {EditableComponent|undefined} The bound component, or undefined if not applicable.
     */
    BindingCom(ele, com, parent, entity) {
        if (!ele || !com || com.ComponentType.IsNullOrEmpty()) {
            return null;
        }
        let child = null;
        if (com.ComponentType === "Section") {
            child = new Section(ele);
            child.Meta = com;
        } else {
            child = ComponentFactory.GetComponent(com, this, ele);
        }
        if (!child) return null;
        child.ParentElement = child.ParentElement || ele;
        child.Entity = entity || child.EditForm?.Entity || this.Entity;
        parent.AddChild(child);
        return child;
    }

    /**
     * Cancels the current form action, with a dirty check.
     */
    Cancel() {
        this.DirtyCheckAndCancel();
    }

    /**
     * Cancels the current form action without asking, directly disposing of the form.
     */
    CancelWithoutAsk() {
        this.Dispose();
    }

    /**
     * Checks if the form is dirty before cancelling. Optionally provides a callback to execute after cancellation.
     * @param {Function|null} closeCallback - Optional callback to execute after closing.
     */
    DirtyCheckAndCancel(closeCallback = null) {
        if (!this.Dirty) {
            this.Dispose();
            if (closeCallback) closeCallback();
            return;
        }

        // Confirm dialog setup assumed
        const confirm = new ConfirmDialog({
            Content: "Do you want to save changes before closing?",
            OnYes: async () => {
                const success = await this.SavePatch();
                if (success) {
                    this.Dispose();
                    if (closeCallback) closeCallback();
                }
            },
            OnNo: () => {
                this.Dispose();
                if (closeCallback) closeCallback();
            },
            IgnoreCancel: true
        });
        confirm.Render();
    }

    /**
     * Disposes the form, removing it from the DOM and cleaning up resources.
     */
    Dispose() {
        window.removeEventListener('resize', this.ResizeHandler.bind(this));
        super.Dispose();
    }

    /**
     * Validates the entire form or specific components within it.
     * @param {boolean} showMessage - Whether to show validation messages.
     * @param {Function|null} predicate - Function to determine which components to validate.
     * @param {Function|null} ignorePredicate - Function to determine which components to ignore.
     * @returns {Promise<boolean>} A promise that resolves to the validation status of the form.
     */
    async IsFormValid(showMessage = true, predicate = null, ignorePredicate = null) {
        predicate = predicate || ((x) => x.Children.length === 0);
        ignorePredicate = ignorePredicate || ((x) => x.AlwaysValid || x.EmptyRow);

        const validationPromises = this.FilterChildren(predicate, ignorePredicate).map(x => x.ValidateAsync());
        const results = await Promise.all(validationPromises);
        const invalidComponents = results.filter(result => !result.IsValid);

        if (invalidComponents.length > 0) {
            if (showMessage) {
                invalidComponents.forEach(comp => { comp.Disabled = false; });
                const firstInvalid = invalidComponents[0];
                firstInvalid.Focus();
                const messages = invalidComponents.flatMap(x => x.ValidationResult.Values);
                Toast.Warning(messages.join("\n"));
            }
            return false;
        }
        return true;
    }

    /**
     * Prints the contents of a selected HTML element.
     * @param {string} selector - The CSS selector to identify the printable area.
     */
    Print(selector = ".printable") {
        const printableArea = this.Element.querySelector(selector);
        if (printableArea) {
            const printWindow = window.open(Str.Empty, '_blank');
            printWindow.document.write(printableArea.innerHTML);
            printWindow.document.close();
            printWindow.focus();
            printWindow.print();
            printWindow.close();
        }
    }

    /**
     * Sends an email with a PDF attachment generated from the selected HTML content.
     * @param {EmailVM} email - The email view model containing details about the email to be sent.
     * @param {string[]} pdfSelector - Array of CSS selectors identifying the content to convert into PDF.
     * @returns {Promise<boolean>} A promise that resolves to the success status of the email operation.
     */
    async EmailPdf(email, pdfSelector = []) {
        if (!email) throw new Error("EmailVM must not be null.");

        const pdfContents = pdfSelector.map(selector => {
            const element = this.Element.querySelector(selector);
            return this.PrintSection(element, false);
        });

        email.PdfText = email.PdfText.concat(pdfContents);

        try {
            const success = await Client.Instance.PostAsync(email, "/user/EmailAttached");
            Toast.Success("Email sent successfully!");
            return success;
        } catch (error) {
            Toast.Error("Error while sending email: " + error.message);
            throw error;
        }
    }

    /**
     * Deletes the entity associated with the form.
     */
    Delete() {
        const confirm = new ConfirmDialog({
            Content: "Are you sure you want to delete this?",
            OnYes: async () => {
                try {
                    const success = await Client.Instance.HardDeleteAsync([this.EntityId], this.Feature.EntityName, this.DataConn, this.MetaConn);
                    if (success) {
                        Toast.Success("Data deleted successfully");
                        this.ParentForm?.UpdateView();
                        this.Dispose();
                    } else {
                        Toast.Warning("An error occurred while deleting data");
                    }
                } catch (error) {
                    Toast.Warning("An error occurred: " + error.message);
                }
            }
        });
        confirm.Render();
    }

    /**
     * Handles the signing in process.
     */
    SignIn() {
        Client.UnAuthorizedEventHandler?.call(null);
    }

    /**
     * Handles the signing out process.
     */
    SignOut() {
        const e = window.event;
        e.preventDefault();
        Client.Instance.PostAsync(Client.Token, "user/SignOut")
            .then(success => {
                Toast.Success("You have successfully signed out!");
                Client.SignOutEventHandler?.call();
                Client.Token = null;
                this.NotificationClient?.Close();
                window.location.reload();
            }).catch(error => {
                Toast.Warning("Error during sign out: " + error.message);
            });
    }

    /**
     * Retrieves security policies for a specific record or set of records.
     * @param {string|string[]} recordIds - The record ID or IDs to fetch policies for.
     * @param {string} entityName - The name of the entity.
     * @returns {FeaturePolicy[]} Array of applicable feature policies.
     */
    GetElementPolicies(recordIds, entityName = 'Component') {
        return Array.isArray(recordIds)
            ? this.Policies.filter(policy => policy.EntityName === entityName && recordIds.includes(policy.RecordId))
            : this.Policies.filter(policy => policy.EntityName === entityName && policy.RecordId === recordIds);
    }

    /**
     * Updates the properties of a component based on a dialog or other user input.
     * @param {object} arg - The argument containing information about the component to update.
     */
    ComponentProperties(arg) {
        const component = arg;
        const editor = new ComponentBL({
            Entity: component,
            ParentElement: this.Element,
            OpenFrom: this.FindClosest(EditForm)
        });
        this.AddChild(editor);
    }

    /**
     * Dynamically adds a new component to the form based on user actions.
     * @param {object} arg - Contains details on the action to perform and the group to which the component will be added.
     */
    AddComponent(arg) {
        /** @type {string} */
        const action = arg.action;
        /** @type {Component} */
        const group = arg.group;
        const com = new Component({
            ComponentType: action,
            ComponentGroupId: group.Id,
            Label: "New Component",
            Visibility: true,
            Order: group.Children?.length ? Math.max(...group.Children.map(c => c.Order)) + 1 : 0
        });

        // Assume an API or service is available to save the new component
        Client.Instance.PatchAsync(ComponentExt.MapToPatch(com)).then(() => {
            group.Children.push(com);
            this.UpdateRender(com, group);
            Toast.Success("Component added successfully!");
        }).catch(error => {
            Toast.Error("Error adding component: " + error.message);
        });
    }

    /**
     * Updates the rendering of components within the form.
     * @param {Component} component - The new component to render.
     * @param {Component} group - The group to which the component belongs.
     */
    UpdateRender(component, group) {
        const section = this.FindComponentByName(group.FieldName);
        const childComponent = ComponentFactory.GetComponent(component, this);
        if (childComponent) {
            childComponent.ParentElement = section.Element;
            section.AddChild(childComponent);
        }
    }

    /** @type {EditableComponent} */
    CtxCom;
    /** @type {FeaturePolicy[]} */
    Policies;
    /**
     * 
     * @param {Event} e 
     * @param {Component} component 
     * @param {Component} group 
     * @param {EditableComponent} ctx 
     * @returns 
     */
    SysConfigMenu(e, component, group, ctx) {
        this.CtxCom = ctx;
        const metaPermission = this.Policies.some(x => x.CanWriteMeta || x.CanWriteMetaAll);
        if (!metaPermission) {
            return;
        }
        const menuItems = [
            { Icon: "fas fa-link mt-2", Text: "Add Link", Click: this.AddComponent, Parameter: { group: group, action: "AddLink" } },
            { Icon: "fas fa-plus-circle mt-2", Text: "Add Input", Click: this.AddComponent, Parameter: { group: group, action: "AddInput" } },
            { Icon: "fas fa-plus-circle mt-2", Text: "Add Timepicker", Click: this.AddComponent, Parameter: { group: group, action: "AddTimepicker" } },
            { Icon: "fas fa-lock mt-2", Text: "Add Password", Click: this.AddComponent, Parameter: { group: group, action: "AddPassword" } },
            { Icon: "fas fa-plus-circle mt-2", Text: "Add Label", Click: this.AddComponent, Parameter: { group: group, action: "AddLabel" } },
            { Icon: "fas fa-plus-circle mt-2", Text: "Add Textarea", Click: this.AddComponent, Parameter: { group: group, action: "AddTextarea" } },
            { Icon: "fas fa-plus-circle mt-2", Text: "Add Dropdown", Click: this.AddComponent, Parameter: { group: group, action: "AddDropdown" } },
            { Icon: "fas fa-images mt-2", Text: "Add Image", Click: this.AddComponent, Parameter: { group: group, action: "AddImage" } },
            { Icon: "fas fa-plus-circle mt-2", Text: "Add GridView", Click: this.AddComponent, Parameter: { group: group, action: "AddGridView" } },
            { Icon: "fas fa-plus-circle mt-2", Text: "Add ListView", Click: this.AddComponent, Parameter: { group: group, action: "AddListView" } },
        ];
        e.preventDefault();
        e.stopPropagation();
        const ctxMenu = ContextMenu.Instance;
        ctxMenu.Top = e.Top();
        ctxMenu.Left = e.Left();
        ctxMenu.MenuItems = [];
        if (component !== null && component.ComponentType.includes("View")) {
            ctxMenu.MenuItems.push({ Icon: "fal fa-tasks", Text: "Header Manage", Click: this.headerMamage, Parameter: component });
        }
        ctxMenu.MenuItems.push(
            component !== null ? { Icon: "fal fa-cog", Text: "Tùy chọn dữ liệu", Click: this.componentProperties, Parameter: component } : null,
            component !== null ? { Icon: "fal fa-clone", Text: "Sao chép", Click: this.copyComponent, Parameter: component } : null,
            { Icon: "fal fa-cogs", Text: "Thêm Component", MenuItems: menuItems },
            { Icon: "fal fa-cogs", Text: "Tùy chọn vùng dữ liệu", Click: this.sectionProperties, Parameter: group },
            { Icon: "fal fa-folder-open", Text: "Thiết lập chung", Click: this.featureProperties },
            { Icon: "fal fa-folder-open", Text: "Layout", Click: this.layoutProperties },
            { Icon: "fal fa-clone", Text: "Clone feature", Click: this.cloneFeature, Parameter: Feature }
        );
        ctxMenu.Render();
    }

    headerMamage(arg) {
        const editor = new HeaderManageBL();
        editor.Entity = arg;
        editor.ParentElement = Element;
        editor.OpenFrom = this.CtxCom;
        editor.FeatureComponent = Feature;
        this.addChild(editor);
    }
}