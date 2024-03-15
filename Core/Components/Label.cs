using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components.Extensions;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using TextAlign = Core.Enums.TextAlign;
using Core.ViewModels;
using Direction = Core.MVVM.Direction;

namespace Core.Components
{
    public class Label : EditableComponent
    {
        public Dictionary<string, List<object>> RefData { get; set; }

        public Label(Component ui, HTMLElement ele = null) : base(ui)
        {
            Meta = ui;
            Element = ele;
        }

        public override void Render()
        {
            SetDefaultVal();
            var cellData = Utils.GetPropValue(Entity, FieldName);
            var isBool = cellData != null && cellData.GetType().IsBool();
            string cellText = string.Empty;
            if (Element is null)
            {
                RenderNewEle(cellText, cellData, isBool);
            }
            if (!Meta.Query.IsNullOrWhiteSpace()
                && Utils.IsFunction(Meta.FormatEntity, out var formatter))
            {
                RenderCellText(formatter);
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
                if (Meta.SimpleText)
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
            Html.Take(ParentElement).TextAlign(CalcTextAlign(Meta, cellData));
            if (isBool)
            {
                if (Meta.SimpleText)
                {
                    Html.Instance.Text((bool?)cellData == true ? "☑" : "☐");
                    Html.Context.Style.FontSize = "1.2rem";
                }
                else
                {
                    Html.Instance.Padding(Direction.bottom, 0)
                        .SmallCheckbox((bool)Utils.GetPropValue(Entity, FieldName));
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

                Html.Instance.Event(EventType.Click, LabelClickHandler).ClassName("cell-text").InnerHTML(cellText);
            }
            Element = Html.Context;
            Html.Instance.End.Render();
        }

        private string CalcCellText(object cellData)
        {
            string cellText = null;
            if (Meta.IsPivot)
            {
                var fields = FieldName.Split(".");
                if (fields.Length < 3)
                {
                    return cellText;
                }

                if (!(Utils.GetPropValue(Entity, fields[0]) is IEnumerable<object> listData))
                {
                    return cellText;
                }

                var restPivotField = string.Join(".", fields.Skip(1).Take(fields.Length - 2));
                var row = listData.FirstOrDefault(x => x.GetPropValue(restPivotField)?.ToString() == fields.Last().ToString());
                cellText = row == null ? string.Empty : Utils.FormatEntity(Meta.FormatEntity, row);
            }
            else
            {
                cellText = Utils.GetCellText(Meta, cellData, Entity, EmptyRow);
            }
            if (cellText is null || cellText == "null")
            {
                cellText = "N/A";
            }
            return cellText;
        }

        private void RenderCellText(Function formatter)
        {
            if (Meta.Query.IsNullOrEmpty() || formatter is null)
            {
                return;
            }
            var isFn = Utils.IsFunction(Meta.PreQuery, out var fn);
            var entity = isFn ? fn.Call(this, this).ToString() : string.Empty;
            var submit = new SqlViewModel
            {
                MetaConn = MetaConn,
                DataConn = DataConn,
                Params = JSON.Stringify(entity),
                ComId = Meta.Id
            };
            Client.Instance.SubmitAsync<object[]>(new XHRWrapper
            {
                Url = Utils.ComQuery,
                IsRawString = true,
                Value = JSON.Stringify(submit),
                Method = Enums.HttpMethod.POST
            }).Done(data =>
            {
                if (data.Nothing())
                {
                    return;
                }
                var text = formatter.Apply(this, new object[] { data }).ToString();
                UpdateEle(text, null, false);
            });
        }

        private void LabelClickHandler(Event e)
        {
            this.DispatchEvent(Meta.Events, EventType.Click, Entity).Done();
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

        public override string GetValueTextAct()
        {
            return Element.TextContent;
        }
    }
}
