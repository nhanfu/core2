using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using System.Linq;

namespace Core.Components.Framework
{
    public class FeatureDetailBL : TabEditor
    {
        private Component FeatureEntity => Entity.CastProp<Component>();

        public FeatureDetailBL() : base(nameof(Models.Component))
        {
            Name = "FeatureEditor";
            Title = "Feature";
            PopulateDirty = false;
            Entity = new Component();
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
