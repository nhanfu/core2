import EditableComponent from "./editableComponent.js";
import { Action } from "./models/action.js";
import { Component } from "./models/component.js";
import { CustomEventType } from "./models/customEventType.js";
import { ActiveStateEnum, AdvSearchVM, MQEvent, Where } from "./models/enum.js";
import { PaginationOptions, Paginator } from "./paginator.js";
import { Utils } from "./utils/utils.js";
import { ObservableList } from './models/observableList.js';
import { ListViewSection } from './section.js';
import { Html } from "./utils/html.js";
import { ContextMenu } from "./contextMenu.js";
import { FeaturePolicy } from "./models/featurePolicy.js";
import './utils/ext.js';
import { string } from "./utils/ext.js";
import { Client } from "./clients/client.js";
import { Spinner } from "./spinner.js";
import { PatchDetail } from "./models/patch.js";

/**
 * Represents a list view component that allows editable features and other interactions like sorting and pagination.
 */
export class ListView extends EditableComponent {
    /** @type {EditableComponent} */
    MainSection;
    CacheData = [];
    DataLoaded = new Action();
    /**
     * Constructs an instance of ListView with the specified UI component.
     * @param {Component} ui The UI component associated with this list view.
     * @param {HTMLElement} [ele] Optional HTML element.
     */
    constructor(ui, ele = null) {
        super(ui);
        this.DeleteTempIds = [];
        this.Meta = ui;
        this.Id = ui.Id?.toString();
        this.Name = ui.FieldName;
        /** @type {Component[]} */
        this.Header = [];
        this.RowData = new ObservableList();
        /** @type {AdvSearchVM} */
        this.AdvSearchVM = {
            ActiveState: ActiveStateEnum.Yes,
            OrderBy: localStorage.getItem('OrderBy' + this.Meta.Id) ?? new List()
        };
        this._hasLoadRef = false;
        if (ele !== null) {
            this.Resolve(ui, ele);
        }

        this._rowHeight = this.Meta.BodyItemHeight ?? 26;
        this._theadTable = this.Meta.HeaderHeight ?? 40;
        this._tfooterTable = this.Meta.FooterHeight ?? 35;
        this._scrollTable = this.Meta.ScrollHeight ?? 10;
        window.addEventListener(this.QueueName, this.RealtimeUpdateListViewItem.bind(this));
        this._preQueryFn = Utils.IsFunction(this.Meta.PreQuery);
    }

