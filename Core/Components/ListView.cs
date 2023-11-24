using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class ListView : EditableComponent
    {
        internal int _rowHeight = 26;
        internal int _theadTable = 40;
        internal int _tfooterTable = 35;
        internal int _scrollTable = 10;
        private const string PermissionLoaded = "PermissionLoaded";
        private const string IsOwner = "IsOwner";
        protected static IEnumerable<object> _copiedRows;
        public Action<object> RowClick;
        public Action<object> DblClick;
        protected bool _isFocusCell;
        public bool CanWrite;
        protected bool _firstCache;
        protected Section _noRecord;
        public Action BodyContextMenuShow;
        private HTMLElement _history;
        public Component LastComponentFocus;
        public int LastHeaderId;
        public int _delay = 0;
        public List<string> DeleteTempIds;
        public AdvSearchVM AdvSearchVM { get; set; }
        public bool Editable { get; set; }
        public ListViewItem LastListViewItem { get; set; }
        public ListViewItem LastShiftViewItem { get; set; }
        public int LastIndex { get; set; }
        public HTMLElement LastElementFocus { get; set; }
        public ListViewSearch ListViewSearch { get; set; }
        public Paginator Paginator { get; set; }
        public List<Component> Header { get; set; }
        public ObservableList<object> RowData { get; set; }
        public List<object> FormattedRowData { get; set; }
        internal bool VirtualScroll { get; set; }
        public string Sql { get; set; }
        public string StartRow { get; set; }
        public string EndRow { get; set; }
        public string StartCol { get; set; }
        public string EndCol { get; set; }
        public List<object> CacheData { get; set; } = new List<object>();
        public string FormattedDataSource
        {
            get
            {
                return GetFormattedDataSource(this, DataSourceFilter ?? GuiInfo.DataSourceFilter);
            }
        }

        public static string GetFormattedDataSource(EditableComponent com, string dataSourceFilter)
        {
            if (Utils.IsFunction(dataSourceFilter, out Function fn))
            {
                return fn.Call(com, com, com.EditForm).ToString();
            }
            var editForm = com.FindClosest<EditForm>();
            if (editForm is null)
            {
                return Utils.FormatEntity(dataSourceFilter, com.Entity);
            }

            var pre = Utils.FormatEntity(dataSourceFilter, editForm.Entity);
            var checkContain = pre.Contains(nameof(EditForm) + ".")
                || pre.Contains(nameof(TabEditor) + ".")
                || pre.Contains(nameof(Entity) + ".");
            return Utils.FormatEntity(pre, null, checkContain ? com : com.Entity, notFoundHandler: x => "null");
        }

        public int SelectedIndex { get; set; } = -1;
        public ListViewSection HeaderSection { get; set; }
        public ListViewSection MainSection { get; set; }
        public ListViewSection FooterSection { get; set; }
        public Dictionary<string, List<object>> RefData { get; set; }
        protected int _rowDataChangeAwaiter;
        private static List<object> _originRows;

        public string DataSourceFilter { get; set; }
        public Action OnDeleteConfirmed { get; set; }
        public IEnumerable<ListViewItem> AllListViewItem => MainSection.Children.Cast<ListViewItem>();
        public List<object> UpdatedRows => AllListViewItem.OrderBy(x => x.RowNo).Where(x => x.Dirty).Select(x => x.Entity).Distinct().ToList();

        public PatchUpdate LastUpdate { get; set; }

        public bool IgnoreConfirmHardDelete { get; set; }
        public List<CellSelected> CellSelected = new List<CellSelected>();
        public List<Where> Wheres = new List<Where>();
        public HashSet<string> SelectedIds { get; set; } = new HashSet<string>();
        public string FocusId { get; set; }
        public string EntityFocusId { get; set; }
        public bool ShouldSetEntity { get; set; } = true;

        public event Action<List<Component>> HeaderLoaded;

        public ListView(Component ui, HTMLElement ele = null) : base(ui)
        {
            DeleteTempIds = new List<string>();
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
            Id = ui.Id?.ToString();
            Name = ui.FieldName;
            Header = new List<Component>();
            RowData = new ObservableList<object>();
            RefData = new Dictionary<string, List<object>>();
            AdvSearchVM = new AdvSearchVM
            {
                ActiveState = ActiveStateEnum.Yes,
                OrderBy = LocalStorage.GetItem<List<OrderBy>>("OrderBy" + GuiInfo.Id) ?? new List<OrderBy>()
            };
            DataSourceFilter = ui.DataSourceFilter;
            StopChildrenHistory = true;
            _hasLoadRef = false;
            if (ele != null)
            {
                Resolve(ui, ele);
            }

            _rowHeight = GuiInfo.BodyItemHeight ?? 26;
            _theadTable = GuiInfo.HeaderHeight ?? 40;
            _tfooterTable = GuiInfo.FooterHeight ?? 35;
            _scrollTable = GuiInfo.ScrollHeight ?? 10;
            Window.AddEventListener(GuiInfo.QueueName, RealtimeUpdateListViewItem);
            Utils.IsFunction(GuiInfo.PreQuery, out _preQueryFn);
        }

        internal void RealtimeUpdateListViewItem(dynamic mqEvent)
        {
            object updatedData = mqEvent.detail.Message;
            var listViewItem = MainSection.FilterChildren<ListViewItem>(x => x.Entity[IdField] == updatedData[IdField]).FirstOrDefault();
            if (listViewItem is null) return;
            CacheData.FirstOrDefault(x => x[IdField] == updatedData[IdField]).CopyPropFrom(updatedData);
            listViewItem.Entity.CopyPropFrom(updatedData);
            var arr = listViewItem.FilterChildren<EditableComponent>(x => !x.Dirty || x.GetValueText().IsNullOrWhiteSpace()).Select(x => x.FieldName).ToArray();
            listViewItem.UpdateView(false, arr);
            Client.ExecTaskNoResult(this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterWebsocket, updatedData, listViewItem));
        }

        public void Resolve(Component com, HTMLElement ele = null)
        {
            var txtArea = Document.CreateElement(ElementType.textarea.ToString()) as HTMLTextAreaElement;
            txtArea.InnerHTML = ele.InnerHTML;
            com.FormatEntity = txtArea.Value;
            ele.InnerHTML = null;
        }

        protected static void OrderHeaderGroup(List<Component> headers)
        {
            Component tmp;
            for (int i = 0; i < headers.Count - 1; i++)
            {
                for (int j = i + 2; j < headers.Count; j++)
                {
                    if (headers[i].GroupName.HasAnyChar()
                        && headers[i].GroupName == headers[j].GroupName
                        && headers[i + 1].GroupName != headers[j].GroupName)
                    {
                        tmp = headers[i + 1];
                        headers[i + 1] = headers[j];
                        headers[j] = tmp;
                    }
                }
            }
        }

        public string CalcDatasourse(int viewPortCount, int skip, string count = "true")
        {
            var source = CalcFilterQuery(true);
            if (!source.Contains("?"))
            {
                source += "?";
            }
            source += $"&$skip={skip}&$top={viewPortCount}{(count == "true" ? $"&$count={count}" : "")}";
            return source;
        }

        public virtual async Task<List<object>> ReloadData(bool cacheHeader = false, int? skip = null, int? pageSize = null)
        {
            if (GuiInfo.LocalRender && GuiInfo.LocalData != null)
            {
                SetRowData(GuiInfo.LocalData);
                return GuiInfo.LocalData;
            }
            if (Paginator != null)
            {
                Paginator.Options.PageSize = Paginator.Options.PageSize == 0 ? (GuiInfo.Row ?? 12) : Paginator.Options.PageSize;
            }
            pageSize = pageSize ?? Paginator?.Options?.PageSize ?? GuiInfo.Row ?? 12;
            skip = skip ?? Paginator?.Options?.PageIndex * pageSize ?? 0;
            return await SqlReader(skip, pageSize, cacheHeader);
        }

        public async Task<List<object>> SqlReader(int? skip, int? pageSize, bool cacheHeader = false)
        {
            var sql = GetSql(skip, pageSize, cacheHeader);
            return await CustomQuery(JSON.Stringify(sql));
        }

        public SqlViewModel GetSql(int? skip = null, int? pageSize = null, bool cacheHeader = false)
        {
            var submitEntity = _preQueryFn != null ? _preQueryFn.Call(null, this) : null;
            var orderBy = AdvSearchVM.OrderBy.Any() ? AdvSearchVM.OrderBy.Combine(x =>
            {
                var sortDirection = x.OrderbyDirectionId == OrderbyDirection.ASC ? "asc" : "desc";
                return $"ds.{x.FieldName} {sortDirection}";
            }) : null;
            var basicCondition = CalcFilterQuery(true);
            var fnBtnCondition = Wheres.Combine(x => $"({x.FieldName})", " and ");
            var finalCon = new string[] { basicCondition, fnBtnCondition }
                .Where(x => !x.IsNullOrWhiteSpace()).Combine(" and ");
            var data = new SqlViewModel
            {
                ComId = GuiInfo.Id,
                Entity = submitEntity != null ? JSON.Stringify(submitEntity) : null,
                OrderBy = orderBy ?? (GuiInfo.OrderBy.IsNullOrWhiteSpace() ? "ds.Id asc\n" : GuiInfo.OrderBy),
                Where = finalCon,
                Count = true,
                SkipXQuery = cacheHeader,
            };
            if (skip.HasValue && pageSize.HasValue)
            {
                data.Paging = $"offset {skip} rows\nfetch next {pageSize} rows only";
            }
            return data;
        }

        protected virtual async Task<List<object>> CustomQuery(string submitEntity)
        {
            var ds = await Client.Instance.SubmitAsync<object[][]>(new XHRWrapper
            {
                Value = submitEntity,
                Url = Utils.ComQuery,
                IsRawString = true,
                Method = HttpMethod.POST
            });
            if (ds.Nothing())
            {
                SetRowData(null);
                return null;
            }
            var total = ds.Length > 1 ? ds[1].ToDynamic()[0].total : ds[0].Length;
            var rows = new List<object>(ds[0]);
            Spinner.Hide();
            SetRowData(rows);
            UpdatePagination(total, rows.Count);
            return rows;
        }

        public override void Render()
        {
            var feature = EditForm.Feature;
            var gridPolicies = EditForm.GetElementPolicies(GuiInfo.Id, Utils.ComponentId);
            CanWrite = CanDo(gridPolicies, x => x.CanWrite);
            Html.Take(ParentElement).DataAttr("name", FieldName);
            AddSections();
            SetRowDataIfExists();
            RowData.ListChanged += RowDataChanged;
            EditForm.ResizeListView();
            if (GuiInfo.LocalData.HasElement() && GuiInfo.LocalHeader.HasElement())
            {
                Header = GuiInfo.LocalHeader;
                if (GuiInfo.LocalRender)
                {
                    Rerender();
                }
                else
                {
                    RowData.Data = GuiInfo.LocalData;
                }
                return;
            }
            Task.Run(async () =>
            {
                await LoadAllData();
                RowData.Data = RowData.Data;
            });
        }

        internal virtual void AddSections()
        {
            Html.Take(ParentElement).Div.ClassName("grid-wrapper")
                .ClassName(Editable ? "editable" : string.Empty);
            Element = Html.Context;
            if (GuiInfo.CanSearch)
            {
                Html.Instance.Div.ClassName("grid-toolbar search").End.Render();
            }
            ListViewSearch = new ListViewSearch(GuiInfo);
            AddChild(ListViewSearch);
            Html.Take(Element).Div.ClassName("list-content");
            MainSection = new ListViewSection(Html.Context);
            AddChild(MainSection);
            Html.Instance.EndOf(".list-content");
            RenderPaginator();
        }

        public virtual async Task ApplyFilter(bool searching = true)
        {
            ClearRowData();
            await ReloadData();
        }

        public virtual void ActionFilter()
        {
            ClearRowData();
            Client.ExecTaskNoResult(ReloadData());
        }

        public virtual string CalcFilterQuery(bool searching)
        {
            return ListViewSearch.CalcFilterQuery(null);
        }

        public virtual async Task LoadAllData()
        {
            await ReloadData(cacheHeader: GuiInfo.CanCache);
            await LoadHeader();
        }

        public async Task LoadHeader()
        {
            if (_hasLoadUserSetting) return;
            var columns = await LoadCustomHeaders();
            columns = FilterColumns(columns);
            Header = columns.OrderBy(x => x.Order).ToList();
            ResetOrder();
            HeaderLoaded?.Invoke(columns);
        }

        public void ResetOrder()
        {
            int order = 0;
            Header.ForEach(x =>
            {
                x.Order = order;
                order++;
            });
        }

        protected virtual List<Component> FilterColumns(List<Component> Component)
        {
            var specificComponent = Component.Any(x => x.ComponentId == GuiInfo.Id);
            if (specificComponent)
            {
                Component = Component.Where(x => x.ComponentId == GuiInfo.Id).ToList();
            }
            else
            {
                Component = Component.Where(x => x.ComponentId == null).ToList();
            }

            var permission = EditForm.GetGridPolicies(Component.Select(x => x.Id).ToArray(), Utils.ComponentId);
            var headers = Component
                .Where(header => !header.IsPrivate || permission.Where(x => x.RecordId == header.Id).HasElementAndAll(policy => policy.CanRead))
                .Select(CalcTextAlign).OrderByDescending(x => x.Frozen).ThenBy(x => x.Order).ToList();
            OrderHeaderGroup(headers);
            Header.Clear();
            Header.AddRange(headers);
            Header = Header.Where(x => x != null).ToList();
            return Header;
        }

        protected Component CalcTextAlign(Component header)
        {
            if (header.TextAlign.HasAnyChar())
            {
                var parsed = Enum.TryParse(header.TextAlign, out Enums.TextAlign textAlign);
                if (parsed)
                {
                    header.TextAlignEnum = textAlign;
                }
            }
            return header;
        }

        internal void RenderPaginator()
        {
            if (GuiInfo.LocalRender)
            {
                if (Paginator != null)
                {
                    Paginator.Show = false;
                }
                return;
            }
            if (GuiInfo.Row is null || GuiInfo.Row == 0)
            {
                GuiInfo.Row = 20;
            }

            if (Paginator is null)
            {
                Paginator = new Paginator(new PaginationOptions
                {
                    Total = 0,
                    PageSize = GuiInfo.Row ?? 20,
                    CurrentPageCount = RowData.Data.Count(),
                });
                AddChild(Paginator);
            }
        }

        public virtual ListViewItem RenderRowData(List<Component> headers, object row, Section section, int? index = null, bool emptyRow = false)
        {
            var rowSection = new ListViewItem(ElementType.div)
            {
                EmptyRow = emptyRow,
                Entity = row,
                ParentElement = section.Element,
                PreQueryFn = _preQueryFn,
                ListView = this,
                ListViewSection = section as ListViewSection,
                GuiInfo = GuiInfo,
            };
            if (section is ListViewSection parent)
            {
                rowSection.ListViewSection = parent;
            }
            else if (section is GroupViewItem group)
            {
                rowSection.ListViewSection = group.ListViewSection;
            }
            section.AddChild(rowSection, index);
            rowSection.RenderRowData(headers, row, index, emptyRow);
            return rowSection;
        }

        public virtual void RenderContent()
        {
            FormattedRowData = FormattedRowData.Nothing() ? RowData.Data : FormattedRowData;
            if (FormattedRowData.Nothing())
            {
                return;
            }

            FormattedRowData.SelectForEach((Action<object, int>)((rowData, index) =>
            {
                var rowSection = RenderRowData((List<Component>)this.Header, rowData, MainSection);
            }));
        }

        protected virtual void Rerender()
        {
            DisposeNoRecord();
            Editable = GuiInfo.CanAdd && Header.Any(x => x.Active && !x.Hidden && x.Editable);
            MainSection.DisposeChildren();
            Html.Take(MainSection.Element).Clear();
            RenderContent();

            if (Editable)
            {
                AddNewEmptyRow();
            }
            else if (RowData.Data.Nothing())
            {
                NoRecordFound();
                return;
            }
            MainSection.Element.AddEventListener(EventType.ContextMenu, BodyContextMenuHandler);
            Spinner.Hide();
        }

        protected void DomLoaded()
        {
            if (!GuiInfo.LocalRender)
            {
                Header.ForEach(x => x.LocalData = null);
            }
            DOMContentLoaded?.Invoke();
        }

        protected virtual void RowDataChanged(ObservableListArgs<object> args)
        {
            if (args.Action == ObservableAction.Remove)
            {
                RemoveRowById(args.Item[IdField].As<string>());
                return;
            }
            Window.ClearTimeout(_rowDataChangeAwaiter);
            _rowDataChangeAwaiter = Window.SetTimeout(async () =>
            {
                if (RowData.Data.Nothing() && args.Action == ObservableAction.Render)
                {
                    Rerender();
                    return;
                }
                switch (args.Action)
                {
                    case ObservableAction.Add:
                        await AddRow(args.Item, args.Index);
                        break;
                    case ObservableAction.AddRange:
                        await AddRows(args.ListData, args.Index);
                        break;
                    case ObservableAction.Update:
                        await AddOrUpdateRow(args.Item);
                        break;
                    case ObservableAction.Render:
                        Rerender();
                        break;
                }
                Spinner.Hide();
            });
        }

        protected virtual void SetRowData(List<object> listData)
        {
            RowData._data.Clear();
            var hasElement = listData.HasElement();
            if (hasElement)
            {
                listData.ForEach(RowData._data.Add); // Not to use AddRange because the _data is not always List
            }
            RowDataChanged(new ObservableListArgs<object>
            {
                Action = ObservableAction.Render,
                ListData = RowData._data
            });
            if (Entity != null && ShouldSetEntity)
            {
                Entity.SetComplexPropValue(FieldName, RowData.Data);
            }
        }

        protected void SetRowDataIfExists()
        {
            if (Entity != null && Utils.GetPropValue(Entity, FieldName) is IEnumerable value && value.GetEnumerator().MoveNext())
            {
                RowData["_data"] = value;
            }
        }

        protected virtual async Task<List<Component>> LoadCustomHeaders()
        {
            List<Component> sysSetting = null;
            UserSetting userSetting = null;
            if (GuiInfo.LocalHeader.Nothing())
            {
                var vm = new SqlViewModel
                {
                    ComId = "UserSetting",
                    Action = "GridColumn",
                    Entity = JSON.Stringify(new { ComId = GuiInfo.Id })
                };
                var userSettingTask = await Client.Instance.SubmitAsync<object[][]>(new XHRWrapper
                {
                    Url = Utils.UserSvc,
                    Value = JSON.Stringify(vm),
                    Method = HttpMethod.POST,
                    IsRawString = true
                });
                userSetting = userSettingTask?.ElementAt(0)?.ElementAt(0)?.As<UserSetting>();
                _hasLoadUserSetting = true;
            }
            else
            {
                sysSetting = new List<Component>(GuiInfo.LocalHeader);
            }
            if (userSetting is null)
            {
                return sysSetting;
            }
            else
            {
                var column = userSetting.Value;
                return MergeComponent(sysSetting, JsonConvert.DeserializeObject<List<Component>>(column));
            }
        }

        protected virtual List<Component> MergeComponent(List<Component> sysSetting, List<Component> userSetting)
        {
            if (userSetting.Nothing() || GuiInfo.Focus)
            {
                return sysSetting;
            }
            var Components = new List<Component>();
            var userSettings = userSetting.ToDictionary(x => x.Id);
            sysSetting.ForEach(x =>
            {
                var current = userSettings.GetValueOrDefault(x.Id);
                if (current != null)
                {
                    x.Width = current.Width;
                    x.MaxWidth = current.MaxWidth;
                    x.MinWidth = current.MinWidth;
                    x.Order = current.Order;
                    x.Frozen = current.Frozen;
                }
            });
            return sysSetting;
        }

        public void UpdatePagination(int total, int currentPageCount)
        {
            if (Paginator is null)
            {
                return;
            }
            var options = Paginator.Options;
            options.Total = total;
            options.CurrentPageCount = currentPageCount;
            options.PageNumber = options.PageIndex + 1;
            options.StartIndex = options.PageIndex * options.PageSize + 1;
            options.EndIndex = options.StartIndex + options.CurrentPageCount - 1;
            Paginator.UpdateView();
        }

        public void RealtimeUpdate(ListViewItem rowData, ObservableArgs arg)
        {
            if (EmptyRow)
            {
                EmptyRow = false;
                return;
            }
            if (!GuiInfo.IsRealtime || arg is null)
            {
                return;
            }
            Client.ExecTaskNoResult(rowData.PatchUpdateOrCreate());
        }

        internal virtual async Task RowChangeHandler(object rowData, ListViewItem rowSection, ObservableArgs observableArgs, EditableComponent component = null)
        {
            if (rowSection.EmptyRow && Editable)
            {
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforeCreated, rowData);
                RowData.Data.Add(rowData);
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCreated, rowData);
                Entity.SetComplexPropValue(FieldName, RowData.Data);
                RowAction(x => x.Entity == rowSection.Entity, x =>
                {
                    x.EmptyRow = false;
                    x.FilterChildren(child => true).SelectForeach(child =>
                    {
                        child.EmptyRow = false;
                        child.UpdateView(force: true);
                    });
                });
                AddNewEmptyRow();
            }
            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, rowData);
        }

        public virtual void AddNewEmptyRow()
        {
            // not to add empty row into list view
        }

        public void NoRecordFound()
        {
            if (MainSection.Children.HasElement())
            {
                MainSection.Children.Where(x => x is ListViewSection).ForEach(x => x.DisposeChildren());
            }
            DisposeNoRecord();
            _noRecord = new Section(ElementType.div)
            {
                ParentElement = Element
            };
            AddChild(_noRecord);
            _noRecord.Element.AddClass("no-records");
            Html.Take(_noRecord.Element).IHtml("Không tìm thấy dữ liệu");
            DomLoaded();
        }

        public void BodyContextMenuHandler(Event e)
        {
            e.PreventDefault();
            e.StopPropagation();
            Task.Run(async () =>
            {
                await TbodyContextMenu(e);
            });
        }

        protected void SetRemoteSource(List<object> remoteData, string typeName, Component header)
        {
            var localSource = RefData.GetValueOrDefault(typeName);
            if (localSource is null)
            {
                RefData.Add(typeName, remoteData);
            }
            else
            {
                remoteData.AddRange(localSource);
                localSource.Clear();
                localSource.AddRange(remoteData.DistinctBy(x => x[IdField].As<int>()));
            }
            if (header != null)
            {
                header.LocalData = remoteData;
            }
        }

        protected void SyncMasterData(IEnumerable<object> rows = null, List<Component> headers = null)
        {
            rows = rows ?? RowData.Data;
            headers = headers ?? Header;
            foreach (var header in headers.Where(x => x.ReferenceId.HasAnyChar()))
            {
                if (header.FieldName.IsNullOrWhiteSpace() || header.FieldName.Length <= 2)
                {
                    continue;
                }

                var containId = header.FieldName.Substr(header.FieldName.Length - 2) == IdField;
                if (!containId)
                {
                    continue;
                }

                foreach (var row in rows)
                {
                    var objField = header.FieldName.Substr(0, header.FieldName.Length - 2);
                    var propType = Utils.GetEntity(header.ReferenceId)?.Name;
                    if (propType is null)
                    {
                        continue;
                    }

                    var propVal = Utils.GetPropValue(row, objField);
                    var found = RefData.GetValueOrDefault(propType)?.FirstOrDefault(source =>
                    {
                        return source[IdField].As<int?>() == Utils.GetPropValue(row, header.FieldName).As<int?>();
                    });
                    if (found != null)
                    {
                        row.SetComplexPropValue(objField, found);
                    }
                    else if (propVal != null && found == null)
                    {
                        var source = RefData.GetValueOrDefault(propType);
                        source?.Add(propVal);
                    }
                }
            }
        }

        private Component FormatDataSourceByEntity(Component currentHeader, IEnumerable<Component> allHeaders, IEnumerable<object> entities)
        {
            var entityIds = allHeaders
                .Where(x => x.RefName == currentHeader.RefName)
                .SelectMany(x => GetEntityIds(x, entities)).Distinct();
            if (entityIds.Nothing())
            {
                return null;
            }

            currentHeader.DataSourceOptimized = entityIds.Where(x => x.HasAnyChar())
                .OrderBy(x => x).Select(x => $"'{x}'").Combine();
            return currentHeader;
        }

        private EnumerableInstance<string> GetEntityIds(Component header, IEnumerable<object> entities)
        {
            if (entities.Nothing())
            {
                return Enumerable.Empty<string>();
            }

            return entities.Select((Func<object, string>)(x =>
            {
                var id = x.GetPropValue(header.FieldName)?.ToString();
                if (StringExt.IsNullOrEmpty(id))
                {
                    return null;
                }

                return (string)id;
            })).Where(id => id != null);
        }

        public void DeactivateSelected(object ev = null)
        {
            var confirm = new ConfirmDialog
            {
                Content = "Are you sure to deactivate?"
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                confirm.Dispose();
                await Deactivate();
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.Deactivated, Entity);
            };
        }

        public void SetSelect(object row, bool selected)
        {
            RowAction(x => x.Entity == row, x => x.Selected = selected);
        }

        public void SetSelectAll(bool selected)
        {
            RowAction(x => x.Selected = selected);
        }

        public void HardDeleteSelected(object ev = null)
        {
            Task.Run(async () =>
            {
                var deletedItems = new List<object>();
                if (GuiInfo.ComponentType == nameof(VirtualGrid))
                {
                    deletedItems = await GetRealTimeSelectedRows();
                }
                else
                {
                    deletedItems = GetSelectedRows();
                }
                if (GuiInfo.IgnoreConfirmHardDelete && OnDeleteConfirmed != null)
                {
                    OnDeleteConfirmed.Invoke();
                    return;
                }
                var confirm = new ConfirmDialog();
                confirm.Title = $"Bạn có chắc xóa {deletedItems.Count} dòng dữ liêu không!";
                confirm.Render();
                confirm.YesConfirmed += async () =>
                {
                    if (OnDeleteConfirmed != null)
                    {
                        OnDeleteConfirmed.Invoke();
                        DOMContentLoaded?.Invoke();
                        return;
                    }
                    confirm.Dispose();
                    if (deletedItems.Nothing())
                    {
                        deletedItems = GetFocusedRows().ToList();
                    }
                    await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforeDeleted, deletedItems);
                    await HardDeleteConfirmed(deletedItems);
                    DOMContentLoaded?.Invoke();
                    await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterDeleted, deletedItems);
                };
            });
        }

        public virtual async Task Deactivate()
        {
            var entity = GuiInfo.RefName;
            var selected = GetSelectedRows();
            var ids = selected.Select(x => (string)x[IdField]).ToList();
            var client = new Client(entity);
            var success = await client.DeactivateAsync(ids);
            if (success)
            {
                Toast.Success("Hủy dữ liệu thành công");
                if (AdvSearchVM.ActiveState == ActiveStateEnum.Yes)
                {
                    RemoveRange(selected);
                }
            }
            else
            {
                Toast.Warning("Đã có lỗi xảy ra khi hủy dữ liệu, vui lòng thử lại hoặc liên hệ hỗ trợ!");
            }
        }

        public virtual async Task<IEnumerable<object>> HardDeleteConfirmed(List<object> deleted)
        {
            var entity = GuiInfo.RefName;
            var ids = deleted.Select(x => x[IdField]?.ToString()).Where(x => x != null).ToList();
            var deletes = deleted.Where(x => x[IdField] != null).ToList();
            var removeRow = deleted.Where(x => x[IdField] == null).ToList();
            if (removeRow.Any())
            {
                RemoveRange(removeRow);
            }
            if (deleted.Nothing())
            {
                Toast.Success("Xóa dữ liệu thành công");
                return null;
            }
            if (EditForm.Feature.DeleteTemp)
            {
                var deleteIds = deleted.Where(x => x[IdField] != null).ToList();
                if (deleteIds.Any())
                {
                    RemoveRange(deleteIds);
                }
                DeleteTempIds.AddRange(ids);
                AllListViewItem.Where(x => x.Selected).ToArray().ForEach(x => x.Dispose());
                ClearSelected();
                base.Dirty = true;
                Toast.Success("Xóa tạm thành công");
                return deleted;
            }
            else
            {
                var client = new Client(entity);
                var success = await client.HardDeleteAsync(ids);
                if (success)
                {
                    AllListViewItem.Where(x => x.Selected).ToArray().ForEach(x => x.Dispose());
                    Toast.Success("Xóa dữ liệu thành công");
                    ClearSelected();
                    if (deletes.Any())
                    {
                        RemoveRange(deletes);
                    }
                    return deleted;
                }
                else
                {
                    Toast.Warning("Xóa không thành công");
                }
                return null;
            }
        }

        public virtual void RemoveRange(IEnumerable<object> deleted)
        {
            deleted.SelectForeach(x => RemoveRowById(x[IdField].As<string>()));
        }

        public List<object> GetFocusedRows()
        {
            return AllListViewItem.Where(x => x.Focused).Select(x => x.Entity).ToList();
        }

        public ListViewItem GetItemFocus()
        {
            return AllListViewItem.Where(x => x.Focused).FirstOrDefault();
        }

        public virtual List<object> GetSelectedRows()
        {
            if (LastListViewItem.GroupRow)
            {
                return new List<object>() { LastListViewItem.Entity };
            }
            else
            {
                return MainSection.Children.Where(x => x is ListViewItem item && item.Selected).Select(x => x.Entity).ToList();
            }
        }

        public virtual async Task<List<object>> GetRealTimeSelectedRows()
        {
            return await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetRawListById<object>(SelectedIds.ToList());
        }

        public void PasteSelected(object ev)
        {
            var clipBoard = (Window.Instance as dynamic).navigator.clipboard.readText() as string;
            if (!clipBoard.IsNullOrWhiteSpace() && _copiedRows.Nothing())
            {
                _copiedRows = JSON.Parse(clipBoard) as object[];
            }
            if (_copiedRows.Nothing())
            {
                return;
            }

            Task.Run(async () =>
            {
                Toast.Success("Đang Sao chép liệu !");
                await ComponentExt.DispatchCustomEventAsync(this, GuiInfo.Events, CustomEventType.BeforePasted, _originRows, _copiedRows);
                var index = AllListViewItem.IndexOf(x => x.Selected);
                var list = await AddRowsNo(_copiedRows, index);
                base.Dirty = true;
                base.Focus();
                await ComponentExt.DispatchCustomEventAsync(this, GuiInfo.Events, CustomEventType.AfterPasted, _originRows, _copiedRows);
                if (GuiInfo.IsRealtime)
                {
                    foreach (var item in list)
                    {
                        await item.PatchUpdateOrCreate();
                    }
                    Toast.Success("Sao chép dữ liệu thành công !");
                    base.Dirty = false;
                    ClearSelected();
                }
                else
                {
                    Toast.Success("Sao chép dữ liệu thành công !");
                }
            });
        }

        protected virtual void RenderIndex(int? skip = null)
        {
            if (skip is null)
            {
                skip = Paginator?.Options?.StartIndex ?? 0;
            }
            if (MainSection.Children.Nothing())
            {
                return;
            }
            MainSection.Children.Cast<ListViewItem>().ForEach((row, rowIndex) =>
            {
                if (row.Children.Nothing() || row.FirstChild is null || row.FirstChild.Element is null)
                {
                    return;
                }
                var previous = row.FirstChild.Element.Closest("td").PreviousElementSibling;
                if (previous is null)
                {
                    return;
                }
                var index = skip + rowIndex;
                previous.InnerHTML = index.ToString();
                row.Selected = SelectedIds.Contains(row.Entity[IdField]);
                row.RowNo = index.Value;
            });
        }

        public virtual void DuplicateSelected(Event ev, bool addRow = false)
        {
            var originalRows = GetSelectedRows();
            var copiedRows = ReflectionExt.CopyRowWithoutId(originalRows);
            if (copiedRows.Nothing() || !CanWrite)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                Toast.Success("Đang Sao chép liệu !");
                await ComponentExt.DispatchCustomEventAsync(this, GuiInfo.Events, CustomEventType.BeforePasted, originalRows, copiedRows);
                var index = AllListViewItem.IndexOf(x => x.Selected);
                if (addRow)
                {
                    if (GuiInfo.TopEmpty)
                    {
                        index = 0;
                    }
                    else
                    {
                        index = AllListViewItem.LastOrDefault().RowNo;
                    }
                }
                var list = await AddRowsNo(copiedRows, index);
                base.Dirty = true;
                base.Focus();
                await ComponentExt.DispatchCustomEventAsync(this, GuiInfo.Events, CustomEventType.AfterPasted, originalRows, copiedRows);
                RenderIndex();
                ClearSelected();
                if (GuiInfo.IsRealtime)
                {
                    foreach (var item in list)
                    {
                        await item.PatchUpdateOrCreate();
                    }
                    Toast.Success("Sao chép dữ liệu thành công !");
                    base.Dirty = false;
                }
                else
                {
                    Toast.Success("Sao chép dữ liệu thành công !");
                }
            });
        }

        public virtual async Task AddOrUpdateRow(object rowData, bool singleAdd = true, bool force = false, params string[] fields)
        {
            var existRowData = MainSection
                .FilterChildren(x => x is ListViewItem && x.Entity == rowData)
                .Cast<ListViewItem>().FirstOrDefault();
            if (existRowData is null)
            {
                if (!singleAdd)
                {
                    RowData.Data.Add(rowData);
                }
                await AddRow(rowData, RowData.Data.Count - 1, singleAdd);
                return;
            }
            if (existRowData.EmptyRow)
            {
                RowData.Data.Add(rowData);
            }
            RowAction(x => x.Entity == rowData, x =>
            {
                existRowData.Entity.CopyPropFrom(rowData);
                x.EmptyRow = false;
                x.UpdateView(force: force, fields);
                x.Dirty = true;
            });
            if (singleAdd)
            {
                FinalAddOrUpdate();
            }
        }

        protected void FinalAddOrUpdate()
        {
            AddNewEmptyRow();
        }

        public virtual async Task AddOrUpdateRows(IEnumerable<object> rows)
        {
            RowData.Data.AddRange(rows);
            await rows.ForEachAsync(async row => await AddOrUpdateRow(row, false));
            FinalAddOrUpdate();
        }

        public virtual Task<ListViewItem> AddRow(object rowData, int index = 0, bool singleAdd = true)
        {
            var tcs = new TaskCompletionSource<ListViewItem>();
            DisposeNoRecord();
            // if (MainSection.FirstOrDefault(x => x.Entity == rowData) is ListViewItem exists)
            // {
            //     return exists;
            // }

            if (singleAdd)
            {
                RowData.Data.Add(rowData);
            }
            Client.ExecTaskNoResult(this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforeCreated, rowData), () => {
                var row = RenderRowData(Header, rowData, MainSection, index);
                tcs.TrySetResult(row);
                Client.ExecTaskNoResult(this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCreated, rowData));
            });
            return tcs.Task;
        }

        public void DisposeNoRecord()
        {
            _noRecord?.Dispose();
            _noRecord = null;
        }

        public virtual async Task<List<ListViewItem>> AddRows(IEnumerable<object> rows, int index = 0)
        {
            if (index < 0)
            {
                index = 0;
            }
            var indextemp = index;
            rows.SelectForeach(row =>
            {
                if (RowData.Data is IList)
                {
                    RowData.Data.Insert(indextemp, row);
                }
                else
                {
                    RowData.Data.Add(row);
                }
                indextemp++;
            });
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforeCreatedList, rows);
            var listItem = new List<ListViewItem>();
            indextemp = index;
            await rows.AsEnumerable().Reverse().ForEachAsync(async data =>
            {
                listItem.Add(await AddRow(data, indextemp, false));
                indextemp++;
            });
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCreatedList, rows);
            AddNewEmptyRow();
            return listItem;
        }

        public virtual async Task<List<ListViewItem>> AddRowsNo(IEnumerable<object> rows, int index = 0)
        {
            if (index < 0)
            {
                index = 0;
            }
            var indextemp = index;
            rows.SelectForeach(row =>
            {
                if (RowData.Data is IList)
                {
                    RowData.Data.Insert(indextemp, row);
                }
                else
                {
                    RowData.Data.Add(row);
                }
                indextemp++;
            });
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforeCreatedList, rows);
            var listItem = new List<ListViewItem>();
            indextemp = index;
            await rows.ForEachAsync(async data =>
            {
                listItem.Add(await AddRow(data, indextemp, false));
                indextemp++;
            });
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCreatedList, rows);
            AddNewEmptyRow();
            return listItem;
        }

        public virtual void RemoveRowById(string id)
        {
            var row = RowData.Data.FirstOrDefault(x => x[IdField]?.ToString() == id);
            if (row != null)
            {
                RowData.Data.Remove(row);
            }
            AllListViewItem.FirstOrDefault(x => x.EntityId == id)?.Dispose();
        }

        public virtual void RemoveRow(object row)
        {
            if (row is null)
            {
                return;
            }

            RowData.Data.Remove(row);
            MainSection.FirstOrDefault(x => x.Entity == row)?.Dispose();
        }

        /// <summary>
        /// Updating row data
        /// </summary>
        /// <param name="rowData">The row object to update</param>
        /// <param name="fieldName">Left this default to update all cells</param>
        public virtual void UpdateRow(object rowData, bool force = false, params string[] fieldName)
        {
            RowAction(
                row => row.Entity == rowData,
                row => row.Children.Where(x => fieldName.Nothing() || fieldName.Contains(x.FieldName)).ForEach(x => x.UpdateView(force))
            );
        }

        public void ClearRowData()
        {
            RowData?.Data?.Clear();
            RowAction(x => !x.EmptyRow, x => x.Dispose());
            MainSection.Element.InnerHTML = null;
            if (Entity is null || Parent is SearchEntry)
            {
                return;
            }
            if (ShouldSetEntity)
            {
                Entity?.SetComplexPropValue(FieldName, RowData.Data);
            }
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            if (!Editable)
            {
                if (force)
                {
                    Task.Run(async () => await ListViewSearch.RefershListView());
                }
            }
            else
            {
                RowAction(row => !row.EmptyRow, row => row.UpdateView(force, dirty, componentNames));
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task<bool> ValidateAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            ValidationResult.Clear();
            return ValidateRequired();
        }

        private bool ValidateRequired()
        {
            if (Element is null || ValidationRules.Nothing())
            {
                return true;
            }

            if (!ValidationRules.ContainsKey(ValidationRule.Required))
            {
                Element.RemoveAttribute(ValidationRule.Required);
                return true;
            }
            var requiredRule = ValidationRules[ValidationRule.Required];
            Element.SetAttribute(ValidationRule.Required, true.ToString());
            if (RowData.Data.HasElement())
            {
                ValidationResult.Remove(ValidationRule.Required);
                return true;
            }
            else
            {
                ValidationResult.TryAdd(ValidationRule.Required, string.Format(requiredRule.Message, LangSelect.Get(GuiInfo.Label), Entity));
                return false;
            }
        }

        public void CopySelected(object ev)
        {
            _originRows = GetSelectedRows();
            _copiedRows = ReflectionExt.CopyRowWithoutId(_originRows);
            (Window.Instance as dynamic).navigator.clipboard.writeText(JSON.Stringify(_copiedRows));
            Task.Run(async () =>
            {
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCopied, _originRows, _copiedRows);
            });
        }

        private void DisposeHistory()
        {
            _history.Remove();
        }

        protected void ViewHistory(object ev)
        {
            var currentItem = GetSelectedRows().LastOrDefault();
            Html.Take(EditForm.Element).Div.ClassName("backdrop")
            .Style("align-items: center;").Escape((e) => Dispose());
            _history = Html.Context;
            Html.Instance.Div.ClassName("popup-content confirm-dialog").Style("top: 0;")
                .Div.ClassName("popup-title").InnerHTML("Xem lịch sử")
                .Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                    .Event(EventType.Click, DisposeHistory)
                .EndOf(".popup-title")
                .Div.ClassName("popup-body scroll-content");
            var body = Html.Context;
            var com = new Component();
            com.Id = Guid.NewGuid().ToString();
            com.FieldName = nameof(AdvSearchVM.Conditions);
            com.Column = 4;
            com.ReferenceId = Utils.GetEntity(nameof(Models.History)).Id;
            com.RefName = nameof(Models.History);
            com.Reference = new Entity { Name = nameof(Models.History), Namespace = typeof(Component).Namespace + "." };
            com.DataSourceFilter = $"?$orderby=Id desc&$filter=Active eq true and EntityId eq '{GuiInfo.ReferenceId}' and RecordId eq '{currentItem[IdField]}'";
            var _filterGrid = new GridView(com);
            _filterGrid.GuiInfo.LocalHeader = new List<Component>
            {
                     new Component
                    {
                        Id = 1 .ToString(),
                        EntityId = Utils.GetEntity(nameof(Models.History)).Id,
                        FieldName = nameof(Models.History.InsertedBy),
                        ShortDesc = "Người thay đổi",
                        ReferenceId=Utils.GetEntity(nameof(User)).Id,
                        RefName=nameof(User),
                        FormatData="{FullName}",
                        Active = true,
                        ComponentType = "Dropdown",
                        MaxWidth = "100px",
                        MinWidth = "100px",
                    },
                     new Component
                    {
                        Id = 2 .ToString(),
                        EntityId = Utils.GetEntity(nameof(Models.History)).Id,
                        FieldName = nameof(Models.History.InsertedDate),
                        ShortDesc = "Ngày thay đổi",
                        Active = true,
                        FormatData = "{0: dd/MM/yyyy HH:mm}",
                        ComponentType = "Datepicker",
                        TextAlign="left",
                        MaxWidth = "150px",
                        MinWidth = "150px",
                    },
                    new Component
                    {
                        Id = 4 .ToString(),
                        EntityId = Utils.GetEntity(nameof(Models.History)).Id,
                        FieldName = nameof(Models.History.TextHistory),
                        ShortDesc = "Dữ liệu thay đổi",
                        Active = true,
                        ComponentType = "Label",
                        MaxWidth = "700px",
                        MinWidth = "700px",
                    }
                };
            _filterGrid.ParentElement = body;
            Parent.AddChild(_filterGrid);
        }

        private void SecurityRows(object arg)
        {
            var selectedRowIds = GetSelectedRows().Where(x => x[IsOwner].As<bool?>() == true)
                .Select(x => x[IdField]?.ToString()).ToArray();
            var security = new SecurityBL
            {
                Entity = new SecurityVM { RecordIds = selectedRowIds, EntityId = GuiInfo.ReferenceId },
                ParentElement = TabEditor.Element
            };
            TabEditor.AddChild(security);
        }

        internal bool CanDo(IEnumerable<FeaturePolicy> gridPolicies, Func<FeaturePolicy, bool> permissionPredicate)
        {
            var featurePolicy = EditForm.Feature.FeaturePolicy.Where(x => x.EntityId == null).Any(permissionPredicate);
            if (!featurePolicy)
            {
                return false;
            }
            var Component = gridPolicies.Any();
            if (!Component)
            {
                return true;
            }
            return gridPolicies.Any(permissionPredicate);
        }

        protected List<FeaturePolicy> RecordPolicy = new List<FeaturePolicy>();
        public async Task TbodyContextMenu(Event e)
        {
            ContextMenu.Instance.MenuItems.Clear();
            BodyContextMenuShow?.Invoke();
            if (Disabled)
            {
                return;
            }
            SetSelected(e);
            var selectedRows = GetSelectedRows();
            if (selectedRows.Nothing())
            {
                return;
            }
            var ctxMenu = ContextMenu.Instance;
            var gridPolicies = EditForm.GetElementPolicies(GuiInfo.Id, Utils.ComponentId);
            var canWrite = GuiInfo.CanAdd && CanWrite;
            await RenderViewMenu();
            RenderCopyPasteMenu(canWrite);
            RenderEditMenu(selectedRows, gridPolicies);
            await RenderShareMenu(selectedRows, gridPolicies);
            ctxMenu.Top = e.Top();
            ctxMenu.Left = e.Left();
            ctxMenu.Render();
            Element.AppendChild(ctxMenu.Element);
            ctxMenu.Element.Style.Position = Position.absolute.ToString();
        }

        private void SetSelected(Event e)
        {
            var target = e.Target as HTMLElement;
            var rawRow = target.Closest(ElementType.tr.ToString());
            var currentRow = this.FirstOrDefault(x => x.Element == rawRow) as ListViewItem;
            if (!(currentRow is GroupViewItem) || GuiInfo.GroupReferenceId != null)
            {
                if (SelectedIds.Count == 1)
                {
                    ClearSelected();
                }
                currentRow.Selected = true;
                LastListViewItem = currentRow;
                SelectedIndex = currentRow.ListViewSection.Children.IndexOf(currentRow);
            }
        }

        public async Task RenderViewMenu()
        {
            var targetRef = await new Client(nameof(EntityRef)).GetRawList<EntityRef>($"?$filter=ComId eq '{GuiInfo.Id}'");
            if (targetRef.Nothing())
            {
                return;
            }
            var menuItems = targetRef.Select(x => new ContextMenuItem
            {
                Text = x.MenuText,
                Click = (arg) => OpenFeature(x),
            }).ToList();
            ContextMenu.Instance.MenuItems.Add(new ContextMenuItem
            {
                Icon = "fa fal fa-ellipsis-h",
                Text = "Dữ liệu liên quan",
                MenuItems = menuItems
            });
        }
        public bool _hasLoadRef { get; set; }
        protected Function _preQueryFn;
        internal string FeatureId;
        protected bool _hasLoadUserSetting;

        private void OpenFeature(EntityRef e)
        {
            var instance = TabEditor.Tabs.Where(x => x.Name == e.ViewClass).FirstOrDefault();
            if (instance != null)
            {
                instance.Focus();
                Filter(instance, e);
                _hasLoadRef = true;
                return;
            }
            _hasLoadRef = false;
            Client.ExecTask(ComponentExt.LoadFeatureByName(e.ViewClass), currentFeature =>
            {
                var id = currentFeature.Name + currentFeature.Id;
                Type type;
                if (currentFeature.ViewClass != null)
                {
                    type = Type.GetType(currentFeature.ViewClass);
                }
                else
                {
                    type = typeof(TabEditor);
                }
                instance = Activator.CreateInstance(type) as TabEditor;
                instance.Name = currentFeature.Name;
                instance.Id = id;
                instance.Icon = currentFeature.Icon;
                instance.Feature = currentFeature;
                instance.Render();
                instance.DOMContentLoaded += () =>
                {
                    var gridView1 = instance.FilterChildren<GridView>().FirstOrDefault(x => x.GuiInfo.Id == e.TargetComId);
                    gridView1.DOMContentLoaded += () =>
                    {
                        if (_hasLoadRef)
                        {
                            return;
                        }
                        Filter(instance, e);
                        _hasLoadRef = true;
                    };
                };
            });
        }

        private void Filter(TabEditor fe, EntityRef e)
        {
            var gridView1 = fe.FilterChildren<GridView>().FirstOrDefault(x => x.GuiInfo.Id == e.TargetComId);
            if (gridView1 is null)
            {
                return;
            }
            gridView1.CellSelected.Clear();
            gridView1.AdvSearchVM.Conditions.Clear();
            gridView1.ListViewSearch.EntityVM.StartDate = null;
            gridView1.ListViewSearch.EntityVM.EndDate = null;
            Client.ExecTask(GetRealTimeSelectedRows(), (Action<List<object>>)(selecteds =>
            {
                var com = Enumerable.FirstOrDefault<Component>(gridView1.Header, (Func<Component, bool>)(x => x.FieldName == e.TargetFieldName));
                var cellSelecteds = selecteds.Select(selected =>
                {
                    return new CellSelected()
                    {
                        FieldName = e.TargetFieldName,
                        FieldText = com.ShortDesc,
                        ComponentType = com.ComponentType,
                        Value = selected.GetPropValue(e.FieldName).ToString(),
                        ValueText = selected.GetPropValue(e.FieldName).ToString(),
                        Operator = (int)OperatorEnum.In,
                        OperatorText = "Chứa",
                        Logic = LogicOperation.Or,
                        IsSearch = true,
                        Group = true
                    };
                });
                gridView1.CellSelected.AddRange(cellSelecteds);
                gridView1.ActionFilter();
            }));
        }

        public virtual void RenderCopyPasteMenu(bool canWrite)
        {
            //
        }

        private void RenderEditMenu(List<object> selectedRows, IEnumerable<FeaturePolicy> gridPolicies)
        {
            ContextMenu.Instance.MenuItems.Add(new ContextMenuItem
            {
                Icon = "fal fa-history",
                Text = "Xem lịch sử",
                Click = ViewHistory,
            });
            var canDeactivate = CanDo(gridPolicies, x => x.CanDeactivate);
            if (canDeactivate)
            {
                ContextMenu.Instance.MenuItems.Add(new ContextMenuItem
                {
                    Icon = "mif-unlink",
                    Text = "Hủy (không xóa)",
                    Click = DeactivateSelected,
                });
            }
            var isOwner = selectedRows.All(x => Utils.IsOwner(x, true));
            var canDelete = CanDo(gridPolicies, x => x.CanDelete && isOwner || x.CanDeleteAll);
            if (canDelete)
            {
                ContextMenu.Instance.MenuItems.Add(new ContextMenuItem
                {
                    Icon = "fa fa-trash",
                    Text = "Xóa dữ liệu",
                    Click = HardDeleteSelected,
                });
            }
        }

        private async Task RenderShareMenu(List<object> selectedRows, IEnumerable<FeaturePolicy> gridPolicies)
        {
            var noPolicyRows = selectedRows.Where(x =>
            {
                var hasPolicy = RecordPolicy.Any(f => f.EntityId == GuiInfo.ReferenceId && f.RecordId == x[IdField]?.ToString());
                var loaded = x[PermissionLoaded].As<bool?>();
                return !(hasPolicy || loaded == true);
            });
            var noPolicyRowIds = noPolicyRows.Select(x => x[IdField].As<string>()).ToArray();
            var rowPolicy = await ComponentExt.LoadRecordPolicy(noPolicyRowIds, GuiInfo.ReferenceId);
            rowPolicy.ForEach(RecordPolicy.Add);
            noPolicyRows.ForEach(x => x[PermissionLoaded] = true);
            var ownedRecords = selectedRows.Where(x =>
            {
                var isOwner = Utils.IsOwner(x);
                x[IsOwner] = isOwner || rowPolicy.Any(policy => policy.CanShare && x[IdField].As<string>() == policy.RecordId);
                return isOwner;
            }).Select(x => x[IdField].As<int>()).ToList();
            var canShare = CanDo(gridPolicies, x => x.CanShare) && ownedRecords.Any();
            if (!canShare)
            {
                return;
            }
            ContextMenu.Instance.MenuItems.Add(new ContextMenuItem
            {
                Icon = "mif-security",
                Text = "Bảo mật & Phân quyền",
                Click = SecurityRows,
            });
        }

        public void MoveDown()
        {
            ClearSelected();
            if (SelectedIndex == -1 || SelectedIndex == AllListViewItem.Count())
            {
                SelectedIndex = 0;
            }
            RowAction(x => x.Selected = true, false);
        }

        public virtual void MoveUp()
        {
            ClearSelected();
            if (SelectedIndex <= 0 || SelectedIndex == AllListViewItem.Count())
            {
                SelectedIndex = AllListViewItem.Count() - 1;
            }
            RowAction(x => x.Selected = true, true);
        }

        public void ClearSelected()
        {
            RowAction(x => x.Selected = false);
            SelectedIds.Clear();
            LastListViewItem = null;
        }

        public void RowAction(Action<ListViewItem> action, bool sub)
        {
            var row = AllListViewItem.FirstOrDefault(x => x.RowNo == (sub ? (SelectedIndex - 1) : (SelectedIndex + 1)));
            if (row is null)
            {
                return;
            }
            if (sub)
            {
                SelectedIndex--;
            }
            else
            {
                SelectedIndex++;
            }
            if (row.GroupRow)
            {
                row = AllListViewItem.FirstOrDefault(x => x.RowNo == (sub ? (SelectedIndex - 1) : (SelectedIndex + 1)));
            }
            action.Invoke(row);
        }

        public void RowAction(Action<ListViewItem> action)
        {
            AllListViewItem.ToList().ForEach(action);
        }

        public void RowAction(Func<ListViewItem, bool> predicate, Action<ListViewItem> action)
        {
            AllListViewItem
                .Where(x => predicate == null || predicate(x))
                .ToList().ForEach(x => action.Invoke(x));
        }

        public bool IsRowDirty(object row)
        {
            return GetListViewItems(row).Any(x => x.Dirty);
        }

        public IEnumerable<ListViewItem> GetListViewItems(object row)
        {
            return AllListViewItem.Where(x => x.Entity[IdField] == row[IdField]);
        }

        public virtual async Task<List<object>> BatchUpdate(bool updateView = false)
        {
            if (!Dirty)
            {
                return null;
            }
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforePatchCreate, Entity, null, this);
            var updateTasks = new List<Task<dynamic>>();
            foreach (var item in UpdatedRows)
            {
                if (item[IdField]?.ToString() != null)
                {
                    updateTasks.Add(new Client(GuiInfo.Reference.Name).UpdateAsync(item));
                }
                else
                {
                    updateTasks.Add(new Client(GuiInfo.Reference.Name).CreateAsync(item));
                }
            }
            await Task.WhenAll(updateTasks);
            var updatedRows = updateTasks.Select(x => x.Result).ToList();
            InternalUpdateRows(updateView, updatedRows);
            return updatedRows;
        }

        private void InternalUpdateRows(bool updateView, List<object> updatedRows)
        {
            UpdatedRows.CopyPropFrom(updatedRows);
            if (updateView)
            {
                RowAction(row => row.Dirty && !row.EmptyRow, row =>
                {
                    row.UpdateView();
                    row.Element.AddClass("new-row");
                });
            }
            Dirty = false;
        }

        public override StringBuilder BuildTextHistory(StringBuilder builder, HashSet<object> visited = null)
        {
            if (builder is null)
            {
                builder = new StringBuilder();
            }
            if (visited is null)
            {
                visited = new HashSet<object>();
            }
            if (visited.Contains(this))
            {
                return builder;
            }
            if (!Editable)
            {
                return builder;
            }
            visited.Add(this);
            var dirty = AllListViewItem.Where(x => x.Dirty).DistinctBy(x => x.Entity).ToArray();
            if (dirty.HasElement())
            {
                string label = GuiInfo.Label;
                if (GuiInfo.Label.IsNullOrWhiteSpace())
                {
                    var section = this.FindClosest<Section>(x => x.ComponentGroup?.Label != null);
                    label = section?.ComponentGroup?.Label;
                }
                builder.Append($"{LangSelect.Get(label)}:{Utils.NewLine}");
                dirty.ForEach(x => x.BuildTextHistory(builder));
            }
            return builder;
        }

        internal int GetRowCountByHeight(double scrollTop)
        {
            return (int)Math.Round(scrollTop / _rowHeight, 0, MidpointRounding.TowardsZero);
        }

        internal void SetRowHeight()
        {
            var existRow = AllListViewItem.FirstOrDefault()?.Element;
            if (existRow != null)
            {
                _rowHeight = existRow.ScrollHeight > 0 ? existRow.ScrollHeight : _rowHeight;
            }
        }

        public override void Dispose()
        {
            Window.RemoveEventListener("", RealtimeUpdateListViewItem);
            base.Dispose();
        }
    }
}
