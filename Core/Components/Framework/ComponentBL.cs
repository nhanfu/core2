using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
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
            var rs = await base.Save(entity);
            return rs;
        }
    }
}
