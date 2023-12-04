using Bridge.Html5;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Linq;

namespace Core.Components
{
    public class Link : EditableComponent
    {
        public Link(Component ui, HTMLElement ele = null) : base(ui)
        {
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
            Element = ele;
        }

        public override void Render()
        {
            if (Element == null)
            {
                Element = Html.Take(ParentElement).A.IText(GuiInfo.PlainText ?? GuiInfo.Label).GetContext();
            }
            Element.AddEventListener(EventType.Click, (e) => DispatchClick(e));
            DOMContentLoaded?.Invoke();
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            // not to do anything here
        }

        private void DispatchClick(Event e)
        {
            e.PreventDefault();
            if (GuiInfo.Events.HasAnyChar() && GuiInfo.Events.ToLower().Contains("click"))
            {
                this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Click, Entity).Done();
                return;
            }
            var a = Element as HTMLAnchorElement;
            var f = a.Href.Split(Utils.Slash).Where(x => x.HasNonSpaceChar())
                .Where(x => !x.Contains(Utils.QuestionMark) && !x.Contains(Utils.Hash)).LastOrDefault();
            Spinner.AppendTo(Document.Body, timeout: 1000);
            ComponentExt.InitFeatureByName(f ?? string.Empty).Done(x =>
            {
                Window.History.PushState(null, a.Title, a.Href);
            });
        }
    }
}
