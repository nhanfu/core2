using Bridge.Html5;
using Core.Clients;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.Structs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Components
{
    public class CodeEditor : EditableComponent
    {
        private readonly object editor;

        public CodeEditor(Component ui, HTMLElement ele = null) : base(ui)
        {
            Element = ele;
        }

        public override void Render()
        {
            if (Element is null)
            {
                Html.Take(ParentElement).Div.Id(Uuid7.Id25());
                Element = Html.Context;
            }
            /*@
            this.editor = initCodeEditor(this);
            */
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            var handler = _events.GetValueOrDefault(nameof(UpdateView));
            handler?.Invoke(new ObservableArgs { Com = this, EvType = EventType.Change });
        }
    }
}
