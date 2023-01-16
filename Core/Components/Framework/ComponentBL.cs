using Core.Models;
using Core.ViewModels;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Threading.Tasks;

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
            DOMContentLoaded += AlterPosition;
        }

        private void AlterPosition()
        {
            Element.ParentElement.AddClass("properties");
        }

        public override Task<bool> Save(object entity)
        {
            var component = Entity.As<Component>();
            component.ClearReferences();
            return base.Save(entity);
        }
    }
}
