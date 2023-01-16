using Bridge.Html5;
using Core.Models;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Components
{
    public class PaginationOptions
    {
        public int Total { get; set; }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
        public int PageNumber { get; set; }
        public int CurrentPageCount { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public Action<int, object> ClickHandler { get; set; }
    }

    public class Paginator : EditableComponent
    {
        public PaginationOptions Options => Entity as PaginationOptions;

        public Paginator(PaginationOptions paginator) : base(null)
        {
            Entity = paginator ?? throw new ArgumentNullException(nameof(paginator));
            PopulateDirty = false;
            AlwaysValid = true;
            StopChildrenHistory = true;
        }

        public override void Render()
        {
            Html.Take(Parent.Element).Div.ClassName("grid-toolbar paging").Label.IText("Phân trang").End.Render();
            Element = Html.Context;
            var pageSize = new Number(new Component { FieldName = nameof(PaginationOptions.PageSize) }, null)
            {
                SetSeclection = false
            };
            var startIndex = new CellText(new Component { FieldName = nameof(PaginationOptions.StartIndex) });
            var endIndex = new CellText(new Component { FieldName = nameof(PaginationOptions.EndIndex) });
            var total = new CellText(new Component { FieldName = nameof(PaginationOptions.Total), FormatData = "{0:n0}" });
            var pageNum = new Number(new Component { FieldName = nameof(PaginationOptions.PageNumber) }, null)
            {
                AlwaysValid = true,
                SetSeclection = false
            };
            AddChild(pageSize);
            pageSize.Element.AddEventListener(EventType.Change.ToString(), ReloadListView);
            Html.Instance.End.Span.Render();
            AddChild(startIndex);
            Html.Instance.Text("-");
            AddChild(endIndex);
            Html.Instance.IText(" trong số ");
            AddChild(total);

            Html.Take(Element).Ul.ClassName("pagination").Li.Text("❮").Event(EventType.Click, PrevPage).End.Render();
            AddChild(pageNum);
            pageNum.Element.AddEventListener(EventType.Change.ToString(), () =>
            {
                Options.PageIndex = Options.PageNumber - 1;
                ReloadListView();
            });
            Html.Instance.End.Li.Text("❯").Event(EventType.Click, NextPage).End.Render();
        }

        private void NextPage()
        {
            var pages = decimal.Round((decimal)Options.Total / Options.PageSize, 0, MidpointRounding.Up);
            if (Options.PageNumber >= pages)
            {
                return;
            }

            Options.PageIndex++;
            Options.ClickHandler?.Invoke(Options.PageIndex, null);
            ReloadListView();
        }

        private void PrevPage()
        {
            if (Options.PageIndex <= 0)
            {
                return;
            }

            Options.PageIndex--;
            Options.ClickHandler?.Invoke(Options.PageIndex, null);
            ReloadListView();
        }

        private void ReloadListView()
        {
            Task.Run(() =>(Parent as ListView).ApplyFilter(true));
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            Show = Options.Total >= Options.PageSize;
            Children?.ForEach(child => child.UpdateView());
        }

        public override bool Disabled { get => false; set => _disabled = false; }

        public override StringBuilder BuildTextHistory(StringBuilder builder, HashSet<object> visited = null)
        {
            return builder;
        }
    }
}
