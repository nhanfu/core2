using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class VirtualGrid : GridView
    {
        private const string RowNo = "_index";
        private const string VirtualRow = "virtualRow";
        private int _renderViewPortAwaiter;
        internal bool _renderingViewPort;
        internal List<object> LastData;
        internal bool _waitingLoad;
        internal bool _f6;
        internal int viewPortCount;
        internal int _skip;
        internal static int cacheAhead = 5;
        private int _renderPrepareCacheAwaiter;

        public VirtualGrid(Component ui) : base(ui)
        {
        }

        internal override void AddSections()
        {
            base.AddSections();
            DataTable.ParentElement.AddEventListener(EventType.Scroll.ToString(), RenderViewPortWrapper);
            DataTable.ParentElement.AddEventListener(EventType.Resize.ToString(), (e) =>
            {
                _lastScrollTop = -1;
                RenderViewPortWrapper(e);
            });
        }

        public override async Task ApplyFilter(bool searching = true)
        {
            _sum = false;
            CacheData.Clear();
            var calcFilter = CalcFilterQuery(searching);
            DataTable.ParentElement.ScrollTop = 0;
            await ReloadData(calcFilter, cache: false);
        }

        private async Task PrepareCache(int skip = 0)
        {
            if (CacheData.HasElement())
            {
                var firstRowNo = (int)CacheData.First()[RowNo];
                var shouldEndNo = firstRowNo + viewPortCount * cacheAhead;
                if (skip > firstRowNo && skip < shouldEndNo)
                {
                    _waitingLoad = false;
                    return;
                }
            }
            var start = skip - viewPortCount * cacheAhead;
            if (start < 0)
            {
                start = 0;
            }
            var source = CalcDatasourse(viewPortCount + viewPortCount * cacheAhead * 2, start, "false");
            if (!GuiInfo.DescValue.IsNullOrWhiteSpace())
            {
                source = OdataExt.AppendClause(source, GuiInfo.DescValue, "$select=");
            }
            var data = await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetList<object>(source);
            if (data.Value.Nothing())
            {
                _waitingLoad = false;
                return;
            }
            CacheData.Clear();
            CacheData.AddRange(data.Value);
            CacheData.ForEach((x, index) => x[RowNo] = start + index + 1);
            await LoadMasterData(data.Value, spinner: false);
            _waitingLoad = false;
        }

        internal override async Task RenderViewPort(bool count = true, bool firstLoad = false)
        {
            _renderingViewPort = true;
            viewPortCount = GetViewPortItem();
            var scrollTop = DataTable.ParentElement.ScrollTop;
            if (scrollTop == _lastScrollTop)
            {
                return;
            }
            SetRowHeight();
            var skip = GetRowCountByHeight(scrollTop);
            if (viewPortCount <= 0)
            {
                viewPortCount = GuiInfo.Row ?? 20;
            }
            List<object> rows;
            if (firstLoad)
            {
                rows = await FirstLoadData(count, skip);
            }
            else
            {
                rows = ReadCache(skip, viewPortCount).ToList();
                if (rows.Count < viewPortCount && rows.Count < Paginator.Options.Total)
                {
                    rows = await FirstLoadData(count, skip);
                }
            }
            FormattedRowData = rows;
            if (scrollTop == 0)
            {
                LastData = FormattedRowData;
            }
            RenderVirtualRow(MainSection.Element as HTMLTableSectionElement, skip, viewPortCount);
            UpdateExistRows(false);
            var existBottomEle = MainSection.Element.Children
                .FirstOrDefault(x => x.GetAttribute(VirtualRow) == Direction.bottom.ToString());
            MainSection.Element.AppendChild(existBottomEle);
            RowData.Data.Clear();
            rows.ForEach(RowData.Data.Add);
            Entity?.SetComplexPropValue(GuiInfo.FieldName, rows);
            SetRowHeight();
            RowAction(x => x.Focused = false);
            SetFocusingCom();
            _renderingViewPort = false;
            RenderIndex();
            DomLoaded();
        }

        private IEnumerable<object> ReadCache(int skip, int viewPortCount)
        {
            var index = -1;
            try
            {
                index = CacheData.IndexOf(x => (int)x[RowNo] == skip + 1);
            }
            catch
            {
                CacheData.Clear();
                yield break;
            }
            if (index < 0)
            {
                CacheData.Clear();
                yield break;
            }
            while (viewPortCount > 0 && CacheData.Count > index)
            {
                yield return CacheData[index];
                index++;
                viewPortCount--;
            }
        }

        private async Task<List<object>> FirstLoadData(bool count, int skip)
        {
            _skip = skip;
            List<object> rows;
            var source = CalcDatasourse(viewPortCount, skip, count ? "true" : "false");
            if (!GuiInfo.DescValue.IsNullOrWhiteSpace())
            {
                source = OdataExt.AppendClause(source, GuiInfo.DescValue, "$select=");
            }
            var oDataRows = await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetList<object>(source);
            Sql = oDataRows.Sql;
            rows = oDataRows.Value;
            if (Paginator != null && count)
            {
                Paginator.Options.Total = oDataRows.Odata.Count ?? rows.Count;
            }
            FormattedRowData = rows;
            await LoadMasterData(FormattedRowData, spinner: false);
            rows.ForEach((x, index) => x[RowNo] = skip + index + 1);
            if (rows.Count < Paginator.Options.Total)
            {
                Window.ClearTimeout(_renderPrepareCacheAwaiter);
                _waitingLoad = true;
                _renderPrepareCacheAwaiter = Window.SetTimeout(async () => await PrepareCache(skip), 7000);
            }
            return rows;
        }

        public override async Task<List<object>> ReloadData(string dataSource = null, bool cache = false, int? skip = null, int? pageSize = null, bool search = false)
        {
            DisposeNoRecord();
            VirtualScroll = GuiInfo.GroupBy.Nothing() && GuiInfo.VirtualScroll && Element.Style.Display.ToString() != Display.None.ToString();
            _lastScrollTop = -1;
            await RenderViewPort(firstLoad: true);
            return FormattedRowData;
        }

        private void RenderViewPortWrapper(Event e)
        {
            if (_waitingLoad)
            {
                Window.ClearTimeout(_renderPrepareCacheAwaiter);
                _renderPrepareCacheAwaiter = Window.SetTimeout(async () => await PrepareCache(_skip), 7000);
            }
            if (_renderingViewPort || !VirtualScroll)
            {
                _renderingViewPort = false;
                e.PreventDefault();
                return;
            }
            Window.ClearTimeout(_renderViewPortAwaiter);
            _renderViewPortAwaiter = Window.SetTimeout(async () => await RenderViewPort(false), 100);
        }

        internal void RenderVirtualRow(HTMLTableSectionElement tbody, int skip, int viewPort)
        {
            var existTopEle = tbody.Children.FirstOrDefault(x => x.GetAttribute(VirtualRow) == Direction.top.ToString());
            var topVirtualRow = existTopEle ?? Document.CreateElement(ElementType.tr.ToString());
            if (!topVirtualRow.HasClass("demo"))
            {
                topVirtualRow.AddClass("demo");
                for (int i = 0; i < Header.Count; i++)
                {
                    topVirtualRow.AppendChild(Document.CreateElement(ElementType.td.ToString()));
                }
            }
            topVirtualRow.Style.Height = skip * _rowHeight + Utils.Pixel;
            topVirtualRow.SetAttribute(VirtualRow, Direction.top.ToString());
            tbody.InsertBefore(topVirtualRow, tbody.FirstChild);

            var existBottomEle = tbody.Children.LastOrDefault(x => x.GetAttribute(VirtualRow) == Direction.bottom.ToString());
            var bottomVirtualRow = existBottomEle ?? Document.CreateElement(ElementType.tr.ToString());
            if (!bottomVirtualRow.HasClass("demo"))
            {
                bottomVirtualRow.AddClass("demo");
                for (int i = 0; i < Header.Count; i++)
                {
                    bottomVirtualRow.AppendChild(Document.CreateElement(ElementType.td.ToString()));
                }
            }
            var bottomHeight = (Paginator.Options.Total - viewPort - skip) * _rowHeight;
            bottomHeight = bottomHeight >= _rowHeight ? bottomHeight : 0;
            bottomVirtualRow.Style.Height = bottomHeight + Utils.Pixel;
            bottomVirtualRow.SetAttribute(VirtualRow, Direction.bottom.ToString());
            tbody.AppendChild(bottomVirtualRow);
            MainSection.Element.ParentElement.ParentElement.ScrollTop = skip * _rowHeight;
            _lastScrollTop = skip * _rowHeight;
            Paginator.Options.PageSize = Paginator.Options.Total;
            Paginator.Options.StartIndex = skip + 1;
            Paginator.Options.EndIndex = skip + viewPort;
            Paginator.Element.AddClass("infinite-scroll");
            Paginator.Children.ForEach(child => child.UpdateView());
        }

        public override void DisposeSumary()
        {
            _renderPrepareCacheAwaiter = Window.SetTimeout(async () => await PrepareCache(_skip), 7000);
            base.DisposeSumary();
        }

        protected override void HotKeyF6Handler(Event e)
        {
            var keyCode = e.KeyCodeEnum();
            if (keyCode == KeyCodeEnum.F6)
            {
                e.PreventDefault();
                e.StopPropagation();
                if (_summarys.Any())
                {
                    var lastElement = _summarys.LastOrDefault();
                    if (lastElement.InnerHTML == string.Empty)
                    {
                        if (CellSelected.Count > 0)
                        {
                            CellSelected.RemoveAt(CellSelected.Count - 1);
                            if (Wheres.Count - 1 >= 0)
                            {
                                Wheres.RemoveAt(Wheres.Count - 1);
                            }
                            AdvSearchVM.Conditions.RemoveAt(AdvSearchVM.Conditions.Count - 1);
                            Task.Run(async () =>
                            {
                                await ActionFilter();
                            });
                        }
                        _summarys.RemoveAt(_summarys.Count - 1);
                    }
                    else
                    {
                        if (_waitingLoad)
                        {
                            Window.ClearTimeout(_renderPrepareCacheAwaiter);
                        }
                        if (lastElement.Style.Display.ToString() == "none")
                        {
                            if (CellSelected.Count > 0)
                            {
                                CellSelected.RemoveAt(CellSelected.Count - 1);
                                if (Wheres.Count - 1 >= 0)
                                {
                                    Wheres.RemoveAt(Wheres.Count - 1);
                                }
                                AdvSearchVM.Conditions.RemoveAt(AdvSearchVM.Conditions.Count - 1);
                                Task.Run(async () =>
                                {
                                    await ActionFilter();
                                });
                            }
                            lastElement.Show();
                        }
                        else
                        {
                            _summarys.RemoveAt(_summarys.Count - 1);
                            lastElement.Remove();
                        }
                    }
                }
            }
            else if (keyCode == KeyCodeEnum.F3)
            {
                e.PreventDefault();
                e.StopPropagation();
                Task.Run(async () =>
                {
                    var selected = await GetRealTimeSelectedRows();
                    if (selected.Count == 0)
                    {
                        selected = RowData.Data.ToList();
                    }
                    var numbers = Header.Where(x => x.ComponentType == nameof(Number)).ToList();
                    if (numbers.Count == 0)
                    {
                        Toast.Warning("Vui lòng cấu hình");
                        return;
                    }
                    var listString = numbers.Select(x =>
                    {
                        var val = selected.Select(k => k[x.FieldName]).Where(k => k != null).Select(y => Convert.ToDecimal(y)).Sum();
                        return x.ShortDesc + " : " + (val % 2 > 0 ? val.ToString("N2") : val.ToString("N0"));
                    });
                    Toast.Success(listString.Combine("</br>"), 6000);
                });
            }
            else if (keyCode == KeyCodeEnum.F1)
            {
                e.PreventDefault();
                e.StopPropagation();
                ToggleAll();
            }
        }

        public override void ViewSumary(object ev, GridPolicy header)
        {
            if (_waitingLoad)
            {
                Window.ClearTimeout(_renderPrepareCacheAwaiter);
            }
            Html.Take(Element).Div.ClassName("backdrop")
            .Style("align-items: center;").Escape((e) => DisposeSumary());
            _summarys.Add(Html.Context);
            Html.Instance.Div.ClassName("popup-content confirm-dialog").Style("top: 0;min-width: 90%")
                .Div.ClassName("popup-title").InnerHTML("Gộp theo cột hiện thời")
                .Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                    .Event(EventType.Click, DisposeSumary)
                .EndOf(".popup-title")
                .Div.ClassName("popup-body scroll-content");
            Html.Instance.Div.ClassName("container-rpt");
            Html.Instance.Div.ClassName("menuBar");
            Html.Instance.EndOf(".menuBar");
            Html.Instance.Div.ClassName("printable");
            var body = Html.Context;
            Task.Run(async () =>
            {
                var filter = Wheres.Where(x => !x.Group).Select(x => x.FieldName).Combine(" and ");
                var filter1 = Wheres.Where(x => x.Group).Select(x => x.FieldName).Combine(" or ");
                var wh = new List<string>();
                if (!filter.IsNullOrWhiteSpace())
                {
                    wh.Add($"({filter})");
                }
                if (!filter1.IsNullOrWhiteSpace())
                {
                    wh.Add($"({filter1})");
                }
                var stringWh = wh.Any() ? $"({wh.Combine(" and ")})" : "";
                var gridPolicy = BasicHeader.Where(x => x.ComponentType == nameof(Number) && x.FieldName != header.FieldName).ToList();
                var sum = gridPolicy.Select(x => $"FORMAT(SUM(isnull([{GuiInfo.RefName}].{x.FieldName},0)),'#,#') as {x.FieldName}").ToList();
                var submitEntity = GuiInfo.PreQuery;
                if (_preQueryFn != null)
                {
                    submitEntity = (string)_preQueryFn.Call(null, this);
                }
                var dataSet = await new Client(GuiInfo.RefName).PostAsync<object[][]>(sum.Combine(), $"ViewSumary?group={header.FieldName}" +
                    $"&tablename={GuiInfo.RefName}" +
                    $"&refname={header.RefName}" +
                    $"&formatsumary={GuiInfo.FormatSumaryField}" +
                    $"&sql={Sql}&orderby={GuiInfo.OrderBySumary}" +
                    $"&where={stringWh} {(submitEntity.IsNullOrWhiteSpace() ? "" : $"{(Wheres.Any() ? " and " : "")} {submitEntity}")}");
                var sumarys = dataSet[0];
                object[] refn = null;
                if (dataSet.Length > 1)
                {
                    refn = dataSet[1];
                }
                var id = "sumary" + (new Random(10)).GetHashCode();
                var dir = refn?.ToDictionary(x => x[IdField]);
                Html.Instance.Div.ClassName("grid-wrapper sticky").Div.ClassName("table-wrapper printable").Table.Id(id).Width("100%").ClassName("table")
                .Thead
                    .TRow.Render();
                Html.Instance.Th.Style("max-width: 100%;").IText(header.ShortDesc).End.Render();
                Html.Instance.Th.Style("max-width: 100%;").IText("Tổng dữ liệu").End.Render();
                foreach (var item in gridPolicy)
                {
                    Html.Instance.Th.Style("max-width: 100%;").IHtml(item.ShortDesc).End.Render();
                }
                Html.Instance.EndOf(ElementType.thead);
                Html.Instance.TBody.Render();
                var ttCount = sumarys.Sum(x => Convert.ToDecimal(x["TotalRecord"].ToString().Replace(",", "") == "" ? "0" : x["TotalRecord"].ToString().Replace(",", "")));
                foreach (var item in sumarys)
                {
                    item[header.FieldName] = item[header.FieldName] ?? "";
                    var dataHeader = item[header.FieldName].ToString();
                    var value = string.Empty;
                    var valueText = string.Empty;
                    if (header.ComponentType == "Dropdown")
                    {
                        var ob = dir.GetValueOrDefault(item[header.FieldName]);
                        if (ob is null)
                        {
                            dataHeader = "";
                        }
                        else
                        {
                            dataHeader = ob[header.FormatCell.Split("}")[0].Replace("{", "")].ToString();
                            value = ob["Id"].ToString();
                            valueText = dataHeader;
                        }
                    }
                    else if (header.ComponentType == nameof(Datepicker))
                    {
                        var datetime = DateTimeExt.TryParseDateTime(item[header.FieldName].ToString());
                        dataHeader = datetime?.ToString("dd/MM/yyyy");
                        value = datetime?.ToString("dd/MM/yyyy");
                        valueText = datetime?.ToString("dd/MM/yyyy");
                    }
                    else if (header.ComponentType == nameof(Number))
                    {
                        var datetime = (item[header.FieldName] is null || item[header.FieldName].ToString() == "") ? default(decimal) : decimal.Parse(item[header.FieldName].ToString());
                        dataHeader = datetime == default(decimal) ? "" : datetime.ToString("N0");
                        value = item[header.FieldName].ToString();
                        valueText = dataHeader;
                    }
                    else
                    {
                        value = item[header.FieldName].ToString();
                        valueText = item[header.FieldName].ToString();
                    }
                    Html.Instance.TRow.Event(EventType.DblClick, () => FilterSumary(header, value, valueText)).Event(EventType.Click, (e) => FocusCell(e, this.HeaderComponentMap[header.GetHashCode()])).Render();
                    Html.Instance.TData.Style("max-width: 100%;").ClassName(header.ComponentType == nameof(Number) ? "text-right" : "text-left").IText(dataHeader.DecodeSpecialChar()).End.Render();
                    Html.Instance.TData.Style("max-width: 100%;").ClassName("text-right").IText(item["TotalRecord"].ToString()).End.Render();
                    foreach (var itemDetail in gridPolicy)
                    {
                        Html.Instance.TData.Style("max-width: 100%;").ClassName("text-right").IText(item[itemDetail.FieldName].ToString()).End.Render();
                    }
                    Html.Instance.EndOf(ElementType.tr);
                }
                Html.Instance.EndOf(ElementType.tbody);
                Html.Instance.TFooter.TRow.ClassName("summary").Render();
                Html.Instance.TData.Style("max-width: 100%;").IText("Tổng cộng").End.Render();
                Html.Instance.TData.ClassName("text-right").Style("max-width: 100%;").IText(ttCount.ToString("N0")).End.Render();
                foreach (var item in gridPolicy)
                {
                    var de = sumarys.Select(x => x[item.FieldName].ToString().Replace(",", "")).ToList();
                    var ttCount1 = de.Where(x => !x.IsNullOrWhiteSpace()).Sum(x => decimal.Parse(x));
                    Html.Instance.TData.ClassName("text-right").Style("max-width: 100%;").IHtml(ttCount1.ToString("N0")).End.Render();
                }
                await Client.LoadScript("//cdn.datatables.net/1.13.2/js/jquery.dataTables.min.js");
                await Client.LoadScript("//cdn.datatables.net/buttons/2.3.4/js/dataTables.buttons.min.js");
                await Client.LoadScript("//cdnjs.cloudflare.com/ajax/libs/jszip/3.1.3/jszip.min.js");
                await Client.LoadScript("//cdnjs.cloudflare.com/ajax/libs/pdfmake/0.1.53/pdfmake.min.js");
                await Client.LoadScript("//cdnjs.cloudflare.com/ajax/libs/pdfmake/0.1.53/vfs_fonts.js");
                await Client.LoadScript("//cdn.datatables.net/buttons/2.3.4/js/buttons.html5.min.js");
                await Client.LoadScript("//cdn.datatables.net/buttons/2.3.4/js/buttons.html5.min.js");
                await Client.LoadScript("//cdn.datatables.net/buttons/2.3.4/js/buttons.print.min.js");
                /*@
                if (!$.fn.DataTable.isDataTable('#'+id)){
                  $('#'+id).DataTable({
                    paging: false,
                    info: false,
                    dom: 'Bfrtip',
                    buttons: [
                        'copy', 'csv', 'excel', 'pdf', 'print'
                    ]
                });
                }
                */
            });
        }

        private void FocusCell(Event e, Component header)
        {
            var td = e.Target as HTMLElement;

            /*@
             var $table = $(e.target).closest('table');
             $table.find("tbody tr").removeClass("focus");
             $table.find("tbody td").removeClass("cell-selected");
             */

            td.Closest(ElementType.tr.ToString()).AddClass("focus");
            td.Closest(ElementType.td.ToString()).AddClass("cell-selected");
        }

        public override void FilterInSelected(object ev)
        {
            if (_waitingLoad)
            {
                Window.ClearTimeout(_renderPrepareCacheAwaiter);
            }
            if (ev["Operator"] is null)
            {
                return;
            }
            var header = Header.FirstOrDefault(x => x.FieldName == ev["FieldName"].ToString());
            var subFilter = string.Empty;
            var lastFilter = Window.LocalStorage.GetItem("LastSearch" + GuiInfo.Id + header.Id);
            if (lastFilter != null)
            {
                subFilter = lastFilter.ToString();
            }
            var confirmDialog = new ConfirmDialog
            {
                Content = $"Nhập {header.ShortDesc} cần tìm" + ev["Text"],
                NeedAnswer = true,
                MultipleLine = false,
                ComType = header.ComponentType == nameof(Datepicker) ? header.ComponentType : nameof(Textbox)
            };
            confirmDialog.YesConfirmed += async () =>
            {
                string value = null;
                string valueText = null;
                if (header.ComponentType == nameof(Datepicker))
                {
                    valueText = confirmDialog.Datepicker.OriginalText;
                    value = confirmDialog.Datepicker.Value.ToString();
                }
                else
                {
                    valueText = confirmDialog.Textbox.Text.Trim().EncodeSpecialChar();
                    value = confirmDialog.Textbox.Text.Trim().EncodeSpecialChar();
                }
                Window.LocalStorage.SetItem("LastSearch" + GuiInfo.Id + header.Id, value);
                if (!CellSelected.Any(x => x.FieldName == ev["FieldName"].ToString() && x.Value == value && x.Operator == ev["Operator"].ToString()))
                {
                    if (CellSelected.Any(x => x.FieldName == ev["FieldName"].ToString() && x.Operator == "in") && !(bool)ev["Shift"])
                    {
                        CellSelected.FirstOrDefault(x => x.FieldName == ev["FieldName"].ToString() && x.Operator == "in").Value = value;
                        CellSelected.FirstOrDefault(x => x.FieldName == ev["FieldName"].ToString() && x.Operator == "in").ValueText = valueText;
                    }
                    else
                    {
                        CellSelected.Add(new CellSelected
                        {
                            FieldName = ev["FieldName"].ToString(),
                            FieldText = header.ShortDesc,
                            ComponentType = header.ComponentType,
                            Value = value,
                            ValueText = valueText,
                            Operator = ev["Operator"].ToString(),
                            OperatorText = ev["OperatorText"].ToString(),
                        });
                    }
                }
                _summarys.Add(new HTMLElement());
                await ActionFilter();
                confirmDialog.Textbox.Text = null;
            };
            confirmDialog.Canceled += () =>
            {
                _renderPrepareCacheAwaiter = Window.SetTimeout(async () => await PrepareCache(_skip), 7000);
            };
            confirmDialog.Entity = new { ReasonOfChange = string.Empty };
            confirmDialog.Render();
            if (!subFilter.IsNullOrWhiteSpace())
            {
                if (header.ComponentType == nameof(Datepicker))
                {
                    confirmDialog.Datepicker.Value = DateTime.Parse(subFilter);
                    var input = confirmDialog.Datepicker.Element as HTMLInputElement;
                    input.SelectionStart = 0;
                    input.SelectionEnd = subFilter.Length;
                }
                else
                {
                    confirmDialog.Textbox.Text = subFilter;
                    var input = confirmDialog.Textbox.Element as HTMLInputElement;
                    input.SelectionStart = 0;
                    input.SelectionEnd = subFilter.Length;
                }
            }
        }
    }
}
