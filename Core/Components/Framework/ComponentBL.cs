using Core.Models;
using Core.ViewModels;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Threading.Tasks;
using Core.Components.Extensions;
using Core.MVVM;
using System.Linq;

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

        public override async Task<bool> Save(object entity)
        {
            var component = Entity.As<Component>();
            component.ClearReferences();
            var rs = await base.Save(entity);
            if (rs)
            {
                var tab = OpenFrom as EditForm;
                Html.Take(tab.Element).Clear();
                var feature = await ComponentExt.LoadFeatureComponent(tab.Feature);
                var groupTree = BuildTree(feature.ComponentGroup.ToList().OrderBy(x => x.Order).ToList());
                tab.RenderTabOrSection(groupTree);
            }
            return rs;
        }
    }
}
