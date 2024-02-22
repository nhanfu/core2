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
            Meta = ui ?? throw new ArgumentNullException(nameof(ui));
            DefaultValue = string.Empty;
        }

        public string Value { get; set; }

        public override void Render()
        {
            Html.Take(ParentElement).Clear().Div.Style($"width:{Meta.Width}px;margin:auto").Id("barcode" + Meta.Id);
            Element = Html.Context;
            Value = Entity.GetPropValue(FieldName)?.ToString();
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
            var value = Entity.GetPropValue(FieldName)?.ToString();
            if (value == Value)
            {
                return;
            }
            Render();
        }
    }
}
