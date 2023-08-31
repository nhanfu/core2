using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class PdfReport : EditableComponent
    {
        private const string ErrorMessage = "ErrorMessage";
        private const string DataNotFound = "Không tìm thấy dữ liệu";
        private const string TemplateNotFound = "Template is null or empty";
        private HTMLElement _rptContent;
        public object Selected { get; set; }
        public object[][] Data { get; set; }
        public bool HiddenButton { get; set; }
        public PdfReport(Component ui, HTMLElement ele = null) : base(ui)
        {
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
            Element = ele;
        }

        public override void Render()
        {
            if (Element == null)
            {
                Element = Html.Take(ParentElement).Div.GetContext();
            }
            Element.InnerHTML = null;
            AddSections();
            Task.Run(RenderAsync);
        }

        private void AddSections()
        {
            if (_rptContent != null || !Show)
            {
                return;
            }
            Element.Style.Display = Display.None;
            var html = Html.Take(Element);
            if (GuiInfo.Precision == 2)
            {
                html.Div.ClassName("printable").Style("page-break-before: always;");
                _rptContent = html.GetContext();
            }
            else
            {
                html.Div.ClassName("container-rpt");
                html.Div.ClassName("menuBar")
                .Div.ClassName("printBtn")
                    .Button.ClassName("btn btn-success mr-1 fa fa-print").Event(EventType.Click, () => EditForm.PrintSection(Element.QuerySelector(".printable") as HTMLElement, printPreview: true, component: GuiInfo)).End
                    .Button.ClassName("btn btn-success mr-1").Text("a4").Event(EventType.Click, async () => await GeneratePdf("a4")).End
                    .Button.ClassName("btn btn-success mr-1").Text("a5").Event(EventType.Click, async () => await GeneratePdf("a5")).End
                    .Button.ClassName("btn btn-success mr-1 fa fa-file-excel").Event(EventType.Click, () =>
                    {
                        if (!(_rptContent.QuerySelector("table") is HTMLTableElement table))
                        {
                            ConfirmDialog.RenderConfirm("Excel data not found in the report");
                            return;
                        }
                        ExcelExt.ExportTableToExcel(null, GuiInfo.Label ?? GuiInfo.FieldName, table);
                    }).End.Render();
                if (Client.SystemRole)
                {
                    html.Button.ClassName("btn btn-success mr-1").ClassName("far fa-eye")
                            .Event(EventType.Click, () => EditForm.PrintSection(Element.QuerySelector(".printable") as HTMLElement)).End.Render();
                }

                html.EndOf(".menuBar");
                html.Div.ClassName("printable").Style("page-break-before: always;");
                _rptContent = html.GetContext();
            }
        }

        public async Task GeneratePdf(string format)
        {
            await Client.LoadScript("https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js");
            var element = (Element.ParentElement.QuerySelector(".printable") as HTMLElement);
            if (GuiInfo.Precision == 2)
            {
                element = (Element.ParentElement.QuerySelector(".print-group")) as HTMLElement;
            }
            var printEl = element;
            printEl.Style.PageBreakBefore = null;
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

        public async Task RenderAsync()
        {
            if (!Show)
            {
                return;
            }
            DisposeChildren();
            string template;
            if (GuiInfo.Template.HasAnyChar())
            {
                template = GuiInfo.Template;
            }
            else
            {
                var featureTemplate = await new Client(nameof(Feature)).FirstOrDefaultAsync<Feature>(GuiInfo.DataSourceFilter);
                template = featureTemplate.Template;
            }
            if (template.IsNullOrEmpty())
            {
                throw new InvalidOperationException(TemplateNotFound);
            }
            var dataSet = await LoadData();
            if (dataSet is null)
            {
                ShowErrorMessage(DataNotFound);
                return;
            }
            if (dataSet[ErrorMessage] != null)
            {
                ShowErrorMessage(dataSet[ErrorMessage].ToString());
                return;
            }
            Element.Style.Display = string.Empty;
            var formatted = Utils.FormatEntity(template, null, dataSet, nullHandler: x => string.Empty);
            _rptContent.InnerHTML = formatted;
            var dsCount = 0;
            _rptContent.Children.ForEach(child =>
            {
                EditForm.BindingTemplate(child, this, false, dataSet, factory: (ele, component, parent, isLayout, entity) =>
                {
                    if (ele is HTMLTableElement table && table.Dataset["grid"] == "true")
                    {
                        dsCount++;
                        var ds = dataSet["ds" + dsCount];
                        var com = new Section(MVVM.ElementType.table)
                        {
                            Element = ele
                        };
                        var tbody = table.TBodies[0];
                        var arr = ds as object[];
                        if (arr.Nothing())
                        {
                            arr = new object[] { new object() };
                        }
                        var formattedRows = arr.Select(arrItem =>
                        {
                            var cloned = CloneRow(tbody.Rows.ToArray());
                            return BindingRowData(cloned, arrItem);
                        }).SelectMany(x => x).ToArray();
                        tbody.InnerHTML = null;
                        formattedRows.ForEach(x => tbody.AppendChild(x));
                        return com;
                    }
                    return EditForm.BindingData(ele, component, parent, isLayout, entity);
                });
            });
            if (GuiInfo != null && GuiInfo.Events.HasAnyChar())
            {
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.DOMContentLoaded, Entity);
            }
        }

        private void ShowErrorMessage(string message)
        {
            if (Element.Hidden())
            {
                return;
            }
            Element.Style.Display = Display.None;
            ConfirmDialog.RenderConfirm(message);
        }

        private HTMLTableRowElement[] BindingRowData(HTMLTableRowElement[] template, object arrItem)
        {
            if (arrItem is null || template.Nothing())
            {
                return null;
            }
            var res = new List<HTMLTableRowElement>();
            var subRowIndex = template.IndexOf(x => x.Dataset.ToDynamic().subRpt == "true");
            for (int i = 0; subRowIndex >= 0 && i < subRowIndex || subRowIndex <= 0 && i < template.Length; i++)
            {
                template[i].InnerHTML = Utils.FormatEntity(template[i].InnerHTML, null, arrItem, nullHandler: Utils.EmptyFormat, notFoundHandler: Utils.EmptyFormat);
                res.Add(template[i]);
            }

            if (subRowIndex < 0)
            {
                return res.ToArray();
            }
            var cloned = template.Skip(subRowIndex).ToArray();
            var subGridRows = BindingSubGrid(cloned, arrItem);
            if (subGridRows.HasElement())
            {
                res.AddRange(subGridRows);
            }
            return res.ToArray();
        }

        private HTMLTableRowElement[] BindingSubGrid(HTMLTableRowElement[] template, object groupItem)
        {
            var arrayField = "SubRpt";
            /*@
             for (var field in groupItem) if (groupItem[field] instanceof Array) arrayField = field;
             */
            var cells = template.SelectMany(x => x.Cells).Cast<HTMLElement>();
            var firstRowIndex = template.IndexOf(x => x == cells.FirstOrDefault(a => a.InnerHTML.Contains("{") && x.InnerHTML.Contains("}"))?.ParentElement);
            var lastRowIndex = template.IndexOf(x => x == cells.LastOrDefault(a => a.InnerHTML.Contains("{") && x.InnerHTML.Contains("}"))?.ParentElement);
            if (!(groupItem[arrayField] is object[] subRptArr))
            {
                return null;
            }
            var res = new List<HTMLTableRowElement>(template.Take(firstRowIndex));
            for (int i = 0; i < subRptArr.Length; i++)
            {
                var subTemplate = CloneRow(template.Skip(firstRowIndex).Take(lastRowIndex - firstRowIndex + 1).ToArray());
                var formatted = BindingRowData(subTemplate, subRptArr[i]);
                if (formatted.HasElement())
                {
                    res.AddRange(formatted);
                }
            }
            res.AddRange(template.Skip(lastRowIndex + 1));
            return res.ToArray();
        }

        private HTMLTableRowElement[] CloneRow(HTMLTableRowElement[] templateRow)
        {
            var res = new List<HTMLTableRowElement>();
            for (int i = 0; i < templateRow.Length; i++)
            {
                res.Add(templateRow[i].CloneNode(true) as HTMLTableRowElement);
            }
            return res.ToArray();
        }

        private async Task<object> LoadData()
        {
            object[][] dataSet = null;
            var datasource = await TryGetDataSource();
            if (datasource.IsNullOrWhiteSpace())
            {
                return null;
            }
            if (Data is null)
            {
                dataSet = Data = await new Client(nameof(User)).PostAsync<object[][]>(datasource, $"ReportDataSet?sys={GuiInfo.System ?? GuiInfo.IdField}");
                if (dataSet.Nothing() || dataSet.All(x => x.Nothing()))
                {
                    return null;
                }
            }

            var res = new object();
            /*@
            var hasSubRpt = false;
            for (var i = 0; i < dataSet.length; i++) {
                for (var j = 0; j < dataSet[i].length; j++) {
                    for (var prop in dataSet[i][j]) {
                        if (typeof(dataSet[i][j][prop]) === 'string') {
                            dataSet[i][j][prop]=Core.Extensions.Utils.DecodeSpecialChar(dataSet[i][j][prop]);
                        }
                    }
                }
	            if (i == 0) {
		            for (var field in dataSet[0][0]) {
			            res[field] = dataSet[0][0][field];
		            }
	            }
	            else {
		            var ds = dataSet[i];
		            var subRptField = [], groupField = [], subRptName;
		            if (ds == null || ds.length == 0) continue;
		            for (var field in ds[0]) if (field.indexOf('.') >= 0) subRptField.push(field); else groupField.push(field);
		            if (subRptField.length > 0) {
			            subRptName = subRptField[0].split('.')[0];
			            var fieldMap = subRptField.map(x => { return { field: x.split('.').reverse()[0], fullField: x} });
			            ds = System.Linq.Enumerable.from(ds)
				            .groupBy(x => System.String.concat(groupField.map(g => x[g])))
				            .select(x => { 
					            var arr = x.ToArray();
					            var first = arr[0];
					            var children = arr.map(item => {
                                    var res = {}; 
                                    fieldMap.forEach(field => { res[field.field] = item[field.fullField]; delete item[field.fullField]; });
                                    return res;
                                });
					            first[subRptName] = children;
					            return first;
				            }).ToArray();
		            }
		            res['ds' + i] = ds;
	            }
            }
            */
            if (Utils.IsFunction(GuiInfo.FormatEntity, out var formatter))
            {
                return formatter.Call(null, res, this);
            }
            return res;
        }

        private Task<string> TryGetDataSource()
        {
            var tcs = new TaskCompletionSource<string>();
            try
            {
                var isFnPowerQuery = Utils.IsFunction(GuiInfo.Query, out var fn);
                var isFnPreQuery = Utils.IsFunction(GuiInfo.PreQuery, out var preQueryFn);
                var preQuery = isFnPreQuery ? preQueryFn.Call(this, Entity, this, Selected) : null;
                string datasource = null;
                if (isFnPowerQuery)
                {
                    var query = fn.Call(this, preQuery ?? Entity, this, Selected);
                    if (query == null)
                    {
                        tcs.SetException(new NullReferenceException("Query is null"));
                    }
                    if (query.GetType() == typeof(string))
                    {
                        tcs.SetResult(query.ToString());
                    }
                    else
                    {
                        /*@
                        query.then(finalQuery => {
                           tcs.setResult(finalQuery);
                        });
                         */
                    }
                }
                else
                {
                    datasource = Utils.FormatEntity(GuiInfo.Query, Entity);
                    tcs.SetResult(datasource);
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            return tcs.Task;
        }

        private int _updateViewAwaiter;

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            Data = null;
            Window.ClearTimeout(_updateViewAwaiter);
            _updateViewAwaiter = Window.SetTimeout(async () => await RenderAsync(), 100);
        }
    }
}
