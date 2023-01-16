using Core.ViewModels;
using Core.Clients;
using Core.Components.Forms;
using Core.Extensions;
using System.Threading.Tasks;

namespace TMS.UI.Business.User
{
    public class ChangePasswordBL : PopupEditor
    {
        public ChangePasswordBL() : base(nameof(API.Models.User))
        {
            Name = "ChangePassword";
            Entity = new UserProfileVM();
        }

        public override async Task<bool> Save(object entity)
        {
            var vm = Entity as UserProfileVM;
            vm.ClearReferences();
            var saved = await Client.UpdateAsync<bool>(vm, "UpdateProfile");
            if (saved)
            {
                Toast.Success("Update profile succeeded!");
            }
            else
            {
                Toast.Warning("Update profile failed!");
            }
            return saved;
        }
    }
}