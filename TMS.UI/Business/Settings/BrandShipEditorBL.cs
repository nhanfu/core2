using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class BrandShipEditorBL : PopupEditor
    {
        public BrandShipEditorBL() : base(nameof(BrandShip))
        {
            Name = "BrandShip Editor";
        }
    }
}