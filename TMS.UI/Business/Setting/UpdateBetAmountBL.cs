using Core.Components.Extensions;
using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Setting
{
    class UpdateBetAmountBL : PopupEditor
    {
        public UpdateBetAmountBL() : base(nameof(MasterData))
        {
            Name = "UpdateBetAmount";
        }
    }
}
