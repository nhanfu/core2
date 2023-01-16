using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class ShipEditorBL : PopupEditor
    {
        public ShipEditorBL() : base(nameof(Ship))
        {
            Name = "Ship Editor";
        }
    }
}