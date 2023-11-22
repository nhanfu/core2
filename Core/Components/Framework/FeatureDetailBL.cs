using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using System.Linq;

namespace Core.Components.Framework
{
    public class FeatureDetailBL : PopupEditor
    {
        private Feature FeatureEntity => Entity.CastProp<Feature>();

        public FeatureDetailBL() : base(nameof(Models.Feature))
        {
            Name = "FeatureEditor";
            Title = "Feature";
            PopulateDirty = false;
            Entity = new Feature();
            Config = true;
            DOMContentLoaded += AlterPosition;
        }

        private void AlterPosition()
        {
            Element.ParentElement.AddClass("properties");
        }

        public void EditGridColumn(object arg)
        {
            var header = arg as Component;
            var editor = new ComponentBL() { Entity = header, ParentElement = base.TabEditor.Element };
            var tab = Tabs.FirstOrDefault(x => x.Show);
            tab?.AddChild(editor);
        }
    }
}
