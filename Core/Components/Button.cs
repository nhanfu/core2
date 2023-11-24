using Bridge.Html5;
using Core.Models;
using Core.Components.Extensions;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Clients;

namespace Core.Components
{
    public class Button : EditableComponent
    {
        public HTMLElement ButtonEle { get; set; }
        private HTMLSpanElement _textEle;
        public string Label
        {
            get => _textEle?.TextContent;
            set
            {
                if (Element is null)
                {
                    throw new InvalidOperationException("Element is null");
                }

                _textEle.TextContent = value;
            }
        }

        public Button(Component ui, HTMLElement ele = null) : base(ui)
        {
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
            ButtonEle = ele;
        }

        public override void Render()
        {
            var html = Html.Instance;
            if (ButtonEle is null)
            {
                Html.Take(ParentElement).Button.ClassName("btn" + GuiInfo.Id).Render();
                Element = ButtonEle = Html.Context;
            }
            else
            {
                Element = ButtonEle;
            }
            Html.Take(Element).ClassName(GuiInfo.ClassName)
                .Event(EventType.Click, DispatchClick).Style(GuiInfo.Style);
            if (!string.IsNullOrEmpty(GuiInfo.Icon))
            {
                html.Icon(GuiInfo.Icon).End.Text(" ").Render();
            }
            html.Span.ClassName("caption").IText(GuiInfo.Label ?? string.Empty);
            _textEle = Html.Context as HTMLSpanElement;
            Element.Closest("td")?.AddEventListener(EventType.KeyDown, ListViewItemTab);
            DOMContentLoaded?.Invoke();
        }

        public virtual void DispatchClick()
        {
            if (Disabled || Element.Hidden())
            {
                return;
            }
            Disabled = true;
            try
            {
                Spinner.AppendTo(Element);
                Client.ExecTaskNoResult(this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Click, Entity, this));
            }
            finally
            {
                Spinner.Hide();
                Disabled = false;
            }
        }

        public override string GetValueText()
        {
            if (Entity is null || GuiInfo is null)
            {
                return _textEle.TextContent;
            }
            return Entity[FieldName]?.ToString();
        }

        public override StringBuilder BuildTextHistory(StringBuilder builder = null, HashSet<object> visited = null)
        {
            return builder;
        }
    }
}
