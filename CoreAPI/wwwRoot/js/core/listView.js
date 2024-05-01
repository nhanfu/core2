import EditableComponent from "./editableComponent.js";
import { Action } from "./models/action.js";
import { Component } from "./models/component.js";
import { CustomEventType } from "./models/customEventType.js";
import { ActiveStateEnum, AdvSearchVM, MQEvent } from "./models/enum.js";
import { PaginationOptions, Paginator } from "./paginator.js";
import { Utils } from "./utils/utils";

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
        this.DeleteTempIds = new List();
        this.Meta = ui;
        this.Id = ui.Id?.toString();
        this.Name = ui.FieldName;
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
        let arr = listViewItem.FilterChildren(x => !x.Dirty || x.GetValueText()).Select(x => x.FieldName).ToArray();
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

    /**
     * Renders the list view, setting up necessary configurations and data bindings.
     */
    Render() {
        let feature = this.EditForm.Meta;
        this.GridPolicies = this.EditForm.GetElementPolicies(this.Meta.Id);
        this.GeneralPolicies = this.EditForm.Feature.FeaturePolicy.Where(x => x.RecordId.IsNullOrWhiteSpace()).ToArray();
        this.CanWrite = this.CanDo(x => x.CanWrite || x.CanWriteAll);
        Html.Take(this.ParentElement).DataAttr('name', this.FieldName);
        this.AddSections();
        this.SetRowDataIfExists();
        this.EditForm.ResizeListView();
        if (this.Meta.LocalRender) this.LocalRender();
        else this.LoadAllData();
    }

    /**
     * Reloads data for the list view, potentially using cached headers and considering pagination settings.
     * @param {boolean} [cacheHeader=false] Specifies whether headers should be cached.
     * @param {number} [skip=null] Specifies the number of items to skip (for pagination).
     * @param {number} [pageSize=null] Specifies the size of the page to load.
     * @returns {Promise<List<object>>} A promise that resolves to the list of reloaded data objects.
     */
    async ReloadData(cacheHeader = false, skip = null, pageSize = null) {
        if (this.Meta.LocalQuery.HasNonSpaceChar()) {
            this.Meta.LocalData = JSON.parse(this.Meta.LocalQuery);
            this.Meta.LocalRender = true;
        }
        if (this.Meta.LocalRender && this.Meta.LocalData !== null) {
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

    /**
 * Gets the SQL for data retrieval based on the current state of the list view.
 * @param {number} [skip=null] Number of records to skip for pagination.
 * @param {number} [pageSize=null] Page size for pagination.
 * @param {boolean} [cacheMeta=false] Whether to cache meta information.
 * @param {boolean} [count=true] Whether to include a count of total records.
 * @returns {SqlViewModel} The SQL view model with query details.
 */
    GetSql(skip = null, pageSize = null, cacheMeta = false, count = true) {
        let submitEntity = this._preQueryFn?.call(null, this);
        let orderBy = this.AdvSearchVM.OrderBy.HasElement() ? this.AdvSearchVM.OrderBy.Combine(x => {
            let sortDirection = x.OrderbyDirectionId === OrderbyDirection.ASC ? "asc" : "desc";
            return `ds.${x.FieldName} ${sortDirection}`;
        }) : null;
        let basicCondition = this.CalcFilterQuery();
        let fnBtnCondition = this.Wheres.Combine(x => `(${x.Condition})`, " and ");
        let finalCon = [basicCondition, fnBtnCondition].filter(x => !x.IsNullOrWhiteSpace()).combine(" and ");
        return {
            ComId: this.Meta.Id,
            Params: submitEntity ? JSON.stringify(submitEntity) : null,
            OrderBy: orderBy || (this.Meta.OrderBy.IsNullOrWhiteSpace() ? "ds.Id asc" : this.Meta.OrderBy),
            Where: finalCon,
            Count: count,
            SkipXQuery: cacheMeta,
            MetaConn: this.MetaConn,
            DataConn: this.DataConn,
        };
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
            let rows = new List(ds[0]);
            this.Spinner.Hide();
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
}
