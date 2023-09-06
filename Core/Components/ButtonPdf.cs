using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System.Linq;
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
            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Click, Entity, GuiInfo);
            Html.Take(Document.Body).Div.ClassName("backdrop")
                .Style("align-items: center;").Escape((e) => Dispose());
            _preview = Html.Context;
            Html.Instance.Div.ClassName("popup-content confirm-dialog").Style("top: 0;")
                .Div.ClassName("popup-title").InnerHTML(GuiInfo.PlainText)
                .Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                    .Event(EventType.Click, ClosePreview)
                .EndOf(".popup-title")
                .Div.ClassName("popup-body scroll-content");
            if (GuiInfo.Precision == 2)
            {
                Html.Instance.Div.ClassName("container-rpt");
                Html.Instance.Div.ClassName("menuBar")
                .Div.ClassName("printBtn")
                    .Button.ClassName("btn btn-success mr-1 fa fa-print").Event(EventType.Click, () => EditForm.PrintSection(_preview.QuerySelector(".print-group") as HTMLElement, printPreview: true, component: GuiInfo)).End
                    .Button.ClassName("btn btn-success mr-1").Text("a4").Event(EventType.Click, async () => await GeneratePdf("a4")).End
                    .Button.ClassName("btn btn-success mr-1").Text("a5").Event(EventType.Click, async () => await GeneratePdf("a5")).End
                    .Render();
                Html.Instance.EndOf(".menuBar").Div.ClassName("print-group");
            }
            var body = Html.Context;
            _pdfReport = new PdfReport(GuiInfo)
            {
                ParentElement = body,
            };
            if (GuiInfo.FocusSearch)
            {
                if (GuiInfo.Precision == 2)
                {
                    var parentGridView = TabEditor.FindActiveComponent<GridView>().FirstOrDefault();
                    var selectedData = parentGridView.CacheData;
                    if (selectedData.Nothing())
                    {
                        selectedData = parentGridView.RowData.Data;
                    }
                    var selectedRow = selectedData.Where(x => parentGridView.SelectedIds.Contains(int.Parse(x["Id"].ToString()))).ToList();
                    foreach (var item in selectedRow)
                    {
                        await Task.Delay(200);
                        var js = new PdfReport(GuiInfo)
                        {
                            ParentElement = body,
                            Selected = item
                        };
                        Parent.AddChild(js);
                    }
                    Window.SetTimeout(() =>
                    {
                        var ele = _preview.QuerySelectorAll(".print-group").Cast<HTMLElement>().ToList();
                        var printWindow = Window.Open("", "_blank");
                        printWindow.Document.Close();
                        printWindow.Document.Body.InnerHTML = ele.Select(x => x.OuterHTML).Combine("</br>");
                        printWindow.Print();
                        printWindow.AddEventListener(EventType.MouseMove, e => printWindow.Close());
                        printWindow.AddEventListener(EventType.Click, e => printWindow.Close());
                        printWindow.AddEventListener(EventType.AfterPrint, async e =>
                        {
                            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.AfterPrint, selectedRow);
                        });
                        printWindow.AddEventListener(EventType.KeyUp, e => printWindow.Close());
                        if (!GuiInfo.Style.IsNullOrWhiteSpace())
                        {
                            var style = Document.CreateElement(MVVM.ElementType.style.ToString()) as HTMLStyleElement;
                            style.AppendChild(new Text(GuiInfo.Style));
                            printWindow.Document.Head.AppendChild(style);
                        }
                        _pdfReport.Dispose();
                        _preview.Remove();
                    }, 2000);
                }
                else
                {
                    Parent.AddChild(_pdfReport);
                    var printWindow = Window.Open("", "_blank");
                    printWindow.Document.Close();
                    Window.SetTimeout(() =>
                    {
                        printWindow.Document.Body.InnerHTML = _pdfReport.Element.QuerySelector(".printable").OuterHTML;
                        printWindow.Print();
                        printWindow.AddEventListener(EventType.MouseMove, e => printWindow.Close());
                        printWindow.AddEventListener(EventType.Click, e => printWindow.Close());
                        printWindow.AddEventListener(EventType.KeyUp, e => printWindow.Close());
                        printWindow.AddEventListener(EventType.AfterPrint, async e =>
                        {
                            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.AfterPrint, EditForm);
                        });
                        if (!GuiInfo.Style.IsNullOrWhiteSpace())
                        {
                            var style = Document.CreateElement(MVVM.ElementType.style.ToString()) as HTMLStyleElement;
                            style.AppendChild(new Text(GuiInfo.Style));
                            printWindow.Document.Head.AppendChild(style);
                        }
                        _pdfReport.Dispose();
                        _preview.Remove();
                    }, 2000);
                }
            }
            else
            {
                if (GuiInfo.Precision == 2)
                {
                    var parentGridView = TabEditor.FindActiveComponent<GridView>().FirstOrDefault();
                    var selectedData = parentGridView.CacheData;
                    if (selectedData.Nothing())
                    {
                        selectedData = parentGridView.RowData.Data;
                    }
                    var selectedRow = selectedData.Where(x => parentGridView.SelectedIds.Contains(int.Parse(x["Id"].ToString()))).ToList();
                    foreach (var item in selectedRow)
                    {
                        await Task.Delay(200);
                        var js = new PdfReport(GuiInfo)
                        {
                            ParentElement = body,
                            Selected = item
                        };
                        Parent.AddChild(js);
                    }
                }
                else
                {
                    Parent.AddChild(_pdfReport);
                }
            }
        }

        public async Task GeneratePdf(string format)
        {
            await Client.LoadScript("https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js");
            var element = (_preview.QuerySelector(".print-group")) as HTMLElement;
            var printEl = element;
            var first = printEl.QuerySelectorAll(".printable").FirstOrDefault() as HTMLElement;
            first.Style.PageBreakBefore = null;
            /*@
            const openPdfInNewWindow = (pdf) => {
            const blob = pdf.output('blob');
            var isMobile = Window.Instance["Cordova"] != null;
            if (!isMobile)
            {
                window.open(window.URL.createObjectURL(blob));
                return;
            }
            var isAndroid = Window.Instance["device"]["platform"].ToString() == "Android";
                if (isAndroid)
                {
                    Window.Location.Href = window.URL.createObjectURL(blob);
                    return;
                }
                cordova.InAppBrowser.open(window.URL.createObjectURL(blob), "_system");
            };
            html2pdf(printEl, {
                filename : this.GuiInfo.PlainText,
                jsPDF: { format: format},
                image: { type: 'jpeg', quality: 0.98 },
                pdfCallback: openPdfInNewWindow,
            });

             */


        }

        private void ClosePreview()
        {
            _preview.Remove();
        }
    }
}