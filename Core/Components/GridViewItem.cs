using Bridge.Html5;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class GridViewItem : ListViewItem
    {
        private GridView Grid { get; set; }
        public GridViewItem(ElementType tr) : base(tr)
        {
        }

        public override void Render()
        {
            base.Render();
            Grid = ListView as GridView;
        }

        internal override void RenderTableCell(object rowData, Component header, HTMLElement cellWrapper = null)
        {
            Html.Take(Element).TData.TabIndex(-1)
                .Event(EventType.FocusIn, (e) => FocusCell(e, header))
                .Event(EventType.KeyDown, (e) => Grid.HotKeyHandler(e, header, this))
                .DataAttr("field", header.FieldName).Render();
            if (header.StatusBar)
            {
                Html.Instance.Icon("fa fa-pencil").End.Render();
            }
            if (string.IsNullOrEmpty(header.FieldName))
            {
                return;
            }
            base.RenderTableCell(rowData, header, cellWrapper ?? Html.Context);
            Html.Instance.EndOf(ElementType.td);
        }

        private void FocusCell(Event e, Component header)
        {
            if (ListViewSection.ListView.LastElementFocus != null)
            {
                ListViewSection.ListView.LastElementFocus.Closest(ElementType.td.ToString()).RemoveClass("cell-selected");
            }
            var td = e.Target as HTMLElement;
            td.Closest(ElementType.td.ToString()).AddClass("cell-selected");
            ListViewSection.ListView.LastListViewItem = this;
            ListViewSection.ListView.LastElementFocus = td;
            ListViewSection.ListView.LastComponentFocus = header;
            ListViewSection.ListView.EntityFocusId = Entity[IdField].As<int>();
        }
    }
}