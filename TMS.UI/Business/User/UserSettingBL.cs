using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using System.Threading.Tasks;
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
                this.SetDataSourceGridView(nameof(UserSetting), $"?$filter=Active eq true and UserId eq {Vendor.Id}");
            };
        }

        public async Task Surcharge_AfterRowCreated(UserSetting user)
        {
            user.UserId = Client.Token.UserId;
            var surchargeGrid = this.FindComponentByName<GridView>(nameof(UserSetting));
            await surchargeGrid.AddOrUpdateRow(user);
        }
    }
}