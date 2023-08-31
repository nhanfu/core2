using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System.Linq;
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
            if(GuiInfo.Precision == 2)
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
                        var htmlBuilder = new StringBuilder("<html><head>");
                        htmlBuilder.Append("<body><div style='padding:7pt'>").Append(ele.Select(x => x.OuterHTML).Combine("</br>")).Append("</div></body></html>");
                        var html = htmlBuilder.ToString();
                        var printWindow = Window.Open("", "_blank");
                        printWindow.Document.Open();
                        printWindow.Document.Write(html);
                        printWindow.Document.Close();
                        printWindow.Print();
                        printWindow.AddEventListener(EventType.MouseMove, e => printWindow.Close());
                        printWindow.AddEventListener(EventType.Click, e => printWindow.Close());
                        printWindow.AddEventListener(EventType.AfterPrint, async e =>
                        {
                            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.AfterPrint, selectedRow);
                        });
                        printWindow.AddEventListener(EventType.KeyUp, e => printWindow.Close());
                        _pdfReport.Dispose();
                        _preview.Remove();
                    }, 2000);
                }
                else
                {
                    Parent.AddChild(_pdfReport);
                    Window.SetTimeout(() =>
                    {
                        var htmlBuilder = new StringBuilder("<html><head>");
                        htmlBuilder.Append("<body><div style='padding:7pt'>").Append(_pdfReport.Element.QuerySelector(".printable").OuterHTML).Append("</div></body></html>");
                        var html = htmlBuilder.ToString();
                        var printWindow = Window.Open("", "_blank");
                        printWindow.Document.Write(html);
                        printWindow.Document.Close();
                        printWindow.Print();
                        printWindow.AddEventListener(EventType.MouseMove, e => printWindow.Close());
                        printWindow.AddEventListener(EventType.Click, e => printWindow.Close());
                        printWindow.AddEventListener(EventType.KeyUp, e => printWindow.Close());
                        printWindow.AddEventListener(EventType.AfterPrint, async e =>
                        {
                            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.AfterPrint, EditForm);
                        });
                        _pdfReport.Dispose();
                        _preview.Remove();
                    }, 2000);
                }
            }
            else
            {
                if(GuiInfo.Precision == 2)
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
            window.open(window.URL.createObjectURL(blob));
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