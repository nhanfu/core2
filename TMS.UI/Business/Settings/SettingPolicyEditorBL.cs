using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class SettingPolicyEditorBL : PopupEditor
    {
        public SettingPolicy SettingPolicyEntity => Entity as SettingPolicy;
        public SettingPolicyEditorBL() : base(nameof(SettingPolicy))
        {
            Name = "SettingPolicy Editor";
        }
    }
}