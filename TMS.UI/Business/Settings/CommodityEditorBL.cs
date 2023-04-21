using Core.Components.Extensions;
using Core.Components.Forms;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class CommodityEditorBL : PopupEditor
    {
        public MasterData masterDataEntity => Entity as MasterData;
        public CommodityEditorBL() : base(nameof(MasterData))
        {
            Name = "Commodity Editor";
        }

        public override Task<bool> Save(object entity = null)
        {
            if (masterDataEntity.ParentId != 7651)
            {
                masterDataEntity.Path += $"{masterDataEntity.ParentId}" + @"\";
            }
            return base.Save(entity);
        }

        public override Task<bool> SaveWithouUpdateView(object entity)
        {
            if (masterDataEntity.ParentId != 7651)
            {
                masterDataEntity.Path += $"{masterDataEntity.ParentId}" + @"\";
            }
            return base.SaveWithouUpdateView(entity);
        }

        public void CheckParentId(MasterData masterData)
        {
            if (masterData.ParentId == 7651)
            {
                this.SetDisabled(true, "ParentId");
            }
        }

        public void BeforeMasterData(MasterData masterData)
        {
            masterData.ParentId = masterDataEntity.Id;
            masterData.Path = @"\7651\" + masterDataEntity.Id + @"\";
            masterData.Level = 2;
        }
    }
}