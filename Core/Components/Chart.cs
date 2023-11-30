using Bridge.Html5;
using Core.Clients;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class Chart : EditableComponent
    {
        public object[] Data { get; set; }

        public Chart(Component ui) : base(ui)
        {
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public override void Render()
        {
            AddElement();
            Task.Run(async () =>
            {
                await Client.LoadScript("/js/canvasjs.min.js");
                await Task.Delay(1000);
                await RenderAsync();
            });
        }

        private void AddElement()
        {
            if (Element != null)
            {
                return;
            }
            Element = Html.Take(ParentElement).Div.ClassName("chart-wrapper").GetContext();
        }

        public async Task RenderAsync()
        {
            await RenderChart();
            DOMContentLoaded?.Invoke();
        }

        private async Task RenderChart()
        {
            AddElement();
            var isFn = Utils.IsFunction(GuiInfo.Query, out var fn);
            var dataSourceFilter = isFn ? fn.Call(this, Entity, this).ToString() : Utils.FormatEntity(GuiInfo.Query, Entity);
            if (Data is null)
            {
                var isPreQueryFn = Utils.IsFunction(GuiInfo.PreQuery, out var _preQuery);
                var submitEntity = isPreQueryFn ? _preQuery.Call(null, this) : null;
                var entity = JSON.Stringify(new SqlViewModel
                {
                    Entity = isPreQueryFn ? JSON.Stringify(submitEntity) : null,
                    ComId = GuiInfo.Id,
                });
                Data = await Client.Instance.SubmitAsync<object[]>(new XHRWrapper
                {
                    Url = Utils.ComQuery,
                    IsRawString = true,
                    Value = entity,
                    Method = Enums.HttpMethod.POST
                });
            }
            var type = GuiInfo.ClassName ?? "pie";
            var text = GuiInfo.PlainText;
            object options = null;
            if (GuiInfo.FormatData.IsNullOrEmpty())
            {
                /*@
                options = {
                    theme: "light2",
                    animationEnabled: true,
                    showInLegend: "true",
		            legendText: "{name}",
                    title: {
                        text: text,
                        fontFamily: "roboto",
                        fontSize: 15
                    },
                    data: [{
                        type: type,
                        toolTipContent: "{label} {y}",
                        dataPoints: this.Data
                    }],
                    legend: {
                        cursor:"pointer",
                        fontSize: 9,
                        fontFamily: "roboto",
                    }
                };
                */
            }
            else
            {
                options = JSON.Parse(GuiInfo.FormatData);
            }
            var isFotmatDataFn = Utils.IsFunction(GuiInfo.FormatEntity, out var function);
            if (!isFotmatDataFn && !GuiInfo.GroupBy.IsNullOrEmpty())
            {
                options["data"] = Data.Select(data =>
                {
                    data["type"] = data["type"] ?? type;
                    return data;
                })
                .GroupBy(x => new { type = x["type"].ToString(), name = x["name"]?.ToString(), axisYType = x["axisYType"]?.ToString() })
                .Select(x => new { type = x.Key.type, toolTipContent = x.FirstOrDefault()["toolTipContent"], axisYType = x.Key.axisYType, dataPoints = x.ToArray() }).ToArray();
            }
            else if (isFotmatDataFn)
            {
                options["data"] = function.Call(this, Data);
            }
            else
            {
                options["data"] = new object[] { new { type = type, toolTipContent = "{label} {y}", dataPoints = Data } };
            }
            /*@
            var chart = new CanvasJS.Chart(this.Element, options);
            chart.render();
            */
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            if (force)
            {
                Data = null;
            }
            if (Element != null)
            {
                Element.InnerHTML = null;
            }
            Task.Run(RenderChart);
        }
    }
}