using Bridge.Html5;
using Core.Clients;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Threading.Tasks;

namespace Core.Components
{
    public class BarCode : EditableComponent
    {
        public BarCode(Component ui, HTMLElement ele) : base(ui)
        {
            ParentElement = ele;
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public string Value { get; set; }

        public override void Render()
        {
            Html.Take(ParentElement).Clear().Div.Style($"width:{GuiInfo.Width}px;margin:auto").Id("barcode" + GuiInfo.Id);
            Element = Html.Context;
            Value = Entity.GetPropValue(GuiInfo.FieldName)?.ToString();
            Task.Run(async () =>
            {
                await Client.LoadScript("/js/qrcode.min.js");
                /*@
                 new QRCode("barcode"+this.GuiInfo.Id, {
                    text: value,
                    width: this.GuiInfo.Width,
                    height: this.GuiInfo.Width,
                    colorDark : "#000000",
                    colorLight : "#ffffff",
                });
                */
            });
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            var value = Entity.GetPropValue(GuiInfo.FieldName)?.ToString();
            if (value == Value)
            {
                return;
            }
            Render();
        }
    }
}
