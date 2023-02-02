using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components.Extensions;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class TreeView : ListView
    {
        public TreeView(Component ui) : base(ui)
        {

        }

        protected override void Rerender()
        {
            DisposeNoRecord();
            Editable = GuiInfo.CanAdd && Header.Any(x => !x.Hidden && x.Editable);
            Header = Header.Where(x => !x.Hidden).ToList();
            MainSection.Element.AddClass("overflow");
            var firstData = FormattedRowData = FormattedRowData.Nothing() ? RowData.Data : FormattedRowData;
            RenderContent(Header, MainSection, true, firstData);
            MainSection.DisposeChildren();
            if (Editable)
            {
                AddNewEmptyRow();
            }
            else if (RowData.Data.Nothing())
            {
                NoRecordFound();
                DomLoaded();
                return;
            }
            if (MainSection.Element is HTMLTableSectionElement tableElement)
            {
                tableElement.AddEventListener(EventType.ContextMenu, BodyContextMenuHandler);
            }
            DomLoaded();
            Spinner.Hide();
        }

        protected void RenderContent(IEnumerable<GridPolicy> headers, EditableComponent node, bool first, List<object> rowDatas)
        {
            if (rowDatas.Nothing())
            {
                return;
            }
            Html.Take(node.Element).Ul.ClassName((!first ? "d-block " : " ") + (first ? " treeview" : " "));
            var ul = Html.Context;
            rowDatas.ForEach(async (row) =>
            {
                 await RenderRow(headers, node, row, ul);
            });
        }

        private async Task RenderRow(IEnumerable<GridPolicy> headers, EditableComponent node, object row, HTMLElement ul)
        {
            var data = await new Client(GuiInfo.RefName).GetList<object>($"?$filter=ParentId eq {row.GetPropValue(IdField)}");
            var datas = data.Value;
            var count = datas.Count;
            Html.Take(ul);
            var rowSection = new ListViewItem(MVVM.ElementType.li)
            {
                Entity = row,
                ListViewSection = MainSection
            };
            node.AddChild(rowSection);
            rowSection.Element.AddClass("tree-node");
            Html.Instance.Div.ClassName("d-inline-flex-fs").Table.ClassName("table").TRow.Render();
            var tr = Html.Context;
            var width = 95 / headers.Count();
            headers.ForEach(header =>
            {
                var com = header.MapToComponent();
                Html.Take(tr).TData.Width($"{width}%").Render();
                rowSection.RenderTableCell(row, com);
                Html.Take(tr).EndOf(MVVM.ElementType.td);
            });
            if (count > 0)
            {
                Html.Take(rowSection.Element).Span.ClassName("node-toggle").Event(EventType.Click, () => FocusIn(rowSection, row, datas));
            }
        }

        private void FocusIn(ListViewItem listViewItem, object row, List<object> datas)
        {
            var ul = listViewItem.Element.QuerySelector("ul");
            if (listViewItem.Element.HasClass("expanded"))
            {
                ul.RemoveClass("d-block");
                ul.AddClass("d-none");
                listViewItem.Element.RemoveClass("expanded");
            }
            else
            {
                listViewItem.Element.AddClass("expanded");
                if (ul is null)
                {
                    RenderContent(Header, listViewItem, false, datas);
                }
                else
                {
                    ul.RemoveClass("d-none");
                    ul.AddClass("d-block");
                    listViewItem.Element.AddClass("expanded");
                }
            }
        }

        public override string CalcFilterQuery(bool searching)
        {
            var res = base.CalcFilterQuery(searching);
            var resetSearch = ListViewSearch.EntityVM.SearchTerm.IsNullOrWhiteSpace() && AdvSearchVM.Conditions.Nothing();
            if (searching && !resetSearch)
            {
                var filterPart = OdataExt.GetClausePart(res, OdataExt.FilterKeyword);
                filterPart = filterPart.Replace(new RegExp(@"((and|or) )?Parent(\w|\W)* eq null( (and|or)$)?"), string.Empty);
                filterPart = filterPart.Replace(new RegExp(@"^Parent(\w|\W)* eq null( (and|or))?"), string.Empty);
                return OdataExt.ApplyClause(res, filterPart);
            }
            return res;
        }
    }
}
