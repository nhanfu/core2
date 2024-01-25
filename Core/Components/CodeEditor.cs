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

        private bool _hasLoadScript;
        private Task<bool> LoadScript()
        {
            if (_hasLoadScript) return Task.FromResult(true);
            _hasLoadScript = true;
            var tcs = new TaskCompletionSource<bool>();
            Client.LoadScript("https://unpkg.com/monaco-editor@latest/min/vs/loader.js")
                .Done(() => tcs.TrySetResult(true))
                .Catch(err => tcs.TrySetException(err));
            return tcs.Task;
        }

        public override void Render()
        {
            if (Element is null)
            {
                Html.Take(ParentElement).Div.Id(Uuid7.Id25());
                Element = Html.Context;
            }
            LoadScript().Done(() =>
            {
                var self = this;
                /*@
                self.editor = initCodeEditor(self);
                 */
            });
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            var handler = _events.GetValueOrDefault(nameof(UpdateView));
            handler?.Invoke(new ObservableArgs { Com = this, EvType = EventType.Change });
        }
    }
}
