using Core.Components.Extensions;
using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class RegionEditorBL : PopupEditor
    {
        public MasterData masterDataEntity => Entity as MasterData;
        public RegionEditorBL() : base(nameof(MasterData))
        {
            Name = "Region Editor";
        }

        public void CheckParentId(MasterData masterData)
        {
            if (masterData.ParentId == 7569)
            {
                this.SetDisabled(true, "ParentId");
            }
        }

        public void BeforeMasterData(MasterData masterData)
        {
            masterData.ParentId = masterDataEntity.Id;
            masterData.Path = @"\7569\" + masterDataEntity.Id + @"\";
            masterData.Level = 2;
        }
    }
}