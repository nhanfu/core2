using Core.Components.Forms;
using Core.Extensions;
using Core.Models;

namespace Core.Components.Framework
{
    public class ComponentGroupBL : PopupEditor
    {
        private ComponentGroup ComGroupEntity => Entity as ComponentGroup;
        public ComponentGroupBL() : base(nameof(ComponentGroup))
        {
            Name = "ComponentGroup";
            Title = "Section properties";
            Icon = "fa fa-wrench";
            PopulateDirty = false;
            DOMContentLoaded += AlterPosition;
        }

        private void AlterPosition()
        {
            Element.ParentElement.AddClass("properties");
        }
    }
}
