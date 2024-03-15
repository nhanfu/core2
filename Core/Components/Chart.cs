using Bridge.Html5;
using Core.Clients;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
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
            Meta = ui ?? throw new ArgumentNullException(nameof(ui));
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
            if (Data is null)
            {
                var isPreQueryFn = Utils.IsFunction(Meta.PreQuery, out var _preQuery);
                var submitEntity = isPreQueryFn ? _preQuery.Call(null, this) : null;
                var entity = JSON.Stringify(new SqlViewModel
                {
                    Params = isPreQueryFn ? JSON.Stringify(submitEntity) : null,
                    ComId = Meta.Id,
                    MetaConn = MetaConn,
                    DataConn = DataConn,
                });
                Data = await Client.Instance.SubmitAsync<object[]>(new XHRWrapper
                {
                    Url = Utils.ComQuery,
                    IsRawString = true,
                    Value = entity,
                    Method = Enums.HttpMethod.POST
                });
            }
            var type = Meta.ClassName ?? "pie";
            var text = Meta.PlainText;
            object options = null;
            if (Meta.FormatData.IsNullOrEmpty())
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
                options = JSON.Parse(Meta.FormatData);
            }
            var isFotmatDataFn = Utils.IsFunction(Meta.FormatEntity, out var function);
            if (!isFotmatDataFn && !Meta.GroupBy.IsNullOrEmpty())
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