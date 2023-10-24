using Core.Components.Extensions;
using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.User
{
    public class UserSettingBL : PopupEditor
    {
        Vendor Vendor => Parent.Entity as Vendor;
        public UserSettingBL() : base(nameof(UserSetting))
        {
            Name = "UserSetting";
            DOMContentLoaded += () =>
            {
                if (Vendor != null)
                {
                    return;
                }
                this.SetDataSourceGridView(nameof(UserSetting), $"?$filter=Active eq true and UserId eq '{Vendor.Id}'");
            };
        }
    }
}