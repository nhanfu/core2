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
        internal bool _f6;
        internal int viewPortCount;
        internal int _skip;
        internal static int cacheAhead = 5;

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
            RowAction(x => x.Focused = false);
            if (IsSmallUp)
            {
                SetFocusingCom();
            }
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
                ComType = header.ComponentType == nameof(Datepicker) || header.ComponentType == nameof(Number) ? header.ComponentType : nameof(Textbox),
                Precision = header.Precision,
                PElement = MainSection.Element
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
                else if (header.ComponentType == nameof(Number))
                {
                    valueText = confirmDialog.Number.GetValueText();
                    value = confirmDialog.Number.Value.ToString();
                }
                else
                {
                    valueText = confirmDialog.Textbox.Text.Trim().EncodeSpecialChar();
                    value = confirmDialog.Textbox.Text.Trim().EncodeSpecialChar();
                }
                Window.LocalStorage.SetItem("LastSearch" + GuiInfo.Id + header.Id, value);
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
                        Shift = (bool)ev["Shift"],
                        ValueText = valueText,
                        Operator = ev["Operator"].ToString(),
                        OperatorText = ev["OperatorText"].ToString(),
                    });
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
                else if (header.ComponentType == nameof(Number))
                {
                    confirmDialog.Number.Value = Convert.ToDecimal(subFilter);
                    var input = confirmDialog.Number.Element as HTMLInputElement;
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
