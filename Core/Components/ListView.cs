using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.Structs;
using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;
using Position = Core.MVVM.PositionEnum;

namespace Core.Components
{
    public class ListView : EditableComponent
    {
        internal int _rowHeight = 26;
        internal int _theadTable = 40;
        internal int _tfooterTable = 35;
        internal int _scrollTable = 10;
        private const string PermissionLoaded = "PermissionLoaded";
        private const string IsOwner = "__IsOwner__";
        protected static IEnumerable<object> _copiedRows;
        public Action<object> RowClick;
        public Action<object> DblClick;
        public FeaturePolicy[] GeneralPolicies { get; set; }
        public FeaturePolicy[] GridPolicies { get; set; }

        public bool CanWrite;
        protected Section _noRecord;
        public Action BodyContextMenuShow;
        private HTMLElement _history;
        public Component LastComponentFocus;
        public List<string> DeleteTempIds;
        public AdvSearchVM AdvSearchVM { get; set; }
        public bool Editable => Meta.CanAdd;
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
        public List<object> CacheData { get; set; } = new List<object>();
        public int SelectedIndex { get; set; } = -1;
        public ListViewSection HeaderSection { get; set; }
        public ListViewSection MainSection { get; set; }
        public ListViewSection FooterSection { get; set; }
        private static List<object> _originRows;
        public Action OnDeleteConfirmed { get; set; }
        public IEnumerable<ListViewItem> AllListViewItem => MainSection.Children.Cast<ListViewItem>();
        public List<object> UpdatedRows => AllListViewItem.OrderBy(x => x.RowNo).Where(x => x.Dirty).Select(x => x.Entity).Distinct().ToList();
        public List<ListViewItem> UpdatedListItems => AllListViewItem.OrderBy(x => x.RowNo).Where(x => x.Dirty).ToList();
        public List<CellSelected> CellSelected = new List<CellSelected>();
        public List<Where> Wheres = new List<Where>();
        public List<string> SelectedIds { get; set; } = new List<string>();
        public string FocusId { get; set; }
        public string EntityFocusId { get; set; }
        public bool ShouldSetEntity { get; set; } = true;
        protected UserSetting Settings { get; set; }

        public event Action<object[][]> DataLoaded;

        public ListView(Component ui, HTMLElement ele = null) : base(ui)
        {
            DeleteTempIds = new List<string>();
            Meta = ui ?? throw new ArgumentNullException(nameof(ui));
            Id = ui.Id?.ToString();
            Name = ui.FieldName;
            Header = new List<Component>();
            RowData = new ObservableList<object>();
            AdvSearchVM = new AdvSearchVM
            {
                ActiveState = ActiveStateEnum.Yes,
                OrderBy = LocalStorage.GetItem<List<OrderBy>>("OrderBy" + Meta.Id) ?? new List<OrderBy>()
            };
            _hasLoadRef = false;
            if (ele != null)
            {
                Resolve(ui, ele);
            }

            _rowHeight = Meta.BodyItemHeight ?? 26;
            _theadTable = Meta.HeaderHeight ?? 40;
            _tfooterTable = Meta.FooterHeight ?? 35;
            _scrollTable = Meta.ScrollHeight ?? 10;
            Window.AddEventListener(Meta.QueueName, RealtimeUpdateListViewItem);
            Utils.IsFunction(Meta.PreQuery, out _preQueryFn);
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
            this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterWebsocket, updatedData, listViewItem).Done();
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

        public virtual Task<List<object>> ReloadData(bool cacheHeader = false, int? skip = null, int? pageSize = null)
        {
            if (Meta.LocalQuery.HasNonSpaceChar())
            {
                Meta.LocalData = JsonConvert.DeserializeObject<List<object>>(Meta.LocalQuery);
                Meta.LocalRender = true;
            }
            if (Meta.LocalRender && Meta.LocalData != null)
            {
                SetRowData(Meta.LocalData);
                return Task.FromResult(Meta.LocalData);
            }
            if (Paginator != null)
            {
                Paginator.Options.PageSize = Paginator.Options.PageSize == 0 ? (Meta.Row ?? 12) : Paginator.Options.PageSize;
            }
            pageSize = pageSize ?? Paginator?.Options?.PageSize ?? Meta.Row ?? 12;
            skip = skip ?? Paginator?.Options?.PageIndex * pageSize ?? 0;
            return SqlReader(skip, pageSize, cacheHeader);
        }

