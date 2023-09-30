using Bridge.Html5;
using Core.Models;
using Core.Components.Extensions;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class GroupListView : ListView
    {
        private const string _groupKey = "__groupkey__";
        public const string GroupRowClass = "group-row";
        public GroupListView(Component ui) : base(ui)
        {
        }

        public override void Render()
        {
            base.Render();
            Html.Take(Element).ClassName("group-listview").End.Render();
        }

        protected override void RowDataChanged(ObservableListArgs<object> args)
        {
            if (args.Action == ObservableAction.Remove)
            {
                RemoveRowById(args.Item[IdField].ToString());
                return;
            }
            Window.ClearTimeout(_rowDataChangeAwaiter);
            _rowDataChangeAwaiter = Window.SetTimeout((Action)(async () =>
            {
                if (GuiInfo.GroupBy.IsNullOrEmpty())
                {
                    return;
                }

                if (args.ListData.Nothing())
                {
                    NoRecordFound();
                    return;
                }
                switch (args.Action)
                {
                    case ObservableAction.Add:
                        await AddRow(args.Item);
                        return;
                    case ObservableAction.AddRange:
                        await AddRows(args.ListData);
                        return;
                    case ObservableAction.Update:
                        await AddOrUpdateRow(args.Item);
                        return;
                }
                FormattedRowData = ComponentExt.BuildGroupTree(args.ListData, GuiInfo.GroupBy.Split(",")).ToList();
                base.Rerender();
            }));
        }

        public override async Task<ListViewItem> AddRow(object item, int fromIndex = 0, bool singleAdd = true)
        {
            DisposeNoRecord();
            var keys = GuiInfo.GroupBy.Split(",");
            if (singleAdd)
            {
                await LoadMasterData(new List<object> { item });
            }
            item[_groupKey] = string.Join(" ", keys.Select(key => item.GetComplexPropValue(key)?.ToString()));
            var groupKey = item[_groupKey];
            var existGroup = AllListViewItem
                .FirstOrDefault(group => group.GroupRow && group.Entity.As<GroupRowData>().Key == groupKey);
            ListViewItem rowSection;
            if (existGroup is null)
            {
                var groupData = new GroupRowData { Key = groupKey, Children = new List<object> { item } };
                FormattedRowData.Add(groupData);
                rowSection = RenderRowData(Header, groupData, MainSection, MainSection.Children.Count);
            }
            else
            {
                existGroup.Entity.As<GroupRowData>().Children.Add(item);
                var index = MainSection.Children.IndexOf(existGroup);
                rowSection = RenderRowData(Header, item, MainSection, index + existGroup.Children.Count);
            }
            if (singleAdd)
            {
                FinalAddOrUpdate();
            }
            Dirty = true;
            return rowSection;
        }

        public override async Task<List<ListViewItem>> AddRows(IEnumerable<object> rowsData, int index = 0)
        {
            if (!GuiInfo.IsRealtime)
            {
                await LoadMasterData(rowsData);
            }
            var listItem = new List<ListViewItem>();
            await rowsData.ForEachAsync(async x =>
            {
                listItem.Add(await AddRow(x, 0, false));
            });
            return listItem;
        }

        public override async Task<List<ListViewItem>> AddRowsNo(IEnumerable<object> rowsData, int index = 0)
        {
            if (!GuiInfo.IsRealtime)
            {
                await LoadMasterData(rowsData);
            }
            var listItem = new List<ListViewItem>();
            await rowsData.ForEachAsync(async x =>
            {
                listItem.Add(await AddRow(x, 0, false));
            });
            return listItem;
        }

        public override ListViewItem RenderRowData(List<Component> headers, object row, Section listViewSection, int? index, bool emptyRow = false)
        {
            if (!(row is GroupRowData groupRow))
            {
                return base.RenderRowData(headers, row, listViewSection, index, emptyRow);
            }
            var wrapper = listViewSection.Element;
            if (groupRow.Key == null || groupRow.Key.ToString().IsNullOrWhiteSpace())
            {
                ListViewItem rowResult = null;
                groupRow.Children.ForEach(child =>
                {
                    Html.Take(wrapper);
                    rowResult = RenderRowData(headers, child, listViewSection, null);
                });
                return rowResult;
            }
            var groupSection = new GroupViewItem(ElementType.div)
            {
                Entity = row,
                ParentElement = wrapper,
                GroupRow = true,
                PreQueryFn = _preQueryFn,
                ListView = this,
                GuiInfo = GuiInfo
            };
            listViewSection.AddChild(groupSection);
            var first = groupRow.Children.FirstOrDefault();
            var groupText = Utils.FormatEntity(GuiInfo.GroupFormat, null, first, x => "N/A", x => "N/A");
            Html.Take(groupSection.Element).AsyncEvent(EventType.Click, DispatchClick, first)
                    .AsyncEvent(EventType.DblClick, DispatchDblClick, first)
                    .Icon("fa fa-chevron-right").Event(EventType.Click, ToggleGroupRow, groupSection).End
                    .Span.InnerHTML(groupText);
            groupSection.GroupText = Html.Context;
            groupRow.Children.ForEach(child =>
            {
                Html.Take(groupSection.Element);
                var childRow = RenderRowData(headers, child, groupSection, null);
                childRow.GroupSection = groupSection;
                Html.Take(childRow.Element).SmallCheckbox().Render();
                var chk = Html.Context.PreviousElementSibling as HTMLInputElement;
                Html.Instance.End.End.Event(EventType.Click, (e) =>
                {
                    e.PreventDefault();
                    childRow.Selected = !childRow.Selected;
                    chk.Checked = childRow.Selected;
                });
            });
            return groupSection;
        }

        private Task DispatchClick(object row)
        {
            return this.DispatchEventToHandlerAsync(GuiInfo.GroupEvent, EventType.Click, row);
        }

        private Task DispatchDblClick(object row)
        {
            return this.DispatchEventToHandlerAsync(GuiInfo.GroupEvent, EventType.DblClick, row);
        }

        private void ToggleGroupRow(ListViewItem groupSection, Event e)
        {
            if (!(e.Target is HTMLElement target))
            {
                return;
            }

            if (target.HasClass("fa-chevron-right"))
            {
                target.ReplaceClass("fa-chevron-right", "fa-chevron-down");
                groupSection.Children.ForEach(x => x.Show = false);
            }
            else
            {
                target.ReplaceClass("fa-chevron-down", "fa-chevron-right");
                groupSection.Children.ForEach(x => x.Show = true);
            }
        }

        public override void RemoveRowById(string id)
        {
            var index = RowData.Data.IndexOf(x => x[IdField].ToString() == id);
            if (index < 0)
            {
                return;
            }

            RowData.Data.RemoveAt(index);
            this.FilterChildren(x => x is ListViewItem && x.Entity[IdField].ToString() == id)
                .Cast<ListViewItem>().ToList().ForEach(x =>
                {
                    if (x.GroupSection != null && x.GroupSection.Entity is GroupRowData)
                    {
                        var groupChildren = x.GroupSection.Entity.As<GroupRowData>().Children;
                        groupChildren.Remove(x.Entity);
                        if (groupChildren.Nothing())
                        {
                            RowData.Data.Remove(x.GroupSection.Entity);
                            x.GroupSection.Dispose();
                        }
                    }
                    x.Dispose();
                });
            if (RowData.Data.Nothing())
            {
                NoRecordFound();
            }
        }

        public override void RemoveRange(IEnumerable<object> data)
        {
            data.ForEach(x => RemoveRowById(x[IdField].ToString()));
        }

        public override async Task AddOrUpdateRow(object rowData, bool singleAdd = true, bool force = false, params string[] fields)
        {
            var existRowData = this
                .FilterChildren(x => x is ListViewItem && x.Entity == rowData)
                .Cast<ListViewItem>().FirstOrDefault();
            if (existRowData is null)
            {
                await AddRow(rowData, 0, singleAdd);
                return;
            }
            if (singleAdd)
            {
                await LoadMasterData(new List<object> { rowData }, false);
            }
            if (existRowData.EmptyRow)
            {
                existRowData.Entity = null;
                await AddRow(rowData, 0, singleAdd);
            }
            else
            {
                existRowData.Entity.CopyPropFrom(rowData);
                RowAction(x => x.Entity == existRowData.Entity, x =>
                {
                    x.EmptyRow = false;
                    x.UpdateView(force: force, fields);
                    x.Dirty = true;
                });
            }
            if (singleAdd)
            {
                FinalAddOrUpdate();
            }
        }

        public override async Task AddOrUpdateRows(IEnumerable<object> rows)
        {
            await LoadMasterData(rows);
            await rows.ForEachAsync(async row => await AddOrUpdateRow(row, false));
            AddNewEmptyRow();
        }
    }
}
