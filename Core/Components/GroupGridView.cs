using Bridge.Html5;
using Core.Components.Extensions;
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
    public class GroupRowData
    {
        public object Key { get; set; }
        public List<object> Children { get; set; }
    }

    public class GroupGridView : GridView
    {
        private const string _groupKey = "__groupkey__";
        public const string GroupRowClass = "group-row";
        public GroupGridView(Component ui) : base(ui)
        {
        }

        public override void Render()
        {
            base.Render();
            Html.Take(Element).ClassName("group-table").End.Render();
        }

        protected override void RowDataChanged(ObservableListArgs<object> args)
        {
            if (args.Action == ObservableAction.Remove)
            {
                if (args.Item is null && args.Item[IdField].As<int?>() != null)
                {
                    return;
                }

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

                if (args.Action == ObservableAction.Render)
                {
                    NoRowData(args.ListData);
                }
                switch (args.Action)
                {
                    case ObservableAction.Add:
                        await AddRow(args.Item);
                        return;
                    case ObservableAction.AddRange:
                        await AddRows(args.ListData, args.Index);
                        return;
                    case ObservableAction.Update:
                        await AddOrUpdateRow(args.Item);
                        return;
                }
                var keys = GuiInfo.GroupBy.Split(",");
                FormattedRowData = args.ListData
                    .Select((Func<object, object>)(x =>
                    {
                        x[_groupKey] = string.Join(" ", keys.Select((Func<string, string>)(key => (string)(x.GetPropValue(key)?.ToString()))));
                        return x;
                    }))
                    .GroupBy(x => x[_groupKey])
                    .Select(x => new GroupRowData
                    {
                        Key = x.Key,
                        Children = x.ToList()
                    })
                    .Cast<object>().ToList();
                base.Rerender();
                DomLoaded();
            }));
        }

        public override void RenderContent()
        {
            AddSections();
            FormattedRowData = FormattedRowData.Nothing() ? RowData.Data : FormattedRowData;
            if (FormattedRowData.Nothing())
            {
                return;
            }
            MainSection.Show = false;
            MainSection.DisposeChildren();
            FormattedRowData.SelectForEach((row, index) =>
            {
                Html.Take(MainSection.Element);
                RenderRowData(Header, row, MainSection, null);
            });
            MainSection.Show = true;
        }

        public override Task ApplyFilter(bool searching = true)
        {
            MainSection.DisposeChildren();
            return base.ApplyFilter(searching);
        }

        private void NoRowData(List<object> list)
        {
            Editable = GuiInfo.CanAdd && Header.Any(x => !x.Hidden && x.Editable);
            if (Editable)
            {
                AddNewEmptyRow();
            }
            else if (list.Nothing())
            {
                NoRecordFound();
            }
        }

        public override async Task<ListViewItem> AddRow(object item, int fromIndex = 0, bool singleAdd = true)
        {
            DisposeNoRecord();
            var keys = GuiInfo.GroupBy.Split(",");
            item[_groupKey] = string.Join(" ", keys.Select(key => item.GetPropValue(key)?.ToString()));
            var groupKey = item[_groupKey];
            var existGroup = AllListViewItem
                .FirstOrDefault(group => group.GroupRow && group.Entity.As<GroupRowData>().Key == groupKey);
            ListViewItem row;
            if (existGroup is null)
            {
                var groupData = new GroupRowData { Key = groupKey, Children = new List<object> { item } };
                FormattedRowData.Add(groupData);
                row = RenderRowData(Header, groupData, MainSection, 0);
            }
            else
            {
                existGroup.Entity.As<GroupRowData>().Children.Add(item);
                var index = MainSection.Children.IndexOf(existGroup);
                row = RenderRowData(Header, item, MainSection, index + existGroup.Children.Count + 1);
            }
            Dirty = true;
            return row;
        }

        public override ListViewItem RenderRowData(List<Component> headers, object row, Section section, int? index, bool emptyRow = false)
        {
            if (!(row is GroupRowData groupRow))
            {
                return base.RenderRowData(headers, row, section, index, emptyRow);
            }
            if (!(section.Element is HTMLTableSectionElement tbody))
            {
                throw new InvalidOperationException("The section is not HTML table element");
            }
            Html.Take(tbody);
            if (groupRow.Key == null || groupRow.Key.ToString().IsNullOrWhiteSpace())
            {
                ListViewItem rowResult = null;
                groupRow.Children.ForEach(child =>
                {
                    Html.Take(tbody);
                    rowResult = base.RenderRowData(headers, child, section, null);
                });
                return rowResult;
            }
            var first = groupRow.Children.FirstOrDefault();
            var groupSection = new GroupViewItem(ElementType.tr)
            {
                Entity = row,
                ParentElement = tbody,
                GroupRow = true,
                ListViewSection = section as ListViewSection,
                ListView = this
            };
            section.AddChild(groupSection);
            groupSection.Element.TabIndex = -1;
            string groupText;
            if (Utils.IsFunction(GuiInfo.GroupFormat, out var fn))
            {
                groupText = fn.Call(this, this, first).ToString();
            }
            else
            {
                groupText = Utils.FormatEntity(GuiInfo.GroupFormat, null, first, Utils.EmptyFormat, Utils.EmptyFormat);
            }
            if (GuiInfo.GroupReferenceId != null)
            {
                var val = first.GetPropValue(GuiInfo.GroupBy.Substr(0, GuiInfo.GroupBy.Length - 2));
                groupSection.Entity = val;
                groupSection.Entity.SetPropValue("ModelName", GuiInfo.RefName);
                headers.Where(x => !x.Hidden).ForEach(header =>
                {
                    Html.Instance.TData.TabIndex(-1)
                   .Style(header.Style)
                   .Event(EventType.FocusIn, (e) => FocusCell(e, header))
                   .DataAttr("field", header.FieldName).Render();
                    var td = Html.Context;
                    groupSection.RenderTableCell(val, header, td);
                    Html.Instance.EndOf(ElementType.td);
                });
            }
            else
            {
                Html.Instance.TData.ClassName("status-cell").Icon("mif-pencil").EndOf(ElementType.td)
                .TData.ColSpan(headers.Count - 1)
                    .AsyncEvent(EventType.Click, DispatchClick, first)
                    .AsyncEvent(EventType.DblClick, DispatchDblClick, first)
                    .Icon("fa fa-chevron-down").Event(EventType.Click, () => groupSection.ShowChildren = !groupSection.ShowChildren).End
                    .Div.ClassName("d-flex").InnerHTML(groupText);
                groupSection.GroupText = Html.Context;
                groupSection.Chevron = Html.Context.PreviousElementSibling;
                groupSection.Chevron.ParentElement.PreviousElementSibling.AppendChild(groupSection.Chevron);
                Html.Instance.EndOf(ElementType.td);
            }
            Html.Instance.EndOf(ElementType.tr);
            groupRow.Children.ForEach(child =>
            {
                Html.Take(tbody);
                var rowSection = base.RenderRowData(headers, child, section);
                rowSection.Element.AddClass("group-detail");
                groupSection.ChildrenItems.Add(rowSection);
                rowSection.GroupSection = groupSection;
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

        public override void ToggleAll()
        {
            var allSelected = AllListViewItem
                .Where(x => !x.GroupRow && !x.EmptyRow)
                .All(x => x.Selected);
            if (allSelected)
            {
                ClearSelected();
            }
            else
            {
                RowAction(x => x.Selected = !x.GroupRow && !x.EmptyRow);
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
            NoRowData(RowData.Data);
        }

        public override void RemoveRange(IEnumerable<object> data)
        {
            data.SelectForeach(x => RemoveRowById(x[IdField].ToString()));
        }

        public override async Task<List<ListViewItem>> AddRows(IEnumerable<object> rowsData, int index = 0)
        {
            var listItem = new List<ListViewItem>();
            await rowsData.ForEachAsync(async x =>
            {
                listItem.Add(await AddRow(x, 0, false));
            });
            return listItem;
        }

        public override async Task<List<ListViewItem>> AddRowsNo(IEnumerable<object> rows, int index = 0)
        {
            var listItem = new List<ListViewItem>();
            await rows.ForEachAsync(async x =>
            {
                listItem.Add(await AddRow(x, 0, false));
            });
            return listItem;
        }

        public override async Task AddOrUpdateRow(object rowData, bool singleAdd = true, bool force = false, params string[] fields)
        {
            var existRowData = this
               .FilterChildren(x => x is ListViewItem && x.Entity[IdField] == rowData[IdField])
               .Cast<ListViewItem>().FirstOrDefault();
            if (existRowData is null)
            {
                await AddRow(rowData, 0, singleAdd);
                return;
            }
            if (existRowData.EmptyRow)
            {
                existRowData.Entity = null;
                await AddRow(rowData, 0, singleAdd);
            }
            else
            {
                existRowData.Entity.CopyPropFrom(rowData);
                RowAction(x => x.Entity == existRowData.Entity, x => x.UpdateView(force: force, componentNames: fields));
            }
        }

        public override async Task AddOrUpdateRows(IEnumerable<object> rows)
        {
            await rows.ForEachAsync(async row => await AddOrUpdateRow(row, false));
        }

        protected override void RenderIndex(int? skip = null)
        {
            Window.ClearTimeout(_renderIndexAwaiter);
            _renderIndexAwaiter = Window.SetTimeout(() =>
            {
                var index = 0;
                var indexRow = 0;
                var isChild = false;
                MainSection.Children.Cast<ListViewItem>().ForEach((row) =>
                {
                    if (row.GroupRow)
                    {
                        index = 0;
                        isChild = true;
                        return;
                    }
                    if (!row.Element.HasClass("group-detail") && isChild)
                    {
                        index = 0;
                        isChild = false;
                    }
                    var previous = row.FirstChild.Element.Closest("td").PreviousElementSibling;
                    if (previous is null)
                    {
                        return;
                    }
                    if (row.EmptyRow)
                    {
                        index = 0;
                        previous.InnerHTML = @"<i class='fal fa-plus'></i>";
                    }
                    else
                    {
                        previous.InnerHTML = (index + 1).ToString();
                        row.RowNo = indexRow;
                        indexRow++;
                        index++;
                    }
                });
            });
        }

        protected override void SetRowData(List<object> listData)
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
    }
}