    /**
     * Handles real-time updates of list view items.
     * @param {MQEvent} mqEvent The message queue event.
     */
    RealtimeUpdateListViewItem(mqEvent) {
        let updatedData = mqEvent.Message;
        let listViewItem = this.MainSection.FilterChildren(x => x.EntityId === updatedData[this.IdField]).FirstOrDefault();
        if (listViewItem === null) return;
        this.CacheData.FirstOrDefault(x => x[this.IdField] === updatedData[this.IdField]).CopyPropFrom(updatedData);
        listViewItem.Entity.CopyPropFrom(updatedData);
        let arr = listViewItem.FilterChildren(x => !x.Dirty || x.GetValueText() != null).Select(x => x.FieldName).ToArray();
        listViewItem.UpdateView(false, arr);
        this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterWebsocket, updatedData, listViewItem).Done();
    }

    /**
     * Resolves additional configurations or setup for the component.
     * @param {Component} com The component to configure.
     * @param {HTMLElement} [ele] Optional HTML element to use in the resolution.
     */
    Resolve(com, ele = null) {
        let txtArea = document.createElement('textarea');
        txtArea.innerHTML = ele.innerHTML;
        com.FormatEntity = txtArea.value;
        ele.innerHTML = null;
    }

    /** @type {FeaturePolicy[]} */
    GridPolicies;
    /** @type {FeaturePolicy[]} */
    GeneralPolicies;
    /**
     * Renders the list view, setting up necessary configurations and data bindings.
     */
    Render() {
        this.GridPolicies = this.EditForm.GetElementPolicies(this.Meta.Id);
        this.GeneralPolicies = this.EditForm.Feature.FeaturePolicy.Where(x => x.RecordId);
        this.CanWrite = this.CanDo(x => x.CanWrite || x.CanWriteAll);
        Html.Take(this.ParentElement).DataAttr('name', this.FieldName);
        this.AddSections();
        this.SetRowDataIfExists();
        this.EditForm.ResizeListView();
        if (this.Meta.LocalRender) this.LocalRender();
        else this.LoadAllData();
    }

    /**
     * Renders the list view either by re-rendering or using locally stored data based on the configuration.
     */
    LocalRender() {
        // Setting the header from the local metadata configuration
        this.Header = this.Meta.LocalHeader;

        if (this.Meta.LocalRender) {
            // If local rendering is enabled, re-render the view
            this.Rerender();
        } else {
            // If local rendering is not enabled, use the local data directly
            this.RowData.Data = this.Meta.LocalData;
        }
    }

    Rerender() {
        this.DisposeNoRecord();
        this.MainSection.DisposeChildren();
        Html.Take(this.MainSection.Element).Clear();
        this.RenderContent();
    }

    /**
     * Evaluates if any policy within the general or grid-specific policies meets the provided condition.
     * @param {(item: FeaturePolicy) => boolean} predicate A function to test each element for a condition.
     * @returns {boolean} True if any policy meets the condition, otherwise false.
     */
    CanDo(predicate) {
        return this.GeneralPolicies.some(predicate) || this.GridPolicies.some(predicate);
    }

    /**
     * Reloads data for the list view, potentially using cached headers and considering pagination settings.
     * @param {boolean} [cacheHeader=false] Specifies whether headers should be cached.
     * @param {number} [skip=null] Specifies the number of items to skip (for pagination).
     * @param {number} [pageSize=null] Specifies the size of the page to load.
     * @returns {Promise<any[]>} A promise that resolves to the list of reloaded data objects.
     */
    async ReloadData(cacheHeader = false, skip = null, pageSize = null) {
        if (this.Meta.LocalQuery) {
            this.Meta.LocalData = typeof this.Meta.LocalQuery === string.Type
                ? JSON.parse(this.Meta.LocalQuery)
                : this.Meta.LocalQuery;
            this.Meta.LocalRender = true;
        }
        if (this.Meta.LocalRender && this.Meta.LocalData != null) {
            this.SetRowData(this.Meta.LocalData);
            return this.Meta.LocalData;
        }
        if (this.Paginator !== null) {
            this.Paginator.Options.PageSize = this.Paginator.Options.PageSize === 0 ? (this.Meta.Row ?? 12) : this.Paginator.Options.PageSize;
        }
        pageSize = pageSize ?? this.Paginator?.Options?.PageSize ?? this.Meta.Row ?? 12;
        skip = skip ?? this.Paginator?.Options?.PageIndex * pageSize ?? 0;
        let sql = this.GetSql(skip, pageSize, cacheHeader);
        return this.CustomQuery(sql);
    }

    CalcFilterQuery() {
        return this.ListViewSearch.CalcFilterQuery();
    }
    /** @type {Where} */
    Wheres;
    /**
     * Gets the SQL for data retrieval based on the current state of the list view.
     * @param {number} [skip=null] Number of records to skip for pagination.
     * @param {number} [pageSize=null] Page size for pagination.
     * @param {boolean} [cacheMeta=false] Whether to cache meta information.
     * @param {boolean} [count=true] Whether to include a count of total records.
     * @returns {SqlViewModel} The SQL view model with query details.
     */
    GetSql(skip = null, pageSize = null, cacheMeta = false, count = true) {
        let submitEntity = Utils.IsFunction(this.Meta.PreQuery)?.call(null, this);
        let orderBy = this.AdvSearchVM.OrderBy.Any() ? this.AdvSearchVM.OrderBy.Combine(x => {
            let sortDirection = x.OrderbyDirectionId === OrderbyDirection.ASC ? "asc" : "desc";
            return `ds.${x.FieldName} ${sortDirection}`;
        }) : null;
        let basicCondition = this.CalcFilterQuery();
        let fnBtnCondition = this.Wheres.Combine(x => `(${x.Condition})`, " and ");
        let finalCon = [basicCondition, fnBtnCondition].filter(x => !x.IsNullOrWhiteSpace()).Combine(" and ");
        return {
            ComId: this.Meta.Id,
            Params: submitEntity ? JSON.stringify(submitEntity) : null,
            OrderBy: orderBy || (!this.Meta.OrderBy ? "ds.Id asc" : this.Meta.OrderBy),
            Where: finalCon,
            Count: count,
            SkipXQuery: cacheMeta,
            MetaConn: this.MetaConn,
            DataConn: this.DataConn,
        };
    }

    ShouldSetEntity = true;
    /**
     * 
     * @param {any[]} listData 
     */
    SetRowData(listData) {
        this.RowData?.Clear();
        const hasElement = listData.length; // Assuming hasElement is a method defined in this class
        if (hasElement) {
            this.RowData._data.push(...listData);
        }
        this.RenderContent(); // Assuming renderContent is a method defined in this class

        if (this.Entity !== null && this.ShouldSetEntity) { // Assuming shouldSetEntity is a property
            this.Entity.SetComplexPropValue(this.FieldName, this.RowData.Data); // Assuming setComplexPropValue is a method
        }
    }

    /**
     * Executes a custom SQL query using the provided SQL view model.
     * @param {SqlViewModel} vm The view model containing SQL query details.
     * @returns {Promise<List<object>>} A promise that resolves to the list of data objects retrieved.
     */
    async CustomQuery(vm) {
        try {
            let ds = await Client.Instance.ComQuery(vm);
            if (!ds || !ds.length) {
                this.SetRowData(null);
                return null;
            }
            let total = ds.length > 1 ? ds[1].ToDynamic()[0].total : ds[0].length;
            let rows = ds[0];
            Spinner.Hide();
            this.SetRowData(rows);
            this.UpdatePagination(total, rows.length);
            Utils.IsFunction(this.Meta.FormatEntity)?.call(null, rows, this);
            this.DataLoaded?.invoke(ds);
            return rows;
        } catch (error) {
            console.error('Error during custom query:', error);
            throw error;
        }
    }

    /**
     * Updates pagination details based on total data and current page count.
     * @param {number} total The total number of records.
     * @param {number} currentPageCount The number of records in the current page.
     */
    UpdatePagination(total, currentPageCount) {
        if (!this.Paginator) {
            return;
        }
        let options = this.Paginator.Options;
        options.Total = total;
        options.CurrentPageCount = currentPageCount;
        options.PageNumber = options.PageIndex + 1;
        options.StartIndex = options.PageIndex * options.PageSize + 1;
        options.EndIndex = options.StartIndex + options.CurrentPageCount - 1;
        this.Paginator.UpdateView();
    }

    /**
     * Handles the rendering of pagination components within the list view.
     */
    RenderPaginator() {
        if (this.Meta.LocalRender || this.Meta.LiteGrid) {
            if (this.Paginator) {
                this.Paginator.Show = false;
            }
            return;
        }
        if (this.Meta.Row === null || this.Meta.Row === 0) {
            this.Meta.Row = 20;
        }

        if (!this.Paginator) {
            /** @type {PaginationOptions} */
            const options = {
                Total: 0,
                PageSize: this.Meta.Row ?? 50,
                CurrentPageCount: this.RowData.Data.length,
            };
            this.Paginator = new Paginator(options);
            this.AddChild(this.Paginator);
        }
    }

    /**
     * Adds sections to the ListView based on the component configurations.
     */
    AddSections() {
        if (this.Meta.LiteGrid) {
            this.Element = this.ParentElement;
            this.Element.innerHTML = null;
            this.MainSection = new ListViewSection(null, this.ParentElement);
            this.AddChild(this.MainSection);
            return;
        }
        Html.Take(this.ParentElement).Div.ClassName("grid-wrapper")
            .ClassName(this.Editable ? "editable" : "");
        this.Element = Html.Context;
        if (this.Meta.CanSearch) {
            Html.Instance.Div.ClassName("grid-toolbar search").End.Render();
        }
        this.ListViewSearch = new ListViewSearch(this.Meta);
        this.AddChild(this.ListViewSearch);
        Html.Take(this.Element).Div.ClassName("list-content").End.Div.ClassName("empty");
        this.EmptySection = new ListViewSection(null, Html.Context);
        this.EmptySection.ParentElement = this.Element;
        this.AddChild(this.EmptySection);

        this.MainSection = new ListViewSection(null, this.EmptySection.Element.previousSibling);
        this.AddChild(this.MainSection);

        Html.Instance.EndOf(".list-content");
        this.RenderPaginator();
    }

    /** @type {any[]} */
    FormattedRowData;
    /**
     * Renders the content within the main section of the ListView.
     */
    RenderContent() {
        this.MainSection.DisposeChildren();
        this.EmptySection?.DisposeChildren();
        this.FormattedRowData = this.FormattedRowData.Nothing() ? this.RowData.Data : this.FormattedRowData;
        if (this.FormattedRowData.Nothing()) {
            return;
        }

        this.FormattedRowData.SelectForEach((rowData, index) => {
            let rowSection = this.RenderRowData(this.Header, rowData, this.MainSection);
        });
        this.ContentRendered();
    }

    /**
     * Renders the data for each row within the list view.
     * @param {Component[]} headers The headers to use in the row.
     * @param {object} row The data object for the row.
     * @param {ListViewSection} section The section where the row is to be added.
     * @param {number} [index=null] Optional index for the row.
     * @param {boolean} [emptyRow=false] Indicates if the row is empty.
     * @returns {ListViewItem} The ListViewItem created for the row.
     */
    RenderRowData(headers, row, section, index = null, emptyRow = false) {
        let rowSection = this.Meta.LiteGrid ? new ListViewItem() : new ListViewItem('div');
        rowSection.EmptyRow = emptyRow;
        rowSection.Entity = row;
        rowSection.ParentElement = section.Element;
        rowSection.ListView = this;
        rowSection.ListViewSection = section instanceof ListViewSection ? section : null;
        rowSection.Meta = this.Meta;
        rowSection.EditForm = this.EditForm;
        section.AddChild(rowSection, index);
        rowSection.RenderRowData(headers, row, index, emptyRow);
        return rowSection;
    }

    /**
     * Clears all row data from the ListView.
     */
    ClearRowData() {
        this.RowData.Clear();
        this.RowAction(x => !x.EmptyRow, x => x.Dispose());
        this.MainSection.Element.innerHTML = null;
        if (this.Entity == null || this.Parent instanceof SearchEntry) {
            return;
        }
        if (this.ShouldSetEntity) {
            this.Entity?.SetComplexPropValue(this.FieldName, this.RowData.Data);
        }
    }

    get AllListViewItem() { return this.MainSection.Children; }
    /**
     * Performs an action on all items that meet the condition specified by predicate.
     * @param {(item: EditableComponent) => boolean} predicate - The condition to check each ListViewItem.
     * @param {(item: EditableComponent) => void} action - The action to perform on each ListViewItem that meets the condition.
     */
    RowAction(predicate, action) {
        this.AllListViewItem.filter(x => !predicate || predicate(x)).forEach(action);
    }

    /**
     * Sets row data if the entity exists and it is not an empty string.
     */
    SetRowDataIfExists() {
        const value = Utils.GetPropValue(this.Entity, this.FieldName);
        if (this.Entity != null && Array.isArray(value)) {
            this.RowData._data = value;
        }
    }

    /**
     * Method to update the view of the ListView, possibly forcing the update and setting the dirty flag.
     * @param {boolean} [force=false] Whether to force the update.
     * @param {boolean|null} [dirty=null] Optional dirty flag to set.
     * @param {string[]} componentNames Component names to specifically update.
     */
    UpdateView(force = false, dirty = null, componentNames = []) {
        if (!this.Editable) {
            if (force) {
                this.ListViewSearch.RefreshListView();
            }
        } else {
            this.RowAction(row => !row.EmptyRow, row => row.UpdateView(force, dirty, componentNames));
        }
    }

    /**
     * Adds a new empty row to the ListView.
     */
    AddNewEmptyRow() {
        if (this.Meta.LiteGrid || this.Disabled || !this.Editable || (this.EmptySection?.Children.HasElement() === true)) {
            return;
        }
        let emptyRowData = {};
        let fn = Utils.IsFunction(this.Meta.DefaultVal);
        if (!this.Meta.DefaultVal && fn) {
            let dfObj = fn.call(this, this);
            Object.keys(dfObj).forEach(key => {
                emptyRowData[key] = dfObj[key];
            });
        }
        emptyRowData[this.IdField] = null;
        let rowSection = this.RenderRowData(this.Header, emptyRowData, this.EmptySection, null, true);
        Object.entries(emptyRowData).forEach(([field, value]) => {
            rowSection.PatchModel.Add(new PatchDetail({
                Field: field,
                Value: value?.toString()
            }));
        });
        if (!this.Meta.TopEmpty) {
            this.MainSection.Element.insertBefore(this.MainSection.Element, this.EmptySection.Element);
        } else {
            this.MainSection.Element.appendChild(this.EmptySection.Element.firstElementChild);
        }
        this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterEmptyRowCreated, emptyRowData).Done();
    }

    /**
     * Filters and sorts the header components based on their properties.
     * @param {List<Component>} components The list of components to filter.
     * @returns {List<Component>} The filtered and sorted list of header components.
     */
    FilterColumns(components) {
        if (components.Nothing()) return components;
        let specificComponent = components.Any(x => x.ComponentId === this.Meta.Id);
        if (specificComponent) {
            components = components.Where(x => x.ComponentId === this.Meta.Id).ToList();
        } else {
            components = components.Where(x => x.ComponentId == null).ToList();
        }

        let permission = this.EditForm.GetGridPolicies(components.Select(x => x.Id).ToArray(), Utils.ComponentId);
        let headers = components
            .Where(header => !header.IsPrivate || permission.Where(x => x.RecordId === header.Id).HasElementAndAll(policy => policy.CanRead))
            .OrderByDescending(x => x.Frozen).ThenBy(x => x.Order).ToList();
        this.OrderHeaderGroup(headers);
        this.Header.Clear();
        this.Header.AddRange(headers);
        this.Header = this.Header.Where(x => x != null).ToList();
        return this.Header;
    }

    /**
     * Renders the paginator component if necessary based on the configuration and data.
     */
    RenderPaginator() {
        if (this.Meta.LocalRender || this.Meta.LiteGrid) {
            if (this.Paginator) {
                this.Paginator.Show = false;
            }
            return;
        }
        if (!this.Meta.Row || this.Meta.Row === 0) {
            this.Meta.Row = 20;
        }

        if (!this.Paginator) {
            this.Paginator = new Paginator({
                Total: 0,
                PageSize: this.Meta.Row ?? 50,
                CurrentPageCount: this.RowData.Data.Count(),
            });
            this.AddChild(this.Paginator);
        }
    }

    /**
     * Applies a filter to the ListView, reloading data based on the current filter settings.
     * @returns {Promise} A promise that resolves once the data has been reloaded with the applied filter.
     */
    ApplyFilter() {
        this.ClearRowData();
        return this.ReloadData(true, 0, true);
    }

    /**
     * Adds a new empty row to the ListView.
     */
    AddNewEmptyRow() {
        if (this.Meta.LiteGrid || this.Disabled || !this.Editable || (this.EmptySection?.Children.HasElement() === true)) {
            return;
        }
        let emptyRowData = {};
        if (this.Meta.DefaultVal) {
            const fn = Utils.IsFunction(this.Meta.DefaultVal);
            let dfObj = fn ? fn.call(this, this) : null;
            Object.keys(dfObj).forEach(key => {
                emptyRowData[key] = dfObj[key];
            });
        }
        emptyRowData[this.IdField] = null;
        let rowSection = this.RenderRowData(this.Header, emptyRowData, this.EmptySection, null, true);
        Object.entries(emptyRowData).forEach(([field, value]) => {
            rowSection.PatchModel.Add(new PatchDetail({
                Field: field,
                Value: value?.toString()
            }));
        });
        if (!this.Meta.TopEmpty) {
            this.MainSection.Element.insertBefore(this.MainSection.Element, this.EmptySection.Element);
        } else {
            this.MainSection.Element.appendChild(this.EmptySection.Element.firstElementChild);
        }
        this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterEmptyRowCreated, emptyRowData).Done();
    }

    /**
     * Filters and sorts the header components based on their properties.
     * @param {Component[]} components The list of components to filter.
     * @returns {Component[]} The filtered and sorted list of header components.
     */
    FilterColumns(components) {
        if (components.length === 0) return components;
        let specificComponent = components.Any(x => x.ComponentId === this.Meta.Id);
        if (specificComponent) {
            components = components.Where(x => x.ComponentId === this.Meta.Id).ToList();
        } else {
            components = components.Where(x => x.ComponentId == null).ToList();
        }

        let permission = this.EditForm.GetGridPolicies(components.Select(x => x.Id).ToArray(), Utils.ComponentId);
        let headers = components
            .Where(header => !header.IsPrivate || permission.Where(x => x.RecordId === header.Id).HasElementAndAll(policy => policy.CanRead))
            .OrderByDescending(x => x.Frozen).ThenBy(x => x.Order).ToList();
        this.OrderHeaderGroup(headers);
        this.Header.Clear();
        this.Header.AddRange(...headers);
        this.Header = this.Header.Where(x => x != null);
        return this.Header;
    }

    /**
     * Renders the paginator component if necessary based on the configuration and data.
     */
    RenderPaginator() {
        if (this.Meta.LocalRender || this.Meta.LiteGrid) {
            if (this.Paginator) {
                this.Paginator.Show = false;
            }
            return;
        }
        if (!this.Meta.Row || this.Meta.Row === 0) {
            this.Meta.Row = 20;
        }

        if (!this.Paginator) {
            this.Paginator = new Paginator({
                Total: 0,
                PageSize: this.Meta.Row ?? 50,
                CurrentPageCount: this.RowData.Data.Count(),
            });
            this.AddChild(this.Paginator);
        }
    }

    /**
     * Applies a filter to the ListView, reloading data based on the current filter settings.
     * @returns {Promise} A promise that resolves once the data has been reloaded with the applied filter.
     */
    ApplyFilter() {
        this.ClearRowData();
        return this.ReloadData(true, 0, true);
    }

    /**
     * Handles the context menu for the body of the list view, showing additional options.
     * @param {Event} e The event object associated with the context menu action.
     */
    BodyContextMenuHandler(e) {
        e.preventDefault();
        e.stopPropagation();
        this.BodyContextMenuShow?.();
        if (this.Disabled) {
            return;
        }
        this.SetSelected(e);
        const selectedRows = this.GetSelectedRows();
        let ctxMenu = ContextMenu.Instance;
        this.RenderRelatedDataMenu().then(() => {
            this.RenderCopyPasteMenu(this.CanWrite);
            this.RenderEditMenu(this.CanWrite);
            this.RenderShareMenu(selectedRows).then(() => {
                ctxMenu.Top = e.clientY;
                ctxMenu.Left = e.clientX;
                ctxMenu.Render();
                this.Element.appendChild(ctxMenu.Element);
                ctxMenu.Element.style.position = "absolute";
            });
        });
    }

    /**
     * Sets the row as selected based on the event target.
     * @param {Event} e The event object.
     */
    SetSelected(e) {
        let target = e.target.closest('tr');
        let currentRow = this.MainSection.Children.find(x => x.Element === target);
        if (currentRow) {
            if (!currentRow.GroupRow || this.Meta.GroupReferenceId) {
                if (this.SelectedIds.length === 1) {
                    this.ClearSelected();
                }
                currentRow.Selected = true;
                this.LastListViewItem = currentRow;
                this.SelectedIndex = currentRow.ListViewSection.Children.indexOf(currentRow);
            }
        }
    }

    /**
     * Clears all selections within the ListView.
     * @param {...string[]} ids Specific IDs to clear, if provided.
     */
    ClearSelected(...ids) {
        let shouldClear = ids.length ? this.SelectedIds.filter(id => ids.includes(id)) : [...this.SelectedIds];
        shouldClear.forEach(id => {
            this.SelectedIds.splice(this.SelectedIds.indexOf(id), 1);
            this.MainSection.Children.forEach(child => {
                if (child.Selected && child.EntityId === id) {
                    child.Selected = false;
                }
            });
        });
        this.LastListViewItem = null;
    }

    /**
     * Renders the pagination details and handles the data loading process.
     */
    LoadAllData() {
        this.ReloadData(true).then(() => {
            this.RenderContent();
        });
    }

    /**
     * Filters the columns based on the header configuration and applies sort order.
     */
    OrderHeaderGroup(headers) {
        for (let i = 0; i < headers.length - 1; i++) {
            for (let j = i + 1; j < headers.length; j++) {
                if (headers[i].GroupName && headers[i].GroupName === headers[j].GroupName && headers[i + 1].GroupName !== headers[j].GroupName) {
                    let temp = headers[i + 1];
                    headers[i + 1] = headers[j];
                    headers[j] = temp;
                }
            }
        }
    }

    /**
     * Renders menus related to the data linked with the selected rows, such as copy, paste, and editing options.
     * @param {boolean} canWrite Indicates whether the user has write permissions.
     */
    RenderCopyPasteMenu(canWrite) {
        if (canWrite) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "fa fa-copy",
                Text: "Copy",
                Click: () => this.CopySelected()
            });
            ContextMenu.Instance.MenuItems.push({
                Icon: "fa fa-clone",
                Text: "Copy & Paste",
                Click: () => this.DuplicateSelected(null, false)
            });
        }
        if (canWrite && this._copiedRows && this._copiedRows.length > 0) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "fa fa-paste",
                Text: "Paste",
                Click: () => this.PasteSelected()
            });
        }
    }

    /**
     * Renders edit menu options based on user permissions.
     * @param {boolean} canWrite Indicates whether the user has write permissions.
     */
    RenderEditMenu(canWrite) {
        if (canWrite) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "fa fa-history",
                Text: "View History",
                Click: () => this.ViewHistory()
            });
        }
        if (this.CanDo(x => x.CanDeactivate || x.CanDeactivateAll)) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "mif-unlink",
                Text: "Deactivate (without deleting)",
                Click: () => this.DeactivateSelected()
            });
        }
        if (this.CanDo(x => x.CanDelete || x.CanDeleteAll)) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "fa fa-trash",
                Text: "Delete Data",
                Click: () => this.HardDeleteSelected()
            });
        }
    }

    /**
 * Handles the context menu for the body of the list view, showing additional options.
 * @param {Event} e The event object associated with the context menu action.
 */
    BodyContextMenuHandler(e) {
        e.preventDefault();
        e.stopPropagation();
        this.BodyContextMenuShow?.();
        if (this.Disabled) {
            return;
        }
        this.SetSelected(e);
        const selectedRows = this.GetSelectedRows();
        let ctxMenu = ContextMenu.Instance;
        this.RenderRelatedDataMenu().then(() => {
            this.RenderCopyPasteMenu(this.CanWrite);
            this.RenderEditMenu(this.CanWrite);
            this.RenderShareMenu(selectedRows).then(() => {
                ctxMenu.Top = e.clientY;
                ctxMenu.Left = e.clientX;
                ctxMenu.Render();
                this.Element.appendChild(ctxMenu.Element);
                ctxMenu.Element.style.position = "absolute";
            });
        });
    }

    /**
     * Sets the row as selected based on the event target.
     * @param {Event} e The event object.
     */
    SetSelected(e) {
        let target = e.target.closest('tr');
        let currentRow = this.MainSection.Children.find(x => x.Element === target);
        if (currentRow) {
            if (!currentRow.GroupRow || this.Meta.GroupReferenceId) {
                if (this.SelectedIds.length === 1) {
                    this.ClearSelected();
                }
                currentRow.Selected = true;
                this.LastListViewItem = currentRow;
                this.SelectedIndex = currentRow.ListViewSection.Children.indexOf(currentRow);
            }
        }
    }

    /**
     * Clears all selections within the ListView.
     * @param {...string[]} ids Specific IDs to clear, if provided.
     */
    ClearSelected(...ids) {
        let shouldClear = ids.length ? this.SelectedIds.filter(id => ids.includes(id)) : [...this.SelectedIds];
        shouldClear.forEach(id => {
            this.SelectedIds.splice(this.SelectedIds.indexOf(id), 1);
            this.MainSection.Children.forEach(child => {
                if (child.Selected && child.EntityId === id) {
                    child.Selected = false;
                }
            });
        });
        this.LastListViewItem = null;
    }

    /**
     * Renders the pagination details and handles the data loading process.
     */
    LoadAllData() {
        this.ReloadData(true).then(() => {
            this.RenderContent();
        });
    }

    /**
     * Filters the columns based on the header configuration and applies sort order.
     */
    OrderHeaderGroup(headers) {
        for (let i = 0; i < headers.length - 1; i++) {
            for (let j = i + 1; j < headers.length; j++) {
                if (headers[i].GroupName && headers[i].GroupName === headers[j].GroupName && headers[i + 1].GroupName !== headers[j].GroupName) {
                    let temp = headers[i + 1];
                    headers[i + 1] = headers[j];
                    headers[j] = temp;
                }
            }
        }
    }

    /**
     * Renders menus related to the data linked with the selected rows, such as copy, paste, and editing options.
     * @param {boolean} canWrite Indicates whether the user has write permissions.
     */
    RenderCopyPasteMenu(canWrite) {
        if (canWrite) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "fa fa-copy",
                Text: "Copy",
                Click: () => this.CopySelected()
            });
            ContextMenu.Instance.MenuItems.push({
                Icon: "fa fa-clone",
                Text: "Copy & Paste",
                Click: () => this.DuplicateSelected(null, false)
            });
        }
        if (canWrite && this._copiedRows && this._copiedRows.length > 0) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "fa fa-paste",
                Text: "Paste",
                Click: () => this.PasteSelected()
            });
        }
    }

    /**
     * Renders edit menu options based on user permissions.
     * @param {boolean} canWrite Indicates whether the user has write permissions.
     */
    RenderEditMenu(canWrite) {
        if (canWrite) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "fa fa-history",
                Text: "View History",
                Click: () => this.ViewHistory()
            });
        }
        if (this.CanDo(x => x.CanDeactivate || x.CanDeactivateAll)) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "mif-unlink",
                Text: "Deactivate (without deleting)",
                Click: () => this.DeactivateSelected()
            });
        }
        if (this.CanDo(x => x.CanDelete || x.CanDeleteAll)) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "fa fa-trash",
                Text: "Delete Data",
                Click: () => this.HardDeleteSelected()
            });
        }
    }

    /**
     * Renders sharing menu options based on user permissions and selected rows.
     * @param {Array<object>} selectedRows Array of selected rows.
     * @returns {Promise} A promise that resolves once the sharing menu is rendered.
     */
    async RenderShareMenu(selectedRows) {
        if (selectedRows.length === 0) return;
        const noPolicyRows = selectedRows.filter(x => !x[PermissionLoaded]);
        const noPolicyRowIds = noPolicyRows.map(x => x[IdField].toString());
        const rowPolicy = await this.LoadRecordPolicy(this.Meta.RefName, noPolicyRowIds);
        rowPolicy.forEach(policy => this.RecordPolicy.push(policy));
        noPolicyRows.forEach(row => row[PermissionLoaded] = true);
        const canShare = this.CanDo(x => x.CanShare || x.CanShareAll) && selectedRows.some(x => x[IsOwner]);
        if (canShare) {
            ContextMenu.Instance.MenuItems.push({
                Icon: "mif-security",
                Text: "Security & Permissions",
                Click: () => this.SecurityRows()
            });
        }
    }

    /**
     * Loads record-specific policies for permissions handling.
     * @param {string} entity The entity reference name.
     * @param {Array<string>} ids Array of record IDs to load policies for.
     * @returns {Promise<Array>} A promise that resolves to an array of policies.
     */
    async LoadRecordPolicy(entity, ids) {
        if (ids.length === 0 || ids.every(x => x === null)) {
            return [];
        }
        const sql = {
            ComId: "Policy",
            Action: "GetById",
            Table: nameof(FeaturePolicy),
            MetaConn: this.MetaConn,
            DataConn: this.DataConn,
            Params: JSON.stringify({ ids, table: entity })
        };
        return await Client.Instance.UserSvc(sql);
    }

    /**
     * Handles the event for selected row deactivation.
     */
    async DeactivateSelected() {
        const confirmDialog = new ConfirmDialog({
            Content: "Are you sure you want to deactivate?"
        });
        confirmDialog.Render();
        confirmDialog.YesConfirmed += async () => {
            confirmDialog.Dispose();
            const deactivatedIds = await this.Deactivate();
            this.DispatchCustomEvent(this.Meta.Events, CustomEventType.Deactivated, this.Entity);
        };
    }

    /**
     * Deactivates selected rows by their IDs.
     * @returns {Promise<Array<string>>} A promise that resolves to an array of deactivated IDs.
     */
    async Deactivate() {
        const ids = this.GetSelectedRows().map(x => x[IdField].toString());
        const deactivatedIds = await Client.Instance.DeactivateAsync(ids, this.Meta.RefName, this.DataConn);
        if (deactivatedIds.length > 0) {
            Toast.Success("Data deactivated successfully");
        } else {
            Toast.Warning("An error occurred during deactivation");
        }
        return deactivatedIds;
    }

    /**
     * Handles deleting selected rows after confirming the action.
     */
    async HardDeleteSelected() {
        const confirmDialog = new ConfirmDialog({
            Title: "Are you sure you want to delete the selected rows?"
        });
        confirmDialog.Render();
        confirmDialog.YesConfirmed += async () => {
            const deletedItems = this.GetSelectedRows();
            const deletedIds = await this.HardDeleteConfirmed(deletedItems);
            this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterDeleted, deletedItems);
        };
    }

    /**
     * Confirms the deletion of selected rows and performs the deletion.
     * @param {Array<object>} deletedItems Items to be deleted.
     * @returns {Promise<Array<object>>} A promise that resolves to the array of deleted items.
     */
    async HardDeleteConfirmed(deletedItems) {
        const ids = deletedItems.map(x => x[IdField]?.toString()).filter(x => x != null);
        const result = await Client.Instance.HardDeleteAsync(ids, this.Meta.RefName, this.DataConn);
        if (result) {
            Toast.Success("Data deleted successfully");
        } else {
            Toast.Warning("No rows were deleted");
        }
        return deletedItems;
    }

    /**
 * Duplicates the selected rows and optionally adds a new row based on the duplicate.
 * @param {Event} ev The event object (not used in this method).
 * @param {boolean} addRow Whether to add a new row based on the duplication.
 */
    async DuplicateSelected(ev, addRow = false) {
        const originalRows = this.GetSelectedRows();
        const copiedRows = originalRows.map(row => ({ ...row }));
        Toast.Success("Duplicating data!");
        this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforePasted, originalRows, copiedRows).then(() => {
            let index = addRow ? 0 : this.MainSection.Children.length;
            this.AddRowsNo(copiedRows, index).then(list => {
                this.RenderIndex();
                this.ClearSelected();
                Toast.Success("Data duplicated successfully!");
            });
        });
    }

    /**
     * Adds rows at a specified index without clearing existing data.
     * @param {Array<object>} rows Array of row data to add.
     * @param {number} index The index at which to insert the new rows.
     * @returns {Promise<Array<ListViewItem>>} A promise that resolves to an array of added ListViewItem instances.
     */
    async AddRowsNo(rows, index = 0) {
        this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforeCreatedList, rows).then(() => {
            const tasks = rows.map((data, i) => this.AddRow(data, index + i, false));
            Promise.all(tasks).then(results => {
                this.AddNewEmptyRow();
                this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterCreatedList, rows).then(() => {
                    return results;
                });
            });
        });
    }

    /**
     * Updates pagination details based on the current data state.
     */
    RenderIndex() {
        if (this.MainSection.Children.length === 0) {
            return;
        }
        this.MainSection.Children.forEach((row, rowIndex) => {
            if (row.Children.length === 0 || row.FirstChild === null || row.FirstChild.Element === null) {
                return;
            }
            const previous = row.FirstChild.Element.closest('td').previousElementSibling;
            if (previous === null) {
                return;
            }
            const index = this.Paginator.Options.StartIndex + rowIndex;
            previous.innerHTML = index.toString();
            row.Selected = this.SelectedIds.includes(row.Entity[this.IdField]);
            row.RowNo = index;
        });
    }

    /**
     * Resets the order of headers based on their original or defined order.
     */
    ResetOrder() {
        let order = 0;
        this.Header.forEach(header => {
            header.Order = order++;
        });
    }

    /**
     * Handles custom events based on row changes, applying data updates and managing component state.
     * @param {object} rowData The data of the row that triggered the change.
     * @param {ListViewItem} rowSection The ListViewItem corresponding to the row.
     * @param {ObservableArgs} observableArgs Additional arguments or data relevant to the event.
     * @param {EditableComponent} [component=null] Optional component that might be affected by the row change.
     * @returns {Promise<boolean>} A promise that resolves to a boolean indicating success or failure of the event handling.
     */
    async RowChangeHandler(rowData, rowSection, observableArgs, component = null) {
        const tcs = new Promise((resolve, reject) => {
            if (!rowSection.EmptyRow || !this.Editable) {
                this.DispatchEvent(this.Meta.Events, EventType.Change, rowData).then(() => {
                    resolve(false);
                });
            } else {
                this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforeCreated, rowData).then(() => {
                    this.RowData.Data.push(rowData);
                    this.Entity.SetComplexPropValue(this.FieldName, this.RowData.Data);
                    rowSection.FilterChildren(child => true).forEach(child => {
                        child.EmptyRow = false;
                        child.UpdateView(true);
                    });
                    this.EmptySection.Children.clear();
                    this.AddNewEmptyRow();
                    this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterCreated, rowData).then(() => {
                        resolve(true);
                    });
                });
            }
        });
        return tcs;
    }

    /**
 * Removes a row from the ListView by its identifier.
 * @param {string} id The identifier of the row to remove.
 */
    RemoveRowById(id) {
        const row = this.RowData.Data.find(x => x[this.IdField] === id);
        if (row) {
            this.RowData.Data.splice(this.RowData.Data.indexOf(row), 1);
            const listViewItem = this.MainSection.Children.find(x => x.EntityId === id);
            if (listViewItem) {
                listViewItem.Dispose();
            }
        }
    }

    /**
     * Adds a single row to the ListView.
     * @param {object} rowData The data object representing the row.
     * @param {number} index The index at which to insert the new row.
     * @param {boolean} singleAdd Specifies whether to add the row as a single addition.
     * @returns {Promise<ListViewItem>} A promise that resolves to the ListViewItem added.
     */
    async AddRow(rowData, index = 0, singleAdd = true) {
        if (singleAdd) {
            this.RowData.Data.splice(index, 0, rowData);
        }
        await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforeCreated, rowData);
        const row = this.RenderRowData(this.Header, rowData, this.MainSection, index);
        await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterCreated, rowData);
        return row;
    }

    /**
     * Adds multiple rows to the ListView.
     * @param {Array<object>} rows An array of objects to be added as rows.
     * @param {number} index The starting index to add new rows.
     * @returns {Promise<Array<ListViewItem>>} A promise that resolves to an array of ListViewItem instances.
     */
    async AddRows(rows, index = 0) {
        await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.BeforeCreatedList, rows);
        const listItems = [];
        for (let i = 0; i < rows.length; i++) {
            const row = await this.AddRow(rows[i], index + i, false);
            listItems.push(row);
        }
        await this.DispatchCustomEvent(this.Meta.Events, CustomEventType.AfterCreatedList, rows);
        this.AddNewEmptyRow();
        return listItems;
    }

    /**
     * Clears selected rows based on provided criteria or clears all if no criteria provided.
     */
    ClearSelected() {
        this.SelectedIds.forEach(id => {
            const row = this.MainSection.Children.find(x => x.Entity[this.IdField] === id);
            if (row) {
                row.Selected = false;
            }
        });
        this.SelectedIds = [];
        this.LastListViewItem = null;
    }

    /**
     * Updates a specific row in the ListView.
     * @param {object} rowData The data object that represents the row to update.
     * @param {boolean} force Whether to force the update regardless of the current state.
     * @param {Array<string>} fields Specific fields to update, if provided.
     */
    UpdateRow(rowData, force = false, fields = []) {
        const row = this.MainSection.Children.find(x => x.Entity === rowData);
        if (row) {
            row.UpdateView(force, fields);
        }
    }

    /**
     * Renders additional content after rows have been added or updated.
     */
    ContentRendered() {
        this.RenderIndex();
        this.DomLoaded();
        if (this.Editable) {
            this.AddNewEmptyRow();
        }
        if (this.RowData.Data.length === 0 && !this.Editable) {
            this.NoRecordFound();
        } else {
            this.DisposeNoRecord();
        }
        if (this.Editable) {
            this.MainSection.Element.addEventListener('contextmenu', this.BodyContextMenuHandler.bind(this));
        }
    }

    /**
     * Handles no record found scenario, showing a specific message or element.
     */
    NoRecordFound() {
        if (this.MainSection.Children.length > 0) {
            this.MainSection.Children.forEach(child => child.Dispose());
        }
        this.DisposeNoRecord();
        this._noRecord = new Section('div', {
            parentElement: this.Element
        });
        this.AddChild(this._noRecord);
        this._noRecord.Element.addClass('no-records');
        Html.Take(this._noRecord.Element).innerHTML('No record found');
        this.DomLoaded();
    }

    /**
     * Disposes of any 'no record found' elements or messages.
     */
    DisposeNoRecord() {
        if (this._noRecord) {
            this._noRecord.Dispose();
            this._noRecord = null;
        }
    }
}
