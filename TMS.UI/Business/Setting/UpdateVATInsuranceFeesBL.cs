using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Setting
{
    class UpdateVATInsuranceFeesBL : PopupEditor
    {
        public UpdateVATInsuranceFeesBL() : base(nameof(MasterData))
        {
            Name = "UpdateVATInsuranceFees";
        }
    }
}
