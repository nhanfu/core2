using Core.Components.Extensions;
using Core.Components.Forms;
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