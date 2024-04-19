using Bridge.Html5;
using Core.Components.Forms;
using Core.Models;
using Core.MVVM;
using System;
using System.Linq;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class LayoutStudioCode : EditableComponent
    {
        public object editorHtml;
        public object editorCss;
        public object editorJavascript;
        public HTMLElement htmlElement;
        public HTMLElement cssElement;
        public HTMLElement jsElement;
        public HTMLIFrameElement previewElement;
        public HTMLStyleElement styleElement;

        public LayoutStudioCode(Component ui, HTMLElement ele = null) : base(ui)
        {
            Meta = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public override void Render()
        {
            if (htmlElement is null)
            {
                Html.Take(ParentElement).Div.ClassName("editor");
                Html.Instance.Style(Meta.Style).Div.ClassName("editor__code__html").Render();
                htmlElement = Html.Instance.GetContext();
                Html.Instance.End.Div.ClassName("editor__code__css").Render();
                cssElement = Html.Instance.GetContext();
                Html.Instance.End.Div.ClassName("editor__code__js").Render();
                jsElement = Html.Instance.GetContext();
                Html.Instance.End.End.Div.ClassName("editor").Style("display: flex;").Iframe.Width("100%").Render();
                previewElement = Html.Instance.GetContext() as HTMLIFrameElement;
                var ComponentGroup = Entity as Tenant;
                var hard = ComponentGroup.Id;
                var section = ComponentGroup.TenantCode.ToLower() + hard;
                var cssContent = Entity["Css"] is null ? string.Empty : Entity["Css"].ToString();
                var iframeDoc = previewElement.ContentWindow.Document;
                var htmlElementInsideIframe = iframeDoc.DocumentElement;
                Element = previewElement.ContentWindow.Document.Body;
                previewElement.Src = Window.Location.Href;
                UpdateViewComponent();
                previewElement.AddEventListener(EventType.Load, () =>
                {
                    var csstag = previewElement.ContentWindow.Document.Head.QuerySelector("#" + ComponentGroup.TenantCode + ComponentGroup.Id) as HTMLStyleElement;
                    if (csstag is null)
                    {
                        styleElement = Document.CreateElement(ElementType.style.ToString()) as HTMLStyleElement;
                        styleElement.Id = section;
                        styleElement.AppendChild(new Text(ComponentGroup.Css));
                        previewElement.ContentWindow.Document.Head.AppendChild(styleElement);
                    }
                    else
                    {
                        styleElement = csstag;
                    }
                });
            }
            /*@
            this.editorHtml = initCodeEditor(this,this.htmlElement,"Html","html");
            this.editorCss = initCodeEditor(this,this.cssElement,"Css","css");
            this.editorJavascript = initCodeEditor(this,this.jsElement,"Javascript","javascript");
            */
        }

        private bool firstLoad;
        public void UpdateViewComponent()
        {
            var ComponentGroup = Entity as Component;
            if (!firstLoad || Dirty)
            {
                firstLoad = true;
                return;
            }
            previewElement.Src = Window.Location.Href;
            previewElement.AddEventListener(EventType.Load, (Action)(() =>
            {
                var section = ComponentGroup.FieldName + ComponentGroup.Id;
                var csstag = previewElement.ContentWindow.Document.Head.QuerySelector((string)("#" + ComponentGroup.FieldName + ComponentGroup.Id)) as HTMLStyleElement;
                if (csstag is null)
                {
                    styleElement = Document.CreateElement(ElementType.style.ToString()) as HTMLStyleElement;
                    styleElement.Id = section;
                    styleElement.AppendChild(new Text(ComponentGroup.Css));
                    previewElement.ContentWindow.Document.Head.AppendChild(styleElement);
                }
                else
                {
                    styleElement = csstag;
                }
            }));
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            //
        }
    }
}
