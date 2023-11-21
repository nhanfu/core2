using Bridge.Html5;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Threading.Tasks;

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
            Element.AddEventListener(EventType.Click, async (e) => await DispatchClick(e));
            DOMContentLoaded?.Invoke();
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            // not to do anything here
        }

        private async Task DispatchClick(Event e)
        {
            e.PreventDefault();
            if (GuiInfo.Events.HasAnyChar() && GuiInfo.Events.ToLower().Contains("click"))
            {
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Click, Entity);
                return;
            }
            var a = Element as HTMLAnchorElement;
            var f = Utils.GetUrlParam(Utils.FeatureField, a.Href);
            Spinner.AppendTo(Document.QuerySelector("header") as HTMLElement ?? Document.Body, timeout: 1000);
            await ComponentExt.InitFeatureByName(f ?? "Home");
            Window.History.PushState(null, a.Title, a.Href);
        }
    }
}
