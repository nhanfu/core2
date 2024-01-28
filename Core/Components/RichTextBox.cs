using Bridge.Html5;
using Core.Clients;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.Structs;
using System;
using System.Collections.Generic;

namespace Core.Components
{
    public class RichTextBox : EditableComponent
    {
        private readonly object editor;

        public RichTextBox(Component ui, HTMLElement ele = null) : base(ui)
        {
            if (Meta.Row <= 0)
            {
                Meta.Row = 1;
            }
            if (ele != null)
            {
                ParentElement = ele;
                BindingWebComponent();
            }
            else
            {
                ParentElement = ParentElement ?? Html.Context;
                BindingWebComponent();
            }
            ParentElement.AppendChild(Element);
        }

        private void BindingWebComponent()
        {
            Element = Html.Take(ParentElement).Div.Id(Uuid7.Id25()).GetContext();
        }

        public override void Render()
        {
            var self = this;
            /*@
            initCkEditor(self);
            */
        }

        public void ImageUploadHandler(object fileWrapper, Action<string> success)
        {
            var file = fileWrapper["blob"].As<Func<File>>().Invoke();
            var reader = new FileReader();
            reader.OnLoad += async (Event e) =>
            {
                var uploader = new Image(new Component());
                var path = await uploader.UploadBase64Image(e.Target["result"].ToString(), file.Name);
                if (success != null)
                {
                    success.Invoke(path);
                    string content = null;
                    /*@
                    content = this.editor.setContent(text);
                    */
                    Entity.SetComplexPropValue(FieldName, content);
                }
            };
            reader.ReadAsDataURL(file);
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            var handler = _events.GetValueOrDefault(nameof(UpdateView));
            handler?.Invoke(new ObservableArgs { Com = this, EvType = EventType.Change });
        }
    }
}