        public Task<List<object>> SqlReader(int? skip, int? pageSize, bool cacheHeader = false)
        {
            var sql = GetSql(skip, pageSize, cacheHeader);
            return CustomQuery(sql);
        }

        public SqlViewModel GetSql(int? skip = null, int? pageSize = null, bool cacheMeta = false, bool count = true)
        {
            var submitEntity = _preQueryFn != null ? _preQueryFn.Call(null, this) : null;
            var orderBy = AdvSearchVM.OrderBy.HasElement() ? AdvSearchVM.OrderBy.Combine(x =>
            {
                var sortDirection = x.OrderbyDirectionId == OrderbyDirection.ASC ? "asc" : "desc";
                return $"ds.{x.FieldName} {sortDirection}";
            }) : null;
            var basicCondition = CalcFilterQuery();
            var fnBtnCondition = Wheres.Combine(x => $"({x.Condition})", " and ");
            var finalCon = new string[] { basicCondition, fnBtnCondition }
                .Where(x => !x.IsNullOrWhiteSpace()).Combine(" and ");
            var data = new SqlViewModel
            {
                ComId = Meta.Id,
                Params = submitEntity != null ? JSON.Stringify(submitEntity) : null,
                OrderBy = orderBy ?? (Meta.OrderBy.IsNullOrWhiteSpace() ? "ds.Id asc\n" : Meta.OrderBy),
                Where = finalCon,
                Count = count,
                SkipXQuery = cacheMeta,
                ConnKey = ConnKey
            };
            if (skip.HasValue && pageSize.HasValue)
            {
                data.Paging = $"offset {skip} rows\nfetch next {pageSize} rows only";
            }
            return data;
        }

        protected virtual Task<List<object>> CustomQuery(SqlViewModel vm)
        {
            var tcs = new TaskCompletionSource<List<object>>();
            var dsTask = Client.Instance.ComQuery(vm);
            dsTask.Done(ds =>
            {
                if (ds.Nothing())
                {
                    SetRowData(null);
                    tcs.TrySetResult(null);
                }
                var total = ds.Length > 1 ? ds[1].ToDynamic()[0].total : ds[0].Length;
                var rows = new List<object>(ds[0]);
                Spinner.Hide();
                SetRowData(rows);
                UpdatePagination(total, rows.Count);
                if (Utils.IsFunction(Meta.FormatEntity, out var formatter))
                {
                    rows = formatter.Call(null, rows, this) as List<object>;
                }
                tcs.TrySetResult(rows);
                DataLoaded?.Invoke(ds);
            }).Catch(err => tcs.TrySetException(err));
            return tcs.Task;
        }

        public override void Render()
        {
            var feature = EditForm.Feature;
            GridPolicies = EditForm.GetElementPolicies(Meta.Id);
            GeneralPolicies = EditForm.Feature.FeaturePolicy.Where(x => x.RecordId.IsNullOrWhiteSpace()).ToArray();
            CanWrite = CanDo(x => x.CanWrite || x.CanWriteAll);
            Html.Take(ParentElement).DataAttr("name", FieldName);
            AddSections();
            SetRowDataIfExists();
            EditForm.ResizeListView();
            if (Meta.LocalRender) LocalRender();
            else LoadAllData();
        }

        private void LocalRender()
        {
            Header = Meta.LocalHeader;
            if (Meta.LocalRender)
            {
                Rerender();
            }
            else
            {
                RowData.Data = Meta.LocalData;
            }
        }

        internal virtual void AddSections()
        {
            if (Meta.LiteGrid)
            {
                Element = ParentElement;
                Element.InnerHTML = null;
                MainSection = new ListViewSection(ParentElement);
                AddChild(MainSection);
                return;
            }
            Html.Take(ParentElement).Div.ClassName("grid-wrapper")
                .ClassName(Editable ? "editable" : string.Empty);
            Element = Html.Context;
            if (Meta.CanSearch)
            {
                Html.Instance.Div.ClassName("grid-toolbar search").End.Render();
            }
            ListViewSearch = new ListViewSearch(Meta);
            AddChild(ListViewSearch);
            Html.Take(Element).Div.ClassName("list-content").End.Div.ClassName("empty");
            EmptySection = new ListViewSection(Html.Context) { ParentElement = Element };
            AddChild(EmptySection);

            MainSection = new ListViewSection(EmptySection.Element.PreviousSibling);
            AddChild(MainSection);


            Html.Instance.EndOf(".list-content");
            RenderPaginator();
        }

