using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
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
            DOMContentLoaded += AlterPosition;
        }

        private void AlterPosition()
        {
            Element.ParentElement.AddClass("properties");
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
            return rs;
        }
    }
}
