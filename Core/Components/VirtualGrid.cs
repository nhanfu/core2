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
        internal List<object> LastData;
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

        public override Task ApplyFilter()
        {
            CacheData.Clear();
            DataTable.ParentElement.ScrollTop = 0;
            return ReloadData(cacheHeader: true);
        }

        private Task<bool> PrepareCache(int skip = 0)
        {
            if (CacheData.HasElement())
            {
                var firstRowNo = (int)CacheData.First()[RowNo];
                var shouldEndNo = firstRowNo + viewPortCount * cacheAhead;
                if (skip > firstRowNo && skip < shouldEndNo)
                {
                    _waitingLoad = false;
                    return Task.FromResult(true);
                }
            }
            var tcs = new TaskCompletionSource<bool>();
            var start = skip - viewPortCount * cacheAhead;
            if (start < 0)
            {
                start = 0;
            }
            var sql = GetSql(start, viewPortCount + viewPortCount * cacheAhead * 2, cacheHeader: true, count: false);
            Client.ExecTask(Client.Instance.ComQuery(sql), ds =>
            {
                if (ds.Nothing())
                {
                    _waitingLoad = false;
                    return;
                }
                CacheData.Clear();
                CacheData.AddRange(ds[0]);
                CacheData.SelectForEach((x, index) => x[RowNo] = start + index + 1);
                _waitingLoad = false;
                tcs.TrySetResult(true);
            }, e => tcs.TrySetException(e));
            return tcs.Task;
        }

        internal override void RenderViewPort(bool count = true, bool firstLoad = false)
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
                LoadData(scrollTop, skip, count);
            }
            else
            {
                rows = ReadCache(skip, viewPortCount).ToList();
                if (rows.Count < viewPortCount && rows.Count < Paginator.Options.Total)
                {
                    LoadData(scrollTop, skip, count);
                }
                else
                {
                    RowDataLoaded(scrollTop, skip, rows);
                }
            }
        }

        private void LoadData(int scrollTop, int skip, bool count)
        {
            var task = FirstLoadData(count, skip);
            Client.ExecTask(task, rows =>
            {
                RowDataLoaded(scrollTop, skip, rows);
            });
        }

        private void RowDataLoaded(int scrollTop, int skip, List<object> rows)
        {
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
            Entity?.SetComplexPropValue(FieldName, rows);
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

        private Task<List<object>> FirstLoadData(bool count, int skip)
        {
            var tcs = new TaskCompletionSource<List<object>>();
            _skip = skip;
            var sql = GetSql(skip, viewPortCount, cacheHeader: false, count);
            Client.ExecTask(Client.Instance.ComQuery(sql), ds =>
            {
                var rows = ds.Length > 0 ? ds[0].ToList() : null;
                if (count) ProcessMetaData(ds, rows.Count);
                FormattedRowData = rows;
                rows.SelectForEach((x, index) => x[RowNo] = skip + index + 1);
                if (rows.Count < Paginator.Options.Total)
                {
                    Window.ClearTimeout(_renderPrepareCacheAwaiter);
                    _waitingLoad = true;
                    _renderPrepareCacheAwaiter = Window.SetTimeout(() =>
                        Client.ExecTaskNoResult(PrepareCache(skip + viewPortCount)), 7000);
                }
                tcs.TrySetResult(rows);
            });
            return tcs.Task;
        }

        public override Task<List<object>> ReloadData(bool cacheHeader = false, int? skip = null, int? pageSize = null)
        {
            DisposeNoRecord();
            VirtualScroll = GuiInfo.GroupBy.Nothing() && GuiInfo.VirtualScroll && Element.Style.Display.ToString() != Display.None.ToString();
            _lastScrollTop = -1;
            RenderViewPort(firstLoad: true);
            return Task.FromResult(FormattedRowData);
        }

        private void RenderViewPortWrapper(Event e)
        {
            if (_waitingLoad)
            {
                Window.ClearTimeout(_renderPrepareCacheAwaiter);
                _renderPrepareCacheAwaiter = Window.SetTimeout(() => Client.ExecTask(PrepareCache(_skip)), 7000);
            }
            if (_renderingViewPort || !VirtualScroll)
            {
                _renderingViewPort = false;
                e.PreventDefault();
                return;
            }
            Window.ClearTimeout(_renderViewPortAwaiter);
            _renderViewPortAwaiter = Window.SetTimeout(() => RenderViewPort(false), 100);
        }

        internal void RenderVirtualRow(HTMLTableSectionElement tbody, int skip, int viewPort)
        {
            var existTopEle = tbody.Children.FirstOrDefault(x => x.GetAttribute(VirtualRow) == Direction.top.ToString());
            var topVirtualRow = existTopEle ?? Document.CreateElement(ElementType.tr.ToString());
            if (!topVirtualRow.HasClass("virtual-row"))
            {
                topVirtualRow.AddClass("virtual-row");
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
            if (!bottomVirtualRow.HasClass("virtual-row"))
            {
                bottomVirtualRow.AddClass("virtual-row");
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

        public override void FocusCell(Event e, Component header)
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
    }
}