        public virtual Task ApplyFilter()
        {
            ClearRowData();
            return ReloadData(skip: 0, cacheHeader: true);
        }

        public virtual void ActionFilter()
        {
            ClearRowData();
            ReloadData().Done();
        }

        public virtual string CalcFilterQuery()
        {
            return ListViewSearch.CalcFilterQuery();
        }

        public void LoadAllData()
        {
            ReloadData(cacheHeader: Meta.CanCache).Done();
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
            if (Component.Nothing()) return Component;
            var specificComponent = Component.Any(x => x.ComponentId == Meta.Id);
            if (specificComponent)
            {
                Component = Component.Where(x => x.ComponentId == Meta.Id).ToList();
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
            if (Meta.LocalRender || Meta.LiteGrid)
            {
                if (Paginator != null)
                {
                    Paginator.Show = false;
                }
                return;
            }
            if (Meta.Row is null || Meta.Row == 0)
            {
                Meta.Row = 20;
            }

            if (Paginator is null)
            {
                Paginator = new Paginator(new PaginationOptions
                {
                    Total = 0,
                    PageSize = Meta.Row ?? 50,
                    CurrentPageCount = RowData.Data.Count(),
                });
                AddChild(Paginator);
            }
        }

        public virtual ListViewItem RenderRowData(List<Component> headers, object row, Section section, int? index = null, bool emptyRow = false)
        {
            var rowSection = Meta.LiteGrid ? new ListViewItem() : new ListViewItem(ElementType.div);
            rowSection.EmptyRow = emptyRow;
            rowSection.Entity = row;
            rowSection.ParentElement = section.Element;
            rowSection.PreQueryFn = _preQueryFn;
            rowSection.ListView = this;
            rowSection.ListViewSection = section as ListViewSection;
            rowSection.Meta = Meta;
            rowSection.EditForm = EditForm;
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
            MainSection.DisposeChildren();
            EmptySection?.DisposeChildren();
            FormattedRowData = FormattedRowData.Nothing() ? RowData.Data : FormattedRowData;
            if (FormattedRowData.Nothing())
            {
                return;
            }

            FormattedRowData.SelectForEach((rowData, index) =>
            {
                var rowSection = RenderRowData(Header, rowData, MainSection);
            });
            ContentRendered();
        }

        protected virtual void Rerender()
        {
            DisposeNoRecord();
            MainSection.DisposeChildren();
            Html.Take(MainSection.Element).Clear();
            RenderContent();
        }

        protected virtual void ContentRendered()
        {
            RenderIndex();
            DomLoaded();
            if (Editable)
            {
                AddNewEmptyRow();
            }
            if (RowData.Data.Nothing() && !Editable)
            {
                NoRecordFound();
            }
            else
            {
                DisposeNoRecord();
            }
            if (Editable)
            {
                MainSection.Element.AddEventListener(EventType.ContextMenu, BodyContextMenuHandler);
            }
            Spinner.Hide();
        }

        protected void DomLoaded()
        {
            if (!Meta.LocalRender)
            {
                Header.ForEach(x => x.LocalData = null);
            }
            DOMContentLoaded?.Invoke();
        }

        protected virtual void SetRowData(List<object> listData)
        {
            RowData._data.Clear();
            var hasElement = listData.HasElement();
            if (hasElement)
            {
                listData.ForEach(RowData._data.Add); // Not to use AddRange because the _data is not always List
            }
            RenderContent();
            if (Entity != null && ShouldSetEntity)
            {
                Entity.SetComplexPropValue(FieldName, RowData.Data);
            }
        }

        protected void SetRowDataIfExists()
        {
            if (Entity != null && Utils.GetPropValue(Entity, FieldName) is IEnumerable value
                && value.GetEnumerator().MoveNext() && (value is string))
            {
                RowData["_data"] = value;
            }
        }

        protected virtual List<Component> MergeComponent(List<Component> sysSetting, UserSetting userSetting)
        {
            if (userSetting is null) return sysSetting;
            var column = JsonConvert.DeserializeObject<List<Component>>(userSetting.Value);
            if (column.Nothing())
            {
                return sysSetting;
            }
            var Components = new List<Component>();
            var userSettings = column.DistinctBy(x => x.Id).ToDictionary(x => x.Id);
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
            if (!Meta.IsRealtime || arg is null)
            {
                return;
            }
            rowData.PatchUpdateOrCreate();
        }

        internal virtual Task RowChangeHandler(object rowData, ListViewItem rowSection, ObservableArgs observableArgs, EditableComponent component = null)
        {
            var tcs = new TaskCompletionSource<bool>();
            if (!rowSection.EmptyRow || !Editable)
            {
                this.DispatchEvent(Meta.Events, EventType.Change, rowData).Done(() =>
                {
                    tcs.TrySetResult(false);
                });
                return tcs.Task;
            }
            this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforeCreated, rowData).Done(() =>
            {
                RowData.Data.Add(rowData);
                Entity.SetComplexPropValue(FieldName, RowData.Data);
                RowAction(x => x.Entity == rowSection.Entity, x =>
                {
                    x.EmptyRow = false;
                    x.FilterChildren(child => true).SelectForEach(child =>
                    {
                        child.EmptyRow = false;
                        child.UpdateView(force: true);
                    });
                });
                EmptySection.Children.Clear();
                AddNewEmptyRow();
                this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterCreated, rowData).Done(() =>
                {
                    tcs.TrySetResult(true);
                });
            });
            return tcs.Task;
        }

