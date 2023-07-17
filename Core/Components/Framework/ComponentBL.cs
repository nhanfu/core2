using Core.Models;
using Core.ViewModels;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Threading.Tasks;
using Core.Components.Extensions;
using Core.MVVM;
using System.Linq;
using static Retyped.dom.DragEvent;

namespace Core.Components.Framework
{
    public class ComponentBL : PopupEditor
    {
        public ComponentBL() : base(nameof(Component))
        {
            Name = "ComponentEditor";
            Title = "Component properties";
            Icon = "fa fa-wrench";
            Id = "EditComponent_" + Id;
            Entity = new Component();
            PopulateDirty = false;
            Config = true;
            DOMContentLoaded += AlterPosition;
        }

        private void AlterPosition()
        {
            Element.ParentElement.AddClass("properties");
        }

        public override async Task<bool> Save(object entity)
        {
            var component = Entity.As<Component>();
            component.ClearReferences();
            var rs = await base.Save(entity);
            return rs;
        }
    }
}
