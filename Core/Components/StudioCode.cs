using Bridge.Html5;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Retyped;
using System;
using System.Linq;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class StudioCode : EditableComponent
    {
        public object editorHtml;
        public object editorCss;
        public object editorJavascript;
        public HTMLElement htmlElement;
        public HTMLElement cssElement;
        public HTMLElement jsElement;
        public HTMLIFrameElement previewElement;
        public HTMLStyleElement styleElement;

        public StudioCode(Component ui, HTMLElement ele = null) : base(ui)
        {
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
                var ComponentGroup = Entity as Component;
                var hard = ComponentGroup.Id;
                var section = ComponentGroup.Name.ToLower() + hard;
                var cssContent = Entity["Css"] is null ? string.Empty : Entity["Css"].ToString();
                var iframeDoc = previewElement.ContentWindow.Document;
                var htmlElementInsideIframe = iframeDoc.DocumentElement;
                Element = previewElement.ContentWindow.Document.Body;
                htmlElementInsideIframe.SetAttribute("data-theme", Document.DocumentElement.GetAttribute("data-theme"));
                foreach (var link in Document.Head.QuerySelectorAll("link"))
                {
                    var link1 = link.CloneNode(true);
                    previewElement.ContentWindow.Document.Head.AppendChild(link1);
                }
                UpdateViewComponent();
                /*@
                    const regex = /(?:^|[\s\r\n])\.([a-zA-Z0-9-_]+)/g;
                    cssContent = cssContent.replace(regex, (match) => {
                        if (/\d/.test(match) || match.includes("minmax") || match.includes("/")) {
                            return match;
                        } else {
                            return match.replace(/([.])/, `[${section}]$1`);
                        }
                    });
                 */
                var csstag = previewElement.ContentWindow.Document.Head.QuerySelector("#" + ComponentGroup.Name + ComponentGroup.Id) as HTMLStyleElement;
                if (csstag is null)
                {
                    styleElement = Document.CreateElement(ElementType.style.ToString()) as HTMLStyleElement;
                    styleElement.Id = section;
                    styleElement.AppendChild(new Text(cssContent));
                    previewElement.ContentWindow.Document.Head.AppendChild(styleElement);
                }
                else
                {
                    styleElement = csstag;
                }
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
            var parentSection = EditForm.OpenFrom.FilterChildren<Section>(x => x.Meta != null && x.Meta.Id == Entity[IdField].ToString()).FirstOrDefault();
            Html.Take(Element).Clear();
            previewElement.ContentWindow.Document.Body.Style.MaxHeight = "700px";
            previewElement.ContentWindow.Document.Body.Style.Overflow = "scroll";
            Section.RenderSection(this, ComponentGroup, parentSection.Entity, parentSection.EditForm);
        }

        private HTMLElement htmPreview;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>")]
        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            //
        }
    }
}