using Bridge.Html5;
using Core.Clients;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using System;

namespace Core.Components
{
    public class Workflow : EditableComponent
    {
        public Workflow(Component guiInfo) : base(guiInfo)
        {
        }

        public override void Render()
        {
            var isFn = Utils.IsFunction(Meta.PreQuery, out var fn);
            if (!isFn) return;
            var submitEntity = fn?.Call(null, this);
            var data = new SqlViewModel
            {
                ComId = "Workflow",
                Action = "GetAll",
                Params = submitEntity != null ? JSON.Stringify(submitEntity) : null,
                ConnKey = ConnKey
            };
            Html.Take(ParentElement).Div.ClassName("ddl").Style("position:relative;z-index:1;")
                .Button.Event(EventType.Click, x => Show = !Show).ClassName("dropbtn").IText(Meta.Label)
                    .Icon("fa fa-caret-down").Margin(MVVM.Direction.left, 3).EndOf(MVVM.ElementType.button)
                .Div.ClassName("dd-content").Style("position:absolute;background:white;border:1px solid #ccc;").Render();
            Element = Html.Context;
            Show = false;
            Client.Instance.UserSvc(data).Done(ds =>
            {
                if (ds.Nothing()) return;
                var isFormatFn = Utils.IsFunction(Meta.Renderer, out var render, false);
                if (!isFormatFn) return;
                var buttons = ds[0];
                buttons.ForEach(btn =>
                {
                    render.Call(null, this, btn);
                });
            });
        }
    }
}
