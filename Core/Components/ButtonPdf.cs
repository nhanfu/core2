using Bridge.Html5;
using Core.Components.Forms;
using Core.Models;
using Core.MVVM;
using System.Text;
using System.Threading.Tasks;

namespace Core.Components
{
    public class ButtonPdf : Button
    {
        public HTMLElement _preview { get; set; }
        public PdfReport _pdfReport { get; set; }
        public ButtonPdf(Component ui, HTMLElement ele = null) : base(ui, ele)
        {
        }

        public override async Task DispatchClickAsync()
        {
            Html.Take(Document.Body).Div.ClassName("backdrop")
                .Style("align-items: center;").Escape((e) => Dispose());
            _preview = Html.Context;
            Html.Instance.Div.ClassName("popup-content confirm-dialog").Style("top: 0;")
                .Div.ClassName("popup-title").InnerHTML(GuiInfo.PlainText)
                .Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                    .Event(EventType.Click, ClosePreview)
                .EndOf(".popup-title")
                .Div.ClassName("popup-body scroll-content");
            var body = Html.Context;
            _pdfReport = new PdfReport(GuiInfo)
            {
                ParentElement = body,
            };
            if (GuiInfo.FocusSearch)
            {
                Parent.AddChild(_pdfReport);
                Window.SetTimeout(() =>
                {
                   var htmlBuilder = new StringBuilder("<html><head>");
                   htmlBuilder.Append("<body><div style='padding:7pt'>").Append(_pdfReport.Element.QuerySelector(".printable").InnerHTML).Append("</div></body></html>");
                   var html = htmlBuilder.ToString();
                   var printWindow = Window.Open("", "_blank");
                   printWindow.Document.Write(html);
                   printWindow.Document.Close();
                   printWindow.Print();
                   printWindow.AddEventListener(EventType.MouseMove, e => printWindow.Close());
                   printWindow.AddEventListener(EventType.Click, e => printWindow.Close());
                   printWindow.AddEventListener(EventType.KeyUp, e => printWindow.Close());
                   _pdfReport.Dispose();
                   _preview.Remove();
               }, 2000);
            }
            else
            {
                Parent.AddChild(_pdfReport);
            }
        }

        private void ClosePreview()
        {
            _preview.Remove();
        }
    }
}