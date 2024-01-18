using Bridge.Html5;
using Core.Models;
using Core.Components.Extensions;
using Core.Extensions;
using Core.MVVM;
using System;

namespace Core.Components
{
    public class Button : EditableComponent
    {
        public HTMLElement ButtonEle { get; set; }
        private HTMLSpanElement _textEle;

        public Button(Component ui, HTMLElement ele = null) : base(ui)
        {
            Meta = ui ?? throw new ArgumentNullException(nameof(ui));
            ButtonEle = ele;
        }

        public override void Render()
        {
            var html = Html.Instance;
            if (ButtonEle is null)
            {
                Html.Take(ParentElement).Button.ClassName("btn" + Meta.Id).Render();
                Element = ButtonEle = Html.Context;
            }
            else
            {
                Element = ButtonEle;
            }
            Html.Take(Element).ClassName(Meta.ClassName)
                .Event(EventType.Click, DispatchClick).Style(Meta.Style);
            if (!string.IsNullOrEmpty(Meta.Icon))
            {
                html.Icon(Meta.Icon).End.Text(" ").Render();
            }
            html.Span.ClassName("caption").IText(Meta.Label ?? string.Empty);
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
                this.DispatchEvent(Meta.Events, EventType.Click, Entity, this).Done();
            }
            finally
            {
                Spinner.Hide();
                Disabled = false;
            }
        }

        public override string GetValueText()
        {
            if (Entity is null || Meta is null)
            {
                return _textEle.TextContent;
            }
            return Entity[FieldName]?.ToString();
        }
    }
}
