using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class VirtualGrid : GridView
    {
        public List<object> CacheData { get; set; } = new List<object>();
        public int LastStartIndexCache { get; set; }
        public int LastEndIndexCache { get; set; }
        public VirtualGrid(Component ui) : base(ui)
        {
        }

        protected override void DOMContentLoadedHandler()
        {
            base.DOMContentLoadedHandler();
            Task.Run(LoadCache);
        }

        private async Task LoadCache()
        {
            if (AllListViewItem.Nothing())
            {
                return;
            }
            var bottomTask = LoadBottomCache(AllListViewItem.LastOrDefault().Index);
            var topTask = LoadTopCache(AllListViewItem.FirstOrDefault().Index);
            await Task.WhenAll(bottomTask, topTask);
            if (bottomTask.Result.Nothing() && topTask.Result.Nothing())
            {
                return;
            }
            CacheData.Clear();
            topTask.Result.Union(bottomTask.Result).Union(RowData.Data).ForEach(CacheData.Add);
            await LoadMasterData(CacheData, spinner: false);
        }

        private async Task<IEnumerable<object>> LoadTopCache(int startIndex)
        {
            if (startIndex < viewPortCount * 10 || startIndex >= LastStartIndexCache)
            {
                return Enumerable.Empty<object>();
            }
            var source = CalcDatasourse(viewPortCount * 10, startIndex - viewPortCount * 10, "false");
            var data = await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetList<object>(source);
            data.Value.Reverse();
            LastStartIndexCache = startIndex - viewPortCount * 10;
            return data.Value;
        }

        private async Task<IEnumerable<object>> LoadBottomCache(int endIndex)
        {
            if (endIndex <= LastEndIndexCache)
            {
                return Enumerable.Empty<object>();
            }
            var source = CalcDatasourse(viewPortCount * 10, endIndex, "false");
            var data = await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetList<object>(source);
            LastEndIndexCache = endIndex + viewPortCount * 10;
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
            var rows = new List<object>();
            if (firstLoad)
            {
                rows = await FirstLoad(count, skip);
            }
            else
            {
                rows = CacheData.Skip(skip).Take(viewPortCount).ToList();
                if (rows.Count < viewPortCount)
                {
                    rows = await FirstLoad(count, skip);
                    await LoadCache();
                }
                FormattedRowData = rows;
            }
            UpdateExistRows(false);
            RowData.Data.Clear();
            rows.ForEach(RowData.Data.Add);
            Entity?.SetComplexPropValue(GuiInfo.FieldName, rows);
            SetRowHeight();
            RenderVirtualRow(MainSection.Element as HTMLTableSectionElement, skip, viewPortCount);
            RowAction(x => x.Focused = false);
            SetFocusingCom();
            _renderingViewPort = false;
        }

        private async Task<List<object>> FirstLoad(bool count, int skip)
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
            await LoadMasterData(FormattedRowData, spinner: false);
            return rows;
        }
    }
}
