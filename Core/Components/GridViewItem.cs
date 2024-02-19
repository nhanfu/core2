using Bridge.Html5;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System.Collections.Generic;
using System.Linq;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class GridViewItem : ListViewItem
    {
        public GridViewItem(ElementType tr) : base(tr)
        {
        }

        internal override void RenderRowData(List<Component> headers, object row, int? index = null, bool emptyRow = false)
        {
            if (index.HasValue)
            {
                if (index >= Element.ParentElement.Children.Count() || index < 0)
                {
                    index = 0;
                }

                Element.ParentElement.InsertBefore(Element, Element.ParentElement.Children[index.Value]);
            }
            headers.Where(x => !x.Hidden).ForEach(header =>
            {
                RenderTableCell(row, header, null);
            });
            BindingEvents();
        }

        internal override void RenderTableCell(object rowData, Component header, HTMLElement cellWrapper = null)
        {
            Html.Take(Element).TData.TabIndex(-1)
                .Event(EventType.FocusIn, (e) => FocusCell(e, header))
                .DataAttr("field", header.FieldName).Render();
            if (header.StatusBar)
            {
                Html.Instance.Icon("fa fa-pencil").End.Render();
            }
            if (string.IsNullOrEmpty(header.FieldName))
            {
                return;
            }
            base.RenderTableCell(rowData, header, Html.Context);
            Html.Instance.EndOf(ElementType.td);
        }

        private void FocusCell(Event e, Component header)
        {
            ListViewSection.ListView.LastElementFocus?.Closest(ElementType.td.ToString()).RemoveClass("cell-selected");
            var td = e.Target as HTMLElement;
            td.Closest(ElementType.td.ToString()).AddClass("cell-selected");
            ListViewSection.ListView.LastElementFocus = td;
            ListViewSection.ListView.LastComponentFocus = header;
            ListViewSection.ListView.EntityFocusId = Entity[IdField]?.ToString();
        }
    }
}