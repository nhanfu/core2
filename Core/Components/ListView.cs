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
using System.Threading;
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
        private const string CmdUrl = "Cmd";
        protected static List<object> _copiedRows;
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
        public AdvSearchVM AdvSearchVM { get; set; }
        public bool Editable { get; set; }
        public ListViewItem LastListViewItem { get; set; }
        public ListViewItem LastShiftViewItem { get; set; }
        public int LastIndex { get; set; }
        public HTMLElement LastElementFocus { get; set; }
        public ListViewSearch ListViewSearch { get; set; }
        public Paginator Paginator { get; set; }
        public List<SortedField> SortedField { get; set; }
        public List<GridPolicy> Header { get; set; }
        public List<GridPolicy> BasicHeader { get; set; }
        public List<GridPolicy> BasicHeaderSearch { get; set; }
        public Dictionary<int, Component> HeaderComponentMap { get; set; }
        public ObservableList<object> RowData { get; set; }
        public List<object> FormattedRowData { get; set; }
        internal bool VirtualScroll { get; set; }
        public string Sql { get; set; }
        public int StartRow { get; set; }
        public int EndRow { get; set; }
        public int StartCol { get; set; }
        public int EndCol { get; set; }
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
        public List<object> UpdatedRows => AllListViewItem.Where(x => x.Dirty).Select(x => x.Entity).Distinct().ToList();

        public PatchUpdate LastUpdate { get; set; }

        public bool IgnoreConfirmHardDelete { get; set; }
        public List<CellSelected> CellSelected = new List<CellSelected>();
        public List<Where> Wheres = new List<Where>();
        public HashSet<int> SelectedIds { get; set; } = new HashSet<int>();
        public int? FocusId { get; set; }
        public int? EntityFocusId { get; set; }
        public bool ShouldSetEntity { get; set; } = true;

        public event Action<List<GridPolicy>> HeaderLoaded;

        public ListView(Component ui, HTMLElement ele = null) : base(ui)
        {
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
            Id = ui.Id.ToString();
            Name = ui.FieldName;
            Header = new List<GridPolicy>();
            RowData = new ObservableList<object>();
            RefData = new Dictionary<string, List<object>>();
            AdvSearchVM = new AdvSearchVM
            {
                ActiveState = ActiveStateEnum.Yes,
                OrderBy = LocalStorage.GetItem<List<OrderBy>>("OrderBy" + GuiInfo.Id) ?? new List<OrderBy>()
            };
            SortedField = AdvSearchVM.OrderBy.Select(x => new SortedField()
            {
                Field = x.Field.FieldName,
                Desc = x.OrderbyOptionId == OrderbyOption.DESC ? true : false,
                Com = x.Field,
            }).ToList();
            DataSourceFilter = ui.DataSourceFilter;
            StopChildrenHistory = true;
            _hasLoadRef = false;
            if (ele != null)
            {
                Resolve(ui, ele);
            }
            Utils.IsFunction(GuiInfo.PreQuery, out _preQueryFn);
            _rowHeight = GuiInfo.BodyItemHeight ?? 26;
            _theadTable = GuiInfo.HeaderHeight ?? 40;
            _tfooterTable = GuiInfo.FooterHeight ?? 35;
            _scrollTable = GuiInfo.FooterHeight ?? 10;
            if (GuiInfo.IsRealtime)
            {
                EditForm.NotificationClient?.AddListener(GuiInfo.ReferenceId.Value, RealtimeUpdateListViewItem);
            }
        }

        internal void RealtimeUpdateListViewItem(object updatedData)
        {
            Task.Run(async () =>
            {
                var listViewItem = MainSection.FilterChildren<ListViewItem>(x => x.Entity[IdField] == updatedData[IdField]).FirstOrDefault();
                if (listViewItem != null)
                {
                    await LoadMasterData(new object[] { updatedData });
                    if (GuiInfo.ComponentType == nameof(VirtualGrid))
                    {
                        CacheData.FirstOrDefault(x => x[IdField] == updatedData[IdField]).CopyPropFrom(updatedData);
                    }
                    listViewItem.Entity.CopyPropFrom(updatedData);
                    var arr = listViewItem.FilterChildren<EditableComponent>(x => !x.Dirty || x.GetValueText().IsNullOrWhiteSpace()).Select(x => x.GuiInfo.FieldName).ToArray();
                    listViewItem.UpdateView(false, arr);
                    await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterWebsocket, updatedData, listViewItem);
                }
            });
        }

        public void Resolve(Component com, HTMLElement ele = null)
        {
            var txtArea = Document.CreateElement(ElementType.textarea.ToString()) as HTMLTextAreaElement;
            txtArea.InnerHTML = ele.InnerHTML;
            com.FormatEntity = txtArea.Value;
            ele.InnerHTML = null;
        }

        protected static void OrderHeaderGroup(List<GridPolicy> headers)
        {
            GridPolicy tmp;
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

        public virtual async Task<List<object>> ReloadData(string dataSource = null, bool cache = false, int? skip = null, int? pageSize = null, bool search = false)
        {
            if (GuiInfo.LocalRender && GuiInfo.LocalData != null)
            {
                SetRowData(GuiInfo.LocalData);
                return GuiInfo.LocalData;
            }
            if (_preQueryFn != null)
            {
                var submitEntity = _preQueryFn.Call(null, this);
                return await CustomQuery(new SqlViewModel
                {
                    CmdId = GuiInfo.Id,
                    CmdType = "Query",
                    Entity = JSON.Stringify(submitEntity),
                });
            }

            dataSource = dataSource.IsNullOrEmpty() ? CalcFilterQuery(true) : dataSource;
            if (dataSource.IsNullOrWhiteSpace())
            {
                dataSource = "?$filter=true";
            }

            if (Paginator != null)
            {
                Paginator.Options.PageSize = Paginator.Options.PageSize == 0 ? (GuiInfo.Row ?? 12) : Paginator.Options.PageSize;
            }
            pageSize = pageSize ?? Paginator?.Options?.PageSize ?? GuiInfo.Row ?? 12;
            skip = skip ?? Paginator?.Options?.PageIndex * pageSize ?? 0;
            if (!dataSource.Contains("?"))
            {
                dataSource += "?";
            }

            var pagingQuery = dataSource + $"&$skip={skip}&$top={pageSize}&$count=true";
            OdataResult<object> result;
            var val = (Entity?.GetComplexPropValue(GuiInfo.FieldName) as IEnumerable<object>)?.ToList();
            if (GuiInfo.CanCache && val != null && val.Any() && !search)
            {
                result = new OdataResult<object>
                {
                    Value = val,
                    Odata = new Odata { Count = val.Count }
                };
            }
            else
            {
                result = await new Client(GuiInfo.RefName, GuiInfo.Reference != null ? GuiInfo.Reference.Namespace : null).GetList<object>(pageSize > 0 ? pagingQuery : dataSource, true);
            }
            Sql = result.Sql;
            UpdatePagination(result.Odata.Count ?? result.Value.Count, result.Value.Count);
            await LoadMasterData(result.Value);
            SetRowData(result.Value);
            if (result.Odata.Count > 0 && result.Value.Nothing())
            {
                if (Paginator != null)
                {
                    Paginator.Options.PageIndex = 0;
                }
                await ReloadData(dataSource, cache: cache);
            }
            Spinner.Hide();
            return result.Value;
        }

        protected virtual async Task<List<object>> CustomQuery(object submitEntity)
        {
            var ds = await new Client(nameof(User)).PostAsync<object[][]>(submitEntity, CmdUrl);
            if (ds.Nothing())
            {
                SetRowData(null);
                return null;
            }
            var total = ds.Length > 1 ? ds[1].ToDynamic()[0].total : ds[0].Length;
            var rows = new List<object>(ds[0]);
            await LoadMasterData(rows);
            SetRowData(rows);
            UpdatePagination(total, rows.Count);
            return rows;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "<Pending>")]
        public virtual async Task<bool> LoadCache(string dataSource, bool cache, int pageSize)
        {
            if (Entity is null)
            {
                return false;
            }

            var rows = Entity?.GetComplexPropValue(GuiInfo.FieldName) as IEnumerable;
            if (rows is null)
            {
                return false;
            }

            var enumerator = rows.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return false;
            }

            var addNew = enumerator.Current[IdField].As<int>() == 0;
            if (cache || addNew)
            {
                RowData["_data"] = rows;
                return true;
            }
            return false;
        }

        public override void Render()
        {
            var feature = EditForm.Feature;
            var gridPolicies = EditForm.GetElementPolicies(GuiInfo.Id, Utils.ComponentId);
            CanWrite = CanDo(gridPolicies, x => x.CanWrite);
            Html.Take(ParentElement).DataAttr("name", GuiInfo.FieldName);
            AddSections();
            SetRowDataIfExists();
            RowData.ListChanged += RowDataChanged;
            EditForm.ResizeListView();
            if (GuiInfo.LocalData.HasElement() && GuiInfo.LocalHeader.HasElement())
            {
                Header = GuiInfo.LocalHeader;
                HeaderComponentMap = Header.DistinctBy(x => x.GetHashCode()).ToDictionary(x => x.GetHashCode(), x => x.MapToComponent());
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
            var calcFilter = CalcFilterQuery(searching);
            ClearRowData();
            await ReloadData(calcFilter, cache: false);
        }

        public virtual async Task ActionFilter()
        {
        }

        public virtual string CalcFilterQuery(bool searching)
        {
            string advFilter = FormattedDataSource;
            if (AdvSearchVM.Conditions.HasElement())
            {
                var advSearch = new AdvancedSearch(this)
                {
                    Parent = this,
                    Entity = AdvSearchVM
                };
                advFilter = advSearch.CalcAdvSearchQuery();
            }
            return ListViewSearch.CalcFilterQuery(advFilter);
        }

        public virtual async Task LoadAllData()
        {
            await LoadHeader();
            await ReloadData(cache: GuiInfo.CanCache);
        }

        public async Task LoadHeader()
        {
            var columns = await LoadGridPolicy();
            columns = FilterColumns(columns);
            BasicHeader = columns.OrderBy(x => x.Order).ToList();
            BasicHeaderSearch = columns.Where(x => x.ComponentType == "Dropdown").ToList();
            ResetOrder();
            HeaderLoaded?.Invoke(columns);
        }

        public void ResetOrder()
        {
            int order = 0;
            BasicHeader.ForEach(x =>
            {
                x.Order = order;
                order++;
            });
        }

        protected virtual List<GridPolicy> FilterColumns(List<GridPolicy> gridPolicy)
        {
            var specificComponent = gridPolicy.Any(x => x.ComponentId == GuiInfo.Id);
            if (specificComponent)
            {
                gridPolicy = gridPolicy.Where(x => x.ComponentId == GuiInfo.Id).ToList();
            }
            else
            {
                gridPolicy = gridPolicy.Where(x => x.ComponentId == null).ToList();
            }

            var permission = EditForm.GetElementPolicies(gridPolicy.Select(x => x.Id).ToArray(), Utils.GridPolicyId);
            var headers = gridPolicy
                .Where(header => !header.IsPrivate || permission.Where(x => x.RecordId == header.Id).HasElementAndAll(policy => policy.CanRead))
                .Select(CalcTextAlign).OrderByDescending(x => x.Frozen).ThenBy(x => x.Order).ToList();
            OrderHeaderGroup(headers);
            Header.Clear();
            Header.AddRange(headers);
            HeaderComponentMap = Header.DistinctBy(x => x.GetHashCode()).ToDictionary(x => x.GetHashCode(), x => x.MapToComponent());
            return headers;
        }

        protected GridPolicy CalcTextAlign(GridPolicy header)
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

        public virtual ListViewItem RenderRowData(List<GridPolicy> headers, object row, Section section, int? index = null, bool emptyRow = false)
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

            FormattedRowData.ForEach((rowData, index) =>
            {
                var rowSection = RenderRowData(Header, rowData, MainSection);
            });
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
                RemoveRowById(args.Item[IdField].As<int>());
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
                Entity.SetComplexPropValue(GuiInfo.FieldName, RowData.Data);
            }
        }

        protected void SetRowDataIfExists()
        {
            if (Entity != null && Entity.GetComplexPropValue(GuiInfo.FieldName) is IEnumerable value && value.GetEnumerator().MoveNext())
            {
                RowData["_data"] = value;
            }
        }

        protected virtual async Task<List<GridPolicy>> LoadGridPolicy()
        {
            List<GridPolicy> sysSetting = null;
            UserSetting userSetting = null;
            if (GuiInfo.LocalHeader.Nothing())
            {
                if (FeatureId != "null")
                {
                    FeatureId = EditForm?.Feature != null ? EditForm.Feature.Id.ToString() : GuiInfo.ComponentGroup.FeatureId.ToString();
                }
                sysSetting = await new Client(nameof(GridPolicy)).GetRawList<GridPolicy>(
                    $"?$filter=Active eq true and EntityId eq {GuiInfo.ReferenceId} and FeatureId eq {FeatureId}");

                userSetting = await new Client(nameof(UserSetting)).FirstOrDefaultAsync<UserSetting>(
                $"?$filter=UserId eq {Client.Token.UserId} and Name eq 'ListView-{GuiInfo.Id}'");
            }
            else
            {
                sysSetting = new List<GridPolicy>(GuiInfo.LocalHeader);
            }
            var dataSource = default(string);
            if (GuiInfo.Precision.HasValue && !GuiInfo.DateTimeField.IsNullOrWhiteSpace())
            {
                var calcFilter = CalcFilterQuery(true);
                dataSource = ListViewSearch.CalcFilterQuery(calcFilter);
            }
            if (userSetting is null)
            {
                if (sysSetting.Count <= 10 && GuiInfo.ComponentType == "GridView")
                {
                    var pe = (100 / sysSetting.Count) + "%";
                    sysSetting.ForEach(x =>
                    {
                        x.Width = pe;
                        x.MaxWidth = pe;
                        x.MinWidth = pe;
                    });
                }
                return sysSetting;
            }
            else
            {
                var column = userSetting.Value;
                return MergeGridPolicy(sysSetting, JsonConvert.DeserializeObject<List<GridPolicy>>(column));
            }
        }

        protected virtual List<GridPolicy> MergeGridPolicy(List<GridPolicy> sysSetting, List<GridPolicy> userSetting)
        {
            if (userSetting.Nothing() || GuiInfo.Focus)
            {
                return sysSetting;
            }
            var gridPolicys = new List<GridPolicy>();
            var userSettings = userSetting.ToDictionary(x => x.Id);
            sysSetting.ForEach(x =>
            {
                var current = userSettings.GetValueOrDefault(x.Id);
                if (current != null)
                {
                    if (current.FieldName == nameof(GridPolicy.InsertedBy) || current.FieldName == nameof(GridPolicy.InsertedDate))
                    {
                        x.Width = "100px";
                        x.MaxWidth = "100px";
                        x.MinWidth = "100px";
                        x.Order = 998;
                    }
                    else if (current.FieldName == nameof(GridPolicy.Id))
                    {
                        x.Width = "75px";
                        x.MaxWidth = "75px";
                        x.MinWidth = "75px";
                        x.Order = 999;
                    }
                    else
                    {
                        x.Width = current.Width;
                        x.MaxWidth = current.MaxWidth;
                        x.MinWidth = current.MinWidth;
                        x.Order = current.Order;
                        x.Frozen = current.Frozen;
                    }
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
            options.EndIndex = options.StartIndex + options.CurrentPageCount;
            Paginator.UpdateView();
        }

        public async Task RealtimeUpdateAsync(ListViewItem rowData, ObservableArgs arg)
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
            if (rowData.EntityId > 0)
            {
                await rowData.PatchUpdate();
            }
            else
            {
                await rowData.CreateUpdate();
            }
        }

        internal virtual async Task RowChangeHandler(object rowData, ListViewItem rowSection, ObservableArgs observableArgs, EditableComponent component = null)
        {
            if (rowSection.EmptyRow && Editable)
            {
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforeCreated, rowData);
                RowData.Data.Add(rowData);
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCreated, rowData);
                Entity.SetComplexPropValue(GuiInfo.FieldName, RowData.Data);
                RowAction(x => x.Entity == rowSection.Entity, x =>
                {
                    x.EmptyRow = false;
                    x.FilterChildren(child => true).ForEach(child =>
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

        public async Task LoadMasterData(IEnumerable<object> rows = null, bool spinner = true)
        {
            var headers = GuiInfo.LocalHeader.Nothing() ? Header : GuiInfo.LocalHeader;
            if (headers.Nothing())
            {
                return;
            }

            rows = rows ?? RowData.Data;
            var refHeaders = headers.Where(x => x.RefName.HasAnyChar()).ToList();
            SyncMasterData(rows, headers);
            var dataSource = refHeaders
                .DistinctBy(x => x.ReferenceId)
                .Select(x => FormatDataSourceByEntity(x, headers, rows))
                .Where(x => x != null).ToList();
            if (dataSource.Nothing())
            {
                return;
            }

            var dataTask = dataSource
                .Where(x => !x.DataSourceOptimized.IsNullOrWhiteSpace())
                .DistinctBy(x => x.ReferenceId + "/" + x.DataSourceOptimized)
                .Select(x => new
                {
                    Header = x,
                    Data = new Client(x.RefName).LoadById(x.DataSourceOptimized, action: $"ById?FieldName={x.FormatExcell}&DatabaseName={x.DatabaseName}")
                }).ToArray();
            await Task.WhenAll(dataTask.Select(x => x.Data).ToArray());
            foreach (var remoteSource in dataTask)
            {
                SetRemoteSource(remoteSource.Data.Result.Value, remoteSource.Header.RefName, remoteSource.Header);
            }
            SyncMasterData(rows, headers);
        }

        protected void SetRemoteSource(List<object> remoteData, string typeName, GridPolicy header)
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

        protected void SyncMasterData(IEnumerable<object> rows = null, List<GridPolicy> headers = null)
        {
            rows = rows ?? RowData.Data;
            headers = headers ?? Header;
            foreach (var header in headers.Where(x => x.ReferenceId.HasValue))
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
                    var propType = Utils.GetEntity(header.ReferenceId.Value)?.Name;
                    if (propType is null)
                    {
                        continue;
                    }

                    var propVal = row.GetComplexPropValue(objField);
                    var found = RefData.GetValueOrDefault(propType)?.FirstOrDefault(source =>
                    {
                        return source[IdField].As<int?>() == row.GetComplexPropValue(header.FieldName).As<int?>();
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

        private GridPolicy FormatDataSourceByEntity(GridPolicy currentHeader, IEnumerable<GridPolicy> allHeaders, IEnumerable<object> entities)
        {
            var entityIds = allHeaders
                .Where(x => x.RefName == currentHeader.RefName)
                .SelectMany(x => GetEntityIds(x, entities)).Distinct();
            if (entityIds.Nothing())
            {
                return null;
            }

            currentHeader.DataSourceOptimized = entityIds.Where(x => x.HasValue).Select(x => x.Value).OrderBy(x => x).Combine();
            return currentHeader;
        }

        private EnumerableInstance<int?> GetEntityIds(GridPolicy header, IEnumerable<object> entities)
        {
            if (entities.Nothing())
            {
                return Enumerable.Empty<int?>();
            }

            return entities.Select(x =>
            {
                var id = x.GetComplexPropValue(header.FieldName)?.ToString();
                if (id.IsNullOrEmpty())
                {
                    return null;
                }

                return id.TryParseInt();
            }).Where(id => id != null && id.Value > 0);
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
            var ids = selected.Select(x => (int)x[IdField]).ToList();
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
            var ids = deleted.Select(x => (int)x[IdField]).Where(x => x > 0).ToList();
            var removeRow = deleted.Where(x => (int)x[IdField] <= 0).ToList();
            if (removeRow.Any())
            {
                RemoveRange(removeRow);
            }
            if (ids.Nothing())
            {
                Toast.Success("Xóa dữ liệu thành công");
                return null;
            }
            var client = new Client(entity);
            var success = await client.HardDeleteAsync(ids);
            if (success)
            {
                var deletedHistory = AllListViewItem
                    .Where(x => x.Selected).Select(x => x.OriginalText).Combine(Utils.NewLine);
                AllListViewItem.Where(x => x.Selected).ToArray().ForEach(x => x.Dispose());
                Toast.Success("Xóa dữ liệu thành công");
                ClearSelected();
                return deleted;
            }
            else
            {
                Toast.Warning("Xóa không thành công");
            }
            return null;
        }

        public virtual void RemoveRange(IEnumerable<object> deleted)
        {
            deleted.ForEach(x => RemoveRowById(x[IdField].As<int>()));
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
            return MainSection.Children.Where(x => x is ListViewItem item && item.Selected).Select(x => x.Entity).ToList();
        }

        public virtual async Task<List<object>> GetRealTimeSelectedRows()
        {
            return await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetRawListById<object>(SelectedIds.ToList());
        }

        public void PasteSelected(object ev)
        {
            if (_copiedRows.Nothing())
            {
                return;
            }

            Task.Run((async () =>
            {
                Toast.Success("Đang Sao chép liệu !");
                await ComponentExt.DispatchCustomEventAsync(this, GuiInfo.Events, CustomEventType.BeforePasted, _originRows, _copiedRows);
                var index = AllListViewItem.IndexOf(x => x.Selected);
                var list = await AddRows(_copiedRows, index);
                base.Dirty = true;
                base.Focus();
                await ComponentExt.DispatchCustomEventAsync(this, GuiInfo.Events, CustomEventType.AfterPasted, _originRows, _copiedRows);
                if (GuiInfo.IsRealtime)
                {
                    foreach (var item in list)
                    {
                        await item.CreateUpdate();
                    }
                    Toast.Success("Sao chép dữ liệu thành công !");
                    base.Dirty = false;
                    ClearSelected();
                }
                else
                {
                    Toast.Success("Sao chép dữ liệu thành công !");
                }
            }));
        }

        public void DuplicateSelected(object ev)
        {
            var originalRows = GetSelectedRows();
            var copiedRows = ReflectionExt.CopyRowWithoutId(originalRows).ToList();
            if (copiedRows.Nothing() || !CanWrite)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                Toast.Success("Đang Sao chép liệu !");
                await ComponentExt.DispatchCustomEventAsync(this, GuiInfo.Events, CustomEventType.BeforePasted, originalRows, copiedRows);
                var index = AllListViewItem.IndexOf(x => x.Selected);
                var list = await AddRows(copiedRows, index);
                base.Dirty = true;
                base.Focus();
                await ComponentExt.DispatchCustomEventAsync(this, GuiInfo.Events, CustomEventType.AfterPasted, originalRows, copiedRows);
                if (GuiInfo.IsRealtime)
                {
                    foreach (var item in list)
                    {
                        await item.CreateUpdate();
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
            if (singleAdd)
            {
                await LoadMasterData(new List<object> { rowData }, false);
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
            await LoadMasterData(rows);
            await rows.ForEachAsync(async row => await AddOrUpdateRow(row, false));
            FinalAddOrUpdate();
        }

        public virtual async Task<ListViewItem> AddRow(object rowData, int index = 0, bool singleAdd = true)
        {
            DisposeNoRecord();
            var exists = MainSection.FirstOrDefault(x => x.Entity == rowData) as ListViewItem;
            if (exists != null)
            {
                return exists;
            }

            if (singleAdd)
            {
                RowData.Data.Add(rowData);
                await LoadMasterData(new List<object> { rowData }, false);
            }
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforeCreated, rowData);
            var row = RenderRowData(Header, rowData, MainSection, index);
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCreated, rowData);
            return row;
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

            rows.ForEach(row =>
            {
                if (RowData.Data is IList)
                {
                    RowData.Data.Insert(index, row);
                }
                else
                {
                    RowData.Data.Add(row);
                }
            });
            if (!GuiInfo.IsRealtime)
            {
                await LoadMasterData(rows);
            }
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforeCreatedList, rows);
            var listItem = new List<ListViewItem>();
            await rows.AsEnumerable().Reverse().ForEachAsync(async data =>
            {
                listItem.Add(await AddRow(data, index, false));
            });
            await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCreatedList, rows);
            AddNewEmptyRow();
            return listItem;
        }

        public virtual void RemoveRowById(int id)
        {
            var row = RowData.Data.FirstOrDefault(x => x[IdField].As<int>() == id);
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
                row => row.Children.Where(x => fieldName.Nothing() || fieldName.Contains(x.GuiInfo.FieldName)).ForEach(x => x.UpdateView(force))
            );
        }

        public void ClearRowData()
        {
            RowData.Data.Clear();
            RowAction(x => !x.EmptyRow, x => x.Dispose());
            MainSection.Element.InnerHTML = null;
            if (Entity is null || Parent is SearchEntry)
            {
                return;
            }
            if (ShouldSetEntity)
            {
                Entity?.SetComplexPropValue(GuiInfo.FieldName, RowData.Data);
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
            Html.Take(Document.Body).Div.ClassName("backdrop")
            .Style("align-items: center;").Escape((e) => Dispose());
            _history = Html.Context;
            Html.Instance.Div.ClassName("popup-content confirm-dialog").Style("top: 0;")
                .Div.ClassName("popup-title").InnerHTML("Xem lịch sử")
                .Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                    .Event(EventType.Click, DisposeHistory)
                .EndOf(".popup-title")
                .Div.ClassName("popup-body scroll-content");
            var body = Html.Context;
            var _filterGrid = new GridView(new Component
            {
                FieldName = nameof(AdvSearchVM.Conditions),
                Column = 4,
                ReferenceId = Utils.GetEntity(nameof(Models.History)).Id,
                RefName = nameof(Models.History),
                Reference = new Entity { Name = nameof(Models.History), Namespace = typeof(Component).Namespace + "." },
                DataSourceFilter = $"?$orderby=Id desc&$filter=Active eq true and EntityId eq {GuiInfo.ReferenceId} and RecordId eq {currentItem[IdField]}",
            });
            _filterGrid.GuiInfo.LocalHeader = new List<GridPolicy>
            {
                     new GridPolicy
                    {
                        Id = 1,
                        EntityId = Utils.GetEntity(nameof(Models.History)).Id,
                        FieldName = nameof(Models.History.InsertedBy),
                        ShortDesc = "Người thay đổi",
                        ReferenceId=Utils.GetEntity(nameof(Models.User)).Id,
                        RefName=nameof(Models.User),
                        FormatCell="{FullName}",
                        Active = true,
                        ComponentType = "Dropdown",
                        MaxWidth = "100px",
                        MinWidth = "100px",
                    },
                     new GridPolicy
                    {
                        Id = 2,
                        EntityId = Utils.GetEntity(nameof(Models.History)).Id,
                        FieldName = nameof(Models.History.InsertedDate),
                        ShortDesc = "Ngày thay đổi",
                        Active = true,
                        FormatCell="{0: dd/MM/yyyy HH:mm}",
                        ComponentType = "Datepicker",
                        TextAlign="left",
                        MaxWidth = "150px",
                        MinWidth = "150px",
                    },
                    new GridPolicy
                    {
                        Id = 4,
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
                .Select(x => (int)x[IdField]).ToArray();
            var security = new SecurityBL
            {
                Entity = new SecurityVM { RecordIds = selectedRowIds, EntityId = GuiInfo.ReferenceId ?? 0 },
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
            var gridPolicy = gridPolicies.Any();
            if (!gridPolicy)
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
            if (!(currentRow is GroupViewItem))
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
            var targetRef = await new Client(nameof(EntityRef)).GetRawList<EntityRef>($"?$filter=ComId eq {GuiInfo.Id}");
            if (targetRef.Nothing())
            {
                return;
            }
            var menuItems = targetRef.Select(x => new ContextMenuItem
            {
                Text = x.MenuText,
                Click = async (arg) => await OpenFeature(x),
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

        private async Task OpenFeature(EntityRef e)
        {
            var instance = TabEditor.Tabs.Where(x => x.Name == e.ViewClass).FirstOrDefault();
            if (instance is null)
            {
                _hasLoadRef = false;
                var currentFeature = await ComponentExt.LoadFeatureByName(e.ViewClass);
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
                    gridView1.DOMContentLoaded += async () =>
                    {
                        if (_hasLoadRef)
                        {
                            return;
                        }
                        await Filter(instance, e);
                        _hasLoadRef = true;
                    };
                };
            }
            else
            {
                instance.Focus();
                await Filter(instance, e);
                _hasLoadRef = true;
            }
        }

        private async Task Filter(TabEditor fe, EntityRef e)
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
            var selecteds = await GetRealTimeSelectedRows();
            var com = gridView1.BasicHeader.FirstOrDefault(x => x.FieldName == e.TargetFieldName);
            var cellSelecteds = selecteds.Select(selected =>
            {
                return new CellSelected()
                {
                    FieldName = e.TargetFieldName,
                    FieldText = com.ShortDesc,
                    ComponentType = com.ComponentType,
                    Value = selected.GetPropValue(e.FieldName).ToString(),
                    ValueText = selected.GetPropValue(e.FieldName).ToString(),
                    Operator = "in",
                    OperatorText = "Chứa",
                    Logic = LogicOperation.Or,
                    IsSearch = true,
                    Group = true
                };
            });
            gridView1.CellSelected.AddRange(cellSelecteds);
            await gridView1.ActionFilter();
        }

        public virtual void RenderCopyPasteMenu(bool canWrite)
        {
            //
        }

        private void RenderEditMenu(List<object> selectedRows, IEnumerable<FeaturePolicy> gridPolicies)
        {
            var lockDeleteDate = gridPolicies.Any() ? gridPolicies.Max(x => x.LockDeleteAfterCreated)
                : EditForm.Feature.FeaturePolicy.Any() ? EditForm.Feature.FeaturePolicy.Max(x => x.LockDeleteAfterCreated) : 0;
            var shouldLockDelete = lockDeleteDate > 0 && selectedRows.Any(x =>
                DateTimeExt.GetBusinessDays(x[nameof(ComponentGroup.InsertedDate)].As<DateTime>()) > lockDeleteDate);
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
            if (canDelete && !shouldLockDelete)
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
                var hasPolicy = RecordPolicy.Any(f => f.EntityId == GuiInfo.ReferenceId && f.RecordId == x[IdField].As<int>());
                var loaded = x[PermissionLoaded].As<bool?>();
                return !(hasPolicy || loaded == true);
            });
            var noPolicyRowIds = noPolicyRows.Select(x => x[IdField].As<int>()).ToArray();
            var rowPolicy = await ComponentExt.LoadRecordPolicy(noPolicyRowIds, GuiInfo.ReferenceId ?? 0);
            rowPolicy.ForEach(RecordPolicy.Add);
            noPolicyRows.ForEach(x => x[PermissionLoaded] = true);
            var ownedRecords = selectedRows.Where(x =>
            {
                var isOwner = Utils.IsOwner(x);
                x[IsOwner] = isOwner || rowPolicy.Any(policy => policy.CanShare && x[IdField].As<int>() == policy.RecordId);
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
            if (SelectedIndex >= RowData.Data.Count - 1)
            {
                SelectedIndex = 0;
            }
            else if (SelectedIndex < RowData.Data.Count - 1)
            {
                SelectedIndex++;
            }
            RowAction(SelectedIndex, x => x.Selected = true);
        }

        public void ClearSelected()
        {
            RowAction(x => x.Selected = false);
            SelectedIds.Clear();
            LastListViewItem = null;
        }

        public void RowAction(int index, Action<ListViewItem> action)
        {
            if (index < 0 || action is null || index >= AllListViewItem.Count())
            {
                return;
            }

            var row = AllListViewItem.ElementAt(index);
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

        public virtual void MoveUp()
        {
            ClearSelected();
            if (SelectedIndex <= 0)
            {
                SelectedIndex = RowData.Data.Count - 1;
            }
            else if (SelectedIndex > 0)
            {
                SelectedIndex--;
            }
            RowAction(SelectedIndex, x => x.Selected = true);
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
            var updatedRows = await new Client(GuiInfo.Reference.Name).BulkUpdateAsync(UpdatedRows);
            await InternalUpdateRows(updateView, updatedRows);
            return updatedRows;
        }

        private async Task InternalUpdateRows(bool updateView, List<object> updatedRows)
        {
            UpdatedRows.CopyPropFrom(updatedRows);
            UpdatedRows.ForEach(x => x.SetComplexPropValue(nameof(User.InsertedDate), Convert.ToDateTime(x.GetComplexPropValue(nameof(User.InsertedDate)))));
            if (updateView)
            {
                await LoadMasterData(updatedRows);
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
            EditForm.NotificationClient?.RemoveListener(RealtimeUpdateListViewItem, GuiInfo.ReferenceId.Value);
            base.Dispose();
        }
    }
}
