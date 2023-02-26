using Core.Models;
using Core.ViewModels;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Threading.Tasks;
using Core.MVVM;
using System.Linq;
using Core.Components.Extensions;

namespace Core.Components.Framework
{
    public class ComponentGroupBL : PopupEditor
    {
        private SyncConfigVM _syncConfig;

        private ComponentGroup ComGroupEntity => Entity as ComponentGroup;
        public ComponentGroupBL() : base(nameof(ComponentGroup))
        {
            Name = "ComponentGroup";
            Title = "Section properties";
            Icon = "fa fa-wrench";
            PopulateDirty = false;
        }

        public override async Task<bool> Save(object entity)
        {
            if (ComGroupEntity is null)
            {
                return false;
            }

            ComGroupEntity.ClearReferences();
            ComGroupEntity.Component.ForEach(x =>
            {
                x.Reference = null;
                x.ComponentGroup = null;
            });

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
