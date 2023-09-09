using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.MVVM;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class Table : GridView
    {
        private HTMLTableSectionElement _bodyEle;
        private HTMLElement _headerEle;
        private HTMLElement _btnEle;
        public object[][] Data { get; set; }

        public Table(Component ui) : base(ui)
        {
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public override void Render()
        {
            Html.Take(ParentElement).Div.ClassName("table-report").Style("overflow: auto;")
                .Button.ClassName("btn-toolbar fas fa-bars box-shadow").Style("padding: 8px;").Event(EventType.Click, ShowExportMenu).End
                .Table.ClassName("table").Thead.End.TBody.Render();
            _bodyEle = Html.Context as HTMLTableSectionElement;
            _headerEle = _bodyEle.PreviousElementSibling;
            Element = _bodyEle.ParentElement.ParentElement;
            _btnEle = Element.FirstElementChild;
            Task.Run(RenderAsync);
        }

        private void ShowExportMenu(Event e)
        {
            var ctx = ContextMenu.Instance;
            ctx.Top = e.Top();
            ctx.Left = e.Left();
            ctx.MenuItems = new System.Collections.Generic.List<ContextMenuItem>
            {
                new ContextMenuItem { Text = "Excel", Icon = "fa fa-file-excel", Click = ExportExcel },
                new ContextMenuItem { Text = "Pdf", Icon = "fa fa-file-pdf", Click = ExportPdf }
            };
            ctx.Render();
            ctx.Element.AlterPosition(_btnEle);
        }

        public void ExportExcel(object arg)
        {
            ExcelExt.ExportTableToExcel(null, GuiInfo.FieldName, Element.ParentElement);
        }

        public void ExportPdf(object arg)
        {
            EditForm.PrintSection(Element.ParentElement);
        }

        private async Task RenderAsync()
        {
            if (GuiInfo.FormatData.HasAnyChar())
            {
                Header = JsonConvert.DeserializeObject<Component[]>(GuiInfo.FormatData).ToList();
            }
            var isFn = Utils.IsFunction(GuiInfo.Query, out var fn);
            if (!isFn)
            {
                return;
            }
            var DataSourceFilter = fn.Call(this, Entity).ToString();
            if (Data is null)
            {
                Data = await new Client(nameof(User)).PostAsync<object[][]>(DataSourceFilter, "ReportDataSet");
            }
            if (Data.Nothing())
            {
                NoRecordFound();
                return;
            }
            DisposeNoRecord();
            var firstData = Data.FirstOrDefault()?.FirstOrDefault();
            var header = Header.ToArray();
            Html.Take(_headerEle).Render();
            var html = Html.Instance;
            /*@
            for (var key in firstData) {
                if (header != null && header.map(x => x.FieldName).indexOf(key) >= 0) {
                    var matchHeader = header.filter(x => x.FieldName == key)[0];
                    this.RenderHeader(matchHeader && matchHeader.ShortDesc);
                } else {
                    this.RenderHeader(key);
                }
            }
            */
            await LoadMasterData(Data);
            foreach (var data in Data)
            {
                Html.Take(_bodyEle).TRow.Render();
                /*@
                for (var key in firstData) {
                    if (header != null && header.map(x => x.FieldName).indexOf(key) >= 0) {
                        var matchHeader = header.filter(x => x.FieldName == key);
                        this.RenderCell(data[key], matchHeader[0] && matchHeader[0].FormatData);
                    } else {
                        this.RenderCell(data[key]);
                    }
                }
                */
            }
            DOMContentLoaded?.Invoke();
        }

        private void RenderHeader(string text)
        {
            Header.Add(new Component
            {
                ShortDesc = text
            });
            Html.Instance.Th.IText(text).End.Render();
        }

        private void RenderCell(string text, string formatData)
        {
            var finalText = formatData.IsNullOrEmpty() ? text : string.Format(formatData, text);
            Html.Instance.TData.IText(finalText).End.Render();
        }
    }
}
