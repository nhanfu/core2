using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Threading.Tasks;

namespace Core.Components
{
    public class RecordDetail : Number
    {
        private string EntityEnum => Entity?[nameof(Feature.EntityId)]?.ToString();
        private Type EntityTye => Utils.GetEntityById(EntityEnum)?.GetEntityType();

        public RecordDetail(Component ui) : base(ui)
        {
        }

        public override void Render()
        {
            base.Render();
            RecordDetailIcon();
            Html.Take(Element).Event(EventType.Click, () =>
            {
                if (!Disabled)
                {
                    return;
                }
                Task.Run(OpenDetail);
            });
        }

        private void RecordDetailIcon()
        {
            Html.Take(Element.ParentElement)
                .Icon("fa fa-info-circle inline-acc")
                .Title($"Chi tiết dữ liệu")
                .AsyncEvent(EventType.Click, OpenDetail);
        }

        private async Task OpenDetail()
        {
            if (GuiInfo.RefClass.IsNullOrEmpty() || Value is null || Value == 0 || EntityEnum is null)
            {
                return;
            }

            var feature = ComponentExt.LoadEditorFeatureByNameByEntity(EntityEnum);
            var entity = new Client(EntityTye.Name).GetRawAsync((int)Value);
            await Task.WhenAll(feature, entity);
            if (feature.Result is null || entity.Result is null)
            {
                return;
            }

            var instance = Activator.CreateInstance(Type.GetType(GuiInfo.RefClass)) as TabEditor;
            instance.Id = feature.Result.Name;
            instance.Entity = entity.Result;
            instance.ParentForm = TabEditor;
            TabEditor.AddChild(instance);
        }
    }
}
