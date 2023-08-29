using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components.Extensions;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TextAlign = Core.Enums.TextAlign;
using Core.Enums;

namespace Core.Components
{
    public class CellText : EditableComponent
    {
        public Dictionary<string, List<object>> RefData { get; set; }

        public CellText(Component ui, HTMLElement ele = null) : base(ui)
        {
            GuiInfo = ui;
            Element = ele;
        }

        public override void Render()
        {
            SetDefaultVal();
            var cellData = Entity.GetComplexPropValue(GuiInfo.FieldName);
            var isBool = cellData != null && cellData.GetType().IsBool();
            string cellText = string.Empty;
            if (Element is null)
            {
                RenderNewEle(cellText, cellData, isBool);
            }
            if (GuiInfo.Query.HasAnyChar())
            {
                Task.Run(RenderCellTextAsync);
                return;
            }
            else
            {
                cellText = CalcCellText(cellData);
                UpdateEle(cellText, cellData, isBool);
            }
            Element.Closest("td")?.AddEventListener(EventType.KeyDown, ListViewItemTab);
            Element.ParentElement.TabIndex = -1;
        }

        private void UpdateEle(string cellText, object cellData, bool isBool)
        {
            if (isBool)
            {
                if (GuiInfo.SimpleText)
                {
                    Element.InnerHTML = (bool?)cellData == true ? "☑" : "☐";
                }
                else
                {
                    Element.PreviousElementSibling.As<HTMLInputElement>().Checked = (bool)cellData;
                }
                return;
            }
            Element.InnerHTML = cellText;
            Element.SetAttribute("title", cellText);
        }

        private void RenderNewEle(string cellText, object cellData, bool isBool)
        {
            Html.Take(ParentElement).TextAlign(CalcTextAlign(GuiInfo, cellData));
            if (isBool)
            {
                if (GuiInfo.SimpleText)
                {
                    Html.Instance.Text((bool?)cellData == true ? "☑" : "☐");
                    Html.Context.Style.FontSize = "1.2rem";
                }
                else
                {
                    Html.Instance.Padding(Direction.bottom, 0)
                        .SmallCheckbox((bool)Entity.GetComplexPropValue(GuiInfo.FieldName));
                    Html.Context.PreviousElementSibling.As<HTMLInputElement>().Disabled = true;
                }

            }
            else
            {
                var containDiv = cellText.Substring(0, 4) == "<div>";
                if (containDiv)
                {
                    Html.Instance.Div.Render();
                }
                else
                {
                    Html.Instance.Span.Render();
                }

                Html.Instance.AsyncEvent(EventType.Click, LabelClickHandler).ClassName("cell-text").InnerHTML(cellText);
            }
            Element = Html.Context;
            Html.Instance.End.Render();
        }

        private string CalcCellText(object cellData)
        {
            string cellText = null;
            if (GuiInfo.IsPivot)
            {
                var fields = GuiInfo.FieldName.Split(".");
                if (fields.Length < 3)
                {
                    return cellText;
                }

                if (!(Entity.GetComplexPropValue(fields[0]) is IEnumerable<object> listData))
                {
                    return cellText;
                }

                var restPivotField = string.Join(".", fields.Skip(1).Take(fields.Length - 2));
                var row = listData.FirstOrDefault(x => x.GetComplexPropValue(restPivotField)?.ToString() == fields.Last().ToString());
                cellText = row == null ? string.Empty : Utils.FormatEntity(GuiInfo.FormatEntity, row);
            }
            else
            {
                cellText = Utils.GetCellText(GuiInfo, cellData, Entity, RefData, false, EmptyRow);
            }
            if (cellText is null || cellText == "null")
            {
                cellText = "N/A";
            }
            return cellText;
        }

        private async Task RenderCellTextAsync()
        {
            if (GuiInfo.Query.IsNullOrEmpty())
            {
                return;
            }
            var isFn = Utils.IsFunction(GuiInfo.Query, out var fn);
            var datasource = isFn ? fn.Call(this, Entity, this).ToString() : Utils.FormatEntity(GuiInfo.Query, Entity);
            var data = await new Client(nameof(User), typeof(User).Namespace).PostAsync<object[]>(datasource, "ReportQuery");
            if (data.Nothing())
            {
                return;
            }
            var isFormatFn = Utils.IsFunction(GuiInfo.FormatEntity, out var formatter);
            var text = isFormatFn ? formatter.Apply(this, new object[] { data }).ToString() : Utils.FormatEntity(GuiInfo.FormatEntity, data[0]);

            UpdateEle(text, null, false);
        }

        private Task LabelClickHandler(Event e)
        {
            return this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Click, Entity);
        }

        public static TextAlign CalcTextAlign(Component header, object cellData)
        {
            var textAlign = header.TextAlignEnum;
            if (textAlign != null)
            {
                return textAlign.Value;
            }

            if (header.ReferenceId != null || cellData is null || cellData is string)
            {
                return TextAlign.left;
            }

            if (cellData.GetType().IsNumber())
            {
                return TextAlign.right;
            }

            if (cellData is bool || cellData is bool?)
            {
                return TextAlign.center;
            }

            return TextAlign.center;
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            PrepareUpdateView(force, dirty);
            Render();
        }

    }
}
