using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;

namespace TMS.UI.Business.User
{
    public class UserBL : TabEditor
    {
        public UserBL() : base(nameof(API.Models.User))
        {
            Name = "User List";
            Title = Name;
        }

        public async Task EditUser(API.Models.User user)
        {
            await InitUserForm(user);
        }

        public async Task CreateUser()
        {
            await InitUserForm(new API.Models.User());
        }

        private async Task InitUserForm(API.Models.User user)
        {
            await this.OpenPopup(
                featureName: "User Detail",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.User.UserDetailBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = user.Id <= 0 ? "Add user" : "Edit user";
                    instance.Entity = user;
                    return instance;
                });
        }
    }
}
