using Core.Models;
using Core.ViewModels;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Threading.Tasks;

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
            return await base.Save(entity);
        }
    }
}
