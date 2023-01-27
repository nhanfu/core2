using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
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
        internal int viewPortCount;

        public List<object> CacheData { get; set; } = new List<object>();
        public int LastStartIndexCache { get; set; }
        public int LastEndIndexCache { get; set; }
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
        protected override void DOMContentLoadedHandler()
        {
            base.DOMContentLoadedHandler();
            Task.Run(() => PrepareCache(0));
        }

        private async Task PrepareCache(int skip = 0)
        {
            if (AllListViewItem.Nothing())
            {
                return;
            }
            var bottomTask = LoadBottomCache(AllListViewItem.LastOrDefault().RowNo + 1);
            var topTask = LoadTopCache(AllListViewItem.FirstOrDefault().RowNo - 1);
            await Task.WhenAll(bottomTask, topTask);
            if (bottomTask.Result.Nothing() && topTask.Result.Nothing())
            {
                return;
            }
            CacheData.Clear();
            CacheData.AddRange(topTask.Result.Concat(FormattedRowData).Concat(bottomTask.Result));
            CacheData.ForEach((x, index) => x[RowNo] = skip + index + 1);
            await LoadMasterData(CacheData, spinner: false);
        }

        private async Task<IEnumerable<object>> LoadTopCache(int startNum)
        {
            if (startNum <= 0 || startNum < viewPortCount * 15 || startNum >= LastStartIndexCache)
            {
                return Enumerable.Empty<object>();
            }
            var source = CalcDatasourse(viewPortCount * 15, startNum - viewPortCount * 15, "false");
            var data = await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetList<object>(source);
            data.Value.Reverse();
            LastStartIndexCache = startNum - viewPortCount * 15;
            return data.Value;
        }

        private async Task<IEnumerable<object>> LoadBottomCache(int endIndex)
        {
            if (endIndex < LastEndIndexCache)
            {
                return Enumerable.Empty<object>();
            }
            var source = CalcDatasourse(viewPortCount * 15, endIndex, "false");
            var data = await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetList<object>(source);
            LastEndIndexCache = endIndex + viewPortCount * 15;
            return data.Value;
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
                if (rows.Count < viewPortCount)
                {
                    rows = await FirstLoadData(count, skip);
                    await PrepareCache(skip);
                }
                FormattedRowData = rows;
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
            var index = CacheData.IndexOf(x => (int)x[RowNo] == skip + 1);
            if (index < 0)
            {
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
            List<object> rows;
            var source = CalcDatasourse(viewPortCount, skip, count ? "true" : "false");
            var oDataRows = await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetList<object>(source);
            Sql = oDataRows.Sql;
            rows = oDataRows.Value;
            if (Paginator != null && count)
            {
                Paginator.Options.Total = oDataRows.Odata.Count ?? rows.Count;
            }
            FormattedRowData = rows;
            rows.ForEach((x, index) => x[RowNo] = skip + index + 1);
            await LoadMasterData(FormattedRowData, spinner: false);
            return rows;
        }

        public override async Task<List<object>> ReloadData(string dataSource = null, bool cache = false, int? skip = null, int? pageSize = null)
        {
            DisposeNoRecord();
            VirtualScroll = GuiInfo.GroupBy.Nothing() && GuiInfo.VirtualScroll && Element.Style.Display.ToString() != Display.None.ToString();
            _lastScrollTop = -1;
            await RenderViewPort(firstLoad: true);
            return FormattedRowData;
        }

        private void RenderViewPortWrapper(Event e)
        {
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
            topVirtualRow.Style.Height = skip * _rowHeight + Utils.Pixel;
            topVirtualRow.SetAttribute(VirtualRow, Direction.top.ToString());
            tbody.InsertBefore(topVirtualRow, tbody.FirstChild);

            var existBottomEle = tbody.Children.LastOrDefault(x => x.GetAttribute(VirtualRow) == Direction.bottom.ToString());
            var bottomVirtualRow = existBottomEle ?? Document.CreateElement(ElementType.tr.ToString());
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
    }
}