        public virtual void AddNewEmptyRow()
        {
            if (Meta.LiteGrid || Disabled || !Editable || EmptySection?.Children.HasElement() == true)
            {
                return;
            }
            var emptyRowData = new object();
            if (!Meta.DefaultVal.IsNullOrWhiteSpace() && Utils.IsFunction(Meta.DefaultVal, out var fn))
            {
                var dfObj = fn.Call(this, this);
                dfObj.ForEachProp(x =>
                {
                    emptyRowData[x] = dfObj[x];
                });
            }
            emptyRowData[IdField] = null;
            var rowSection = RenderRowData(Header, emptyRowData, EmptySection, null, true);
            emptyRowData.ForEachProp((field, value) =>
            {
                rowSection.PatchModel.Add(new PatchDetail
                {
                    Field = field,
                    Value = value?.ToString()
                });
            });
            if (!Meta.TopEmpty)
            {
                MainSection.Element.InsertBefore(MainSection.Element, EmptySection.Element);
            }
            else
            {
                MainSection.Element.AppendChild(EmptySection.Element.FirstElementChild);
            }
            this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterEmptyRowCreated, emptyRow).Done();
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
            Html.Take(_noRecord.Element).IHtml("No record found");
            DomLoaded();
        }

        public void BodyContextMenuHandler(Event e)
        {
            e.PreventDefault();
            e.StopPropagation();
            TbodyContextMenu(e);
        }

        public void DeactivateSelected(object ev = null)
        {
            var confirm = new ConfirmDialog
            {
                Content = "Are you sure to deactivate?"
            };
            confirm.Render();
            confirm.YesConfirmed += () =>
            {
                confirm.Dispose();
                Deactivate().Done(() =>
                {
                    this.DispatchCustomEvent(Meta.Events, CustomEventType.Deactivated, Entity).Done();
                });
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

        protected virtual void HardDeleteSelected(object e = null)
        {
            if (Meta.IgnoreConfirmHardDelete && OnDeleteConfirmed != null)
            {
                OnDeleteConfirmed.Invoke();
                return;
            }
            var deletedItems = GetSelectedRows().ToList();
            var confirm = new ConfirmDialog();
            confirm.Title = $"Bạn có chắc xóa {deletedItems.Count} dòng dữ liệu không!";
            confirm.Render();
            confirm.YesConfirmed += () =>
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
                    deletedItems = GetFocusedRows();
                }
                this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforeDeleted, deletedItems).Done(() =>
                {
                    HardDeleteConfirmed(deletedItems).Done(res =>
                    {
                        DOMContentLoaded?.Invoke();
                        this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterDeleted, deletedItems).Done();
                    });
                });
            };
        }

        public virtual Task<string[]> Deactivate()
        {
            var tcs = new TaskCompletionSource<string[]>();
            var entity = Meta.RefName;
            var selected = GetSelectedRows();
            var ids = selected.Select(x => x[IdField] as string).ToArray();
            Client.Instance.DeactivateAsync(ids, Meta.RefName, ConnKey)
            .Done(deacvitedIds =>
            {
                if (deacvitedIds.HasElement())
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
                tcs.TrySetResult(deacvitedIds);
            });
            return tcs.Task;
        }

        public virtual Task<List<object>> HardDeleteConfirmed(List<object> deleted)
        {
            var entity = Meta.RefName;
            var ids = deleted.Select(x => x[IdField]?.ToString()).Where(x => x != null).ToList();
            var deletes = deleted.Where(x => x[IdField] != null).ToList();
            var removeRow = deleted.Where(x => x[IdField] == null).ToList();
            if (removeRow.Any())
            {
                RemoveRange(removeRow);
            }
            if (deleted.Nothing())
            {
                Toast.Success("Select at least 1 row to delete");
                return Task.FromResult(null as List<object>);
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
                Dirty = true;
                Toast.Success("Delete data success");
                return Task.FromResult(deleted);
            }
            var tcs = new TaskCompletionSource<List<object>>();
            Client.Instance.HardDeleteAsync(ids.ToArray(), Meta.RefName, ConnKey)
            .Done(sucess =>
            {
                if (sucess)
                {
                    Toast.Success("Delete data success");
                    AllListViewItem
                        .Where(x => x.Selected && ids.Contains(x.EntityId))
                        .ToArray().ForEach(x => x.Dispose());
                    tcs.TrySetResult(deleted);
                    return;
                }
                else
                {
                    Toast.Warning("No row was deleted");
                }
                tcs.TrySetResult(null);
            });
            return tcs.Task;
        }

        public virtual void RemoveRange(IEnumerable<object> deleted)
        {
            deleted.SelectForEach(x => RemoveRowById(x[IdField].As<string>()));
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
            if (LastListViewItem?.GroupRow == true)
            {
                return new List<object>() { LastListViewItem.Entity };
            }
            else
            {
                return MainSection.Children.Where(x => x is ListViewItem item && item.Selected).Select(x => x.Entity).ToList();
            }
        }

        public virtual Task<List<object>> GetRealTimeSelectedRows()
        {
            var tcs = new TaskCompletionSource<List<object>>();
            Client.Instance.GetByIdAsync(Meta.RefName, ConnKey ?? Client.ConnKey, SelectedIds.ToArray())
                .Done(res =>
                {
                    tcs.TrySetResult(res?.ToList());
                });
            return tcs.Task;
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

            Toast.Success("Đang Sao chép liệu !");
            this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforePasted, _originRows, _copiedRows).Done(() =>
            {
                var index = AllListViewItem.IndexOf(x => x.Selected);
                AddRowsNo(_copiedRows, index).Done(list =>
                {
                    base.Focus();
                    if (Meta.IsRealtime)
                    {
                        foreach (var item in list)
                        {
                            item.PatchUpdateOrCreate();
                        }
                        Toast.Success("Sao chép dữ liệu thành công !");
                        base.Dirty = false;
                        ClearSelected();
                    }
                    else
                    {
                        Toast.Success("Sao chép dữ liệu thành công !");
                    }
                    this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterPasted, _originRows, _copiedRows).Done();
                });
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
            var copiedRows = ReflectionExt.CloneRows(originalRows);
            if (copiedRows.Nothing() || !CanWrite)
            {
                return;
            }

            Toast.Success("Đang Sao chép liệu !");
            this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforePasted, originalRows, copiedRows)
            .Done(() =>
            {
                var index = AllListViewItem.IndexOf(x => x.Selected);
                if (addRow)
                {
                    if (Meta.TopEmpty)
                    {
                        index = 0;
                    }
                    else
                    {
                        index = AllListViewItem.LastOrDefault().RowNo;
                    }
                }
                AddRowsNo(copiedRows, index).Done(list =>
                {
                    DuplicateRowSuccess(list, originalRows, copiedRows);
                });
            });
        }

        private void DuplicateRowSuccess(ListViewItem[] list, List<object> originalRows, IEnumerable<object> copiedRows)
        {
            list.ForEach(x => x.Dirty = true);
            base.Focus();
            ComponentExt.DispatchCustomEvent(this, Meta.Events, CustomEventType.AfterPasted, originalRows, copiedRows)
            .Done(() =>
            {
                RenderIndex();
                ClearSelected();
                if (Meta.IsRealtime)
                {
                    foreach (var item in list)
                    {
                        item.PatchUpdateOrCreate();
                        item.Dirty = false;
                    }
                }
                Toast.Success("Sao chép dữ liệu thành công !");
            });
        }

        public virtual Task AddOrUpdateRow(object rowData, bool singleAdd = true, bool force = false, params string[] fields)
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
                return AddRow(rowData, RowData.Data.Count - 1, singleAdd);
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
                AddNewEmptyRow();
            }
            return Task.FromResult(true);
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
            this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforeCreated, rowData).Done(() =>
            {
                var row = RenderRowData(Header, rowData, MainSection, index);
                tcs.TrySetResult(row);
                this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterCreated, rowData).Done();
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
            rows.SelectForEach(row =>
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
            await this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforeCreatedList, rows);
            var listItem = new List<ListViewItem>();
            indextemp = index;
            await rows.AsEnumerable().Reverse().ForEachAsync(async data =>
            {
                listItem.Add(await AddRow(data, indextemp, false));
                indextemp++;
            });
            await this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterCreatedList, rows);
            AddNewEmptyRow();
            return listItem;
        }

        public virtual Task<ListViewItem[]> AddRowsNo(IEnumerable<object> rows, int index = 0)
        {
            if (index < 0)
            {
                index = 0;
            }
            var indextemp = index;
            rows.SelectForEach(row =>
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
            var tcs = new TaskCompletionSource<ListViewItem[]>();
            this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforeCreatedList, rows)
            .Done(() =>
            {
                var tasks = rows.SelectForEach((data, innerIndex) =>
                {
                    return AddRow(data, innerIndex + index, false);
                });
                Task.WhenAll(tasks).Done((result) =>
                {
                    AddNewEmptyRow();
                    this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterCreatedList, rows).Done();
                    tcs.TrySetResult(result);
                });
            });
            return tcs.Task;
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
            if (RowData?.Data is IList list && list != null) list.Clear();
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
                    ListViewSearch.RefershListView();
                }
            }
            else
            {
                RowAction(row => !row.EmptyRow, row => row.UpdateView(force, dirty, componentNames));
            }
        }

        public override Task<bool> ValidateAsync()
        {
            ValidationResult.Clear();
            ValidateRequired();
            return Task.FromResult(IsValid);
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
                ValidationResult.TryAdd(ValidationRule.Required, string.Format(requiredRule.Message, LangSelect.Get(Meta.Label), Entity));
                return false;
            }
        }

        public void CopySelected(object ev)
        {
            _originRows = GetSelectedRows();
            _copiedRows = ReflectionExt.CloneRows(_originRows);
            (Window.Instance as dynamic).navigator.clipboard.writeText(JSON.Stringify(_copiedRows));
            Task.Run(async () =>
            {
                await this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterCopied, _originRows, _copiedRows);
            });
        }

        private void DisposeHistory()
        {
            _history.Remove();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "<Pending>")]
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
            com.Id = Uuid7.Id25();
            com.FieldName = nameof(AdvSearchVM.Conditions);
            com.Column = 4;
            com.ReferenceId = Utils.GetEntity(nameof(Models.History)).Id;
            com.RefName = nameof(Models.History);
            var _filterGrid = new GridView(com);
            _filterGrid.Meta.LocalHeader = new List<Component>
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
                    ComponentType = nameof(SearchEntry),
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
                Entity = new SecurityVM { RecordIds = selectedRowIds, EntityId = Meta.ReferenceId },
                ParentElement = TabEditor.Element
            };
            TabEditor.AddChild(security);
        }

        internal bool CanDo(Func<FeaturePolicy, bool> predicate)
        {
            var hasPermission = GridPolicies.Any(predicate);
            return hasPermission || GeneralPolicies.Any(predicate);
        }

        protected List<FeaturePolicy> RecordPolicy = new List<FeaturePolicy>();
        public void TbodyContextMenu(Event e)
        {
            ContextMenu.Instance.MenuItems.Clear();
            BodyContextMenuShow?.Invoke();
            if (Disabled)
            {
                return;
            }
            SetSelected(e);
            var selectedRows = GetSelectedRows();
            var ctxMenu = ContextMenu.Instance;
            RenderRelatedDataMenu().Done(() =>
            {
                RenderCopyPasteMenu(CanWrite);
                RenderEditMenu(CanWrite);
                RenderShareMenu(selectedRows).Done(() =>
                {
                    ctxMenu.Top = e.Top();
                    ctxMenu.Left = e.Left();
                    ctxMenu.Render();
                    Element.AppendChild(ctxMenu.Element);
                    ctxMenu.Element.Style.Position = Position.absolute.ToString();
                });
            });
        }

        private void SetSelected(Event e)
        {
            var target = e.Target as HTMLElement;
            var rawRow = target.Closest(ElementType.tr.ToString());
            var currentRow = this.FirstOrDefault(x => x.Element == rawRow) as ListViewItem;
            if (currentRow is null) return;
            if (!(currentRow is GroupViewItem) || Meta.GroupReferenceId != null)
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

        public Task RenderRelatedDataMenu()
        {
            var tcs = new TaskCompletionSource<object>();
            Client.Instance.GetByIdAsync(nameof(EntityRef), Meta.ConnKey, Meta.Id)
            .Done(targetRef =>
            {
                if (targetRef.Nothing())
                {
                    tcs.TrySetResult(null);
                    return;
                }
                var tRef = targetRef.As<EntityRef[]>();
                var menuItems = tRef.Select(x => new ContextMenuItem
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
                tcs.TrySetResult(null);
            });
            return tcs.Task;
        }
        public bool _hasLoadRef { get; set; }
        public ListViewSection EmptySection { get; set; }
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
            ComponentExt.LoadFeature(ConnKey, e.ViewClass).Done(currentFeature =>
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
                    var gridView1 = instance.FilterChildren<GridView>().FirstOrDefault(x => x.Meta.Id == e.TargetComId);
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
            var gridView1 = fe.FilterChildren<GridView>().FirstOrDefault(x => x.Meta.Id == e.TargetComId);
            if (gridView1 is null)
            {
                return;
            }
            gridView1.CellSelected.Clear();
            gridView1.AdvSearchVM.Conditions.Clear();
            gridView1.ListViewSearch.EntityVM.StartDate = null;
            gridView1.ListViewSearch.EntityVM.EndDate = null;
            GetRealTimeSelectedRows().Done(selecteds =>
            {
                var com = Enumerable.FirstOrDefault(gridView1.Header, x => x.FieldName == e.TargetFieldName);
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
            });
        }

        public virtual void RenderCopyPasteMenu(bool canWrite)
        {
            if (canWrite)
            {
                ContextMenu.Instance.MenuItems.Add(new ContextMenuItem { Icon = "fa fa-copy", Text = "Copy", Click = CopySelected });
                ContextMenu.Instance.MenuItems.Add(new ContextMenuItem { Icon = "fa fa-clone", Text = "Copy & Dán", Click = (e) => DuplicateSelected(null, false) });
            }
            if (canWrite && _copiedRows.HasElement())
            {
                ContextMenu.Instance.MenuItems.Add(new ContextMenuItem { Icon = "fal fa-paste", Text = "Dán", Click = PasteSelected });
            }
        }

        private void RenderEditMenu(bool canWrite)
        {
            if (canWrite)
                ContextMenu.Instance.MenuItems.Add(new ContextMenuItem
                {
                    Icon = "fal fa-history",
                    Text = "Xem lịch sử",
                    Click = ViewHistory,
                });
            if (CanDo(x => x.CanDeactivate || x.CanDeactivate))
                ContextMenu.Instance.MenuItems.Add(new ContextMenuItem
                {
                    Icon = "mif-unlink",
                    Text = "Hủy (không xóa)",
                    Click = DeactivateSelected,
                });
            if (CanDo(x => x.CanDelete || x.CanDeleteAll))
                ContextMenu.Instance.MenuItems.Add(new ContextMenuItem
                {
                    Icon = "fa fa-trash",
                    Text = "Xóa dữ liệu",
                    Click = HardDeleteSelected,
                });
        }

        private Task RenderShareMenu(List<object> selectedRows)
        {
            if (selectedRows.Nothing()) return Task.FromResult(true);
            var noPolicyRows = selectedRows.Where(x =>
            {
                var hasPolicy = RecordPolicy.Any(f => f.EntityId == Meta.ReferenceId && f.RecordId == x[IdField]?.ToString());
                var loaded = x[PermissionLoaded].As<bool?>();
                return !(hasPolicy || loaded == true);
            });
            var noPolicyRowIds = noPolicyRows.Select(x => x[IdField].As<string>()).ToArray();
            var tcs = new TaskCompletionSource<object>();
            LoadRecordPolicy(ConnKey, Meta.RefName, noPolicyRowIds).Done(rowPolicy =>
            {
                rowPolicy.ForEach(RecordPolicy.Add);
                noPolicyRows.ForEach(x => x[PermissionLoaded] = true);
                var ownedRecords = selectedRows.Where(x =>
                {
                    var isOwner = Utils.IsOwner(x);
                    x[IsOwner] = isOwner;
                    return isOwner;
                }).Select(x => x[IdField].As<string>()).ToList();
                var canShare = CanDo(x => x.CanShare || x.CanShareAll) && ownedRecords.Any();
                if (!canShare)
                {
                    tcs.TrySetResult(null);
                    return;
                }
                ContextMenu.Instance.MenuItems.Add(new ContextMenuItem
                {
                    Icon = "mif-security",
                    Text = "Bảo mật & Phân quyền",
                    Click = SecurityRows,
                });
                tcs.TrySetResult(null);
            });
            return tcs.Task;
        }

        public static Task<FeaturePolicy[]> LoadRecordPolicy(string connKey, string entity, string[] ids)
        {
            if (ids.Nothing() || ids.All(x => x == null))
            {
                return Task.FromResult(new FeaturePolicy[] { });
            }
            var sql = new SqlViewModel
            {
                ComId = "Policy",
                Action = "GetById",
                Table = nameof(FeaturePolicy),
                ConnKey = connKey,
                Params = JSON.Stringify(new { ids, table = entity })
            };
            var xhr = new XHRWrapper
            {
                Method = HttpMethod.POST,
                Url = Utils.UserSvc,
                IsRawString = true,
                Value = JSON.Stringify(sql)
            };
            return Client.Instance.SubmitAsync<FeaturePolicy[]>(xhr);
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

        public void ClearSelected(params string[] ids)
        {
            var shouldClear = ids.Any() ? SelectedIds.Intersect(ids).ToArray() : SelectedIds.ToArray();
            shouldClear.ForEach(x => SelectedIds.Remove(x));
            AllListViewItem.Where(x => shouldClear.Contains(x.EntityId)).ForEach(x => x.Selected = false);
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

        public virtual List<PatchVM> GetPatches(bool updateView = false)
        {
            if (!Dirty)
            {
                return null;
            }
            if (Meta.IdField != null)
            {
                UpdatedRows.ForEach(row => row[Meta.IdField] = EntityId);
            }
            var res = new List<PatchVM>();
            foreach (var item in UpdatedListItems)
            {
                res.Add(item.GetPatchEntity());
            }
            return res;
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

        public Task<object[][]> GetUserSetting(string prefix)
        {
            return Client.Instance.SubmitAsync<object[][]>(new XHRWrapper
            {
                Url = Utils.UserSvc,
                Value = new SqlViewModel
                {
                    ComId = "UserSetting",
                    Action = "GetByComId",
                    Params = JSON.Stringify(new { ComId = Meta.Id, Prefix = prefix })
                },
                Method = HttpMethod.POST
            });
        }

        public Task<bool> UpdateSetting(UserSetting setting, string prefix, string value)
        {
            var tcs = new TaskCompletionSource<bool>();
            PatchVM patch;
            if (setting is null)
            {
                patch = CreateSettingPatch(prefix, Uuid7.Id25(), value);
            }
            else
            {
                setting.Value = value;
                patch = CreateSettingPatch(prefix, setting.Id, value, setting.Id);
            }
            Client.Instance.PatchAsync(patch)
            .Done(r =>
            {
                tcs.TrySetResult(r > 0);
            });
            return tcs.Task;
        }

        public PatchVM CreateSettingPatch(string prefix, string newId, string value, string oldId = null)
        {
            var patch = new PatchVM
            {
                Changes = new List<PatchDetail> {
                    new PatchDetail { Field = nameof(UserSetting.Name), Value = $"{prefix}-{Meta.Id}" },
                    new PatchDetail { Field = nameof(UserSetting.UserId), Value = Client.Token.UserId },
                    new PatchDetail { Field = nameof(UserSetting.Value), Value = value },
                },
                Table = nameof(UserSetting),
                ConnKey = ConnKey ?? Client.ConnKey
            };
            patch.Changes.Add(new PatchDetail { Field = IdField, Value = newId, OldVal = oldId });
            return patch;
        }

        public override void Dispose()
        {
            Window.RemoveEventListener("", RealtimeUpdateListViewItem);
            base.Dispose();
        }
    }
}
