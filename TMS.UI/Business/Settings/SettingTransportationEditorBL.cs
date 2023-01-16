using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class SettingTransportationEditorBL : PopupEditor
    {
        public SettingTransportationEditorBL() : base(nameof(SettingTransportation))
        {
            Name = "SettingTransportation Editor";
        }
    }
}