using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Setting
{
    public class MasterDataDetailsBL: PopupEditor
    {
        public MasterDataDetailsBL() : base(nameof(MasterData))
        {
            Name = "MasterData Detail";
            Entity = new MasterData();
        }
    }
}
