using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using System;
using System.Threading.Tasks;

namespace Core.Fw.User
{
    public class UserBL : TabEditor
    {
        public UserBL() : base(nameof(Core.Models.User))
        {
            Name = "User List";
            Title = Name;
        }

        public async Task EditUser(Core.Models.User user)
        {
            await InitUserForm(user);
        }

        public async Task CreateUser()
        {
            await InitUserForm(new Core.Models.User());
        }

        private async Task InitUserForm(Core.Models.User user)
        {
            await this.OpenTab(id: "User" + user.Id,
                featureName: "User Detail",
                factory: () =>
                {
                    var type = Type.GetType("Core.Fw.User.UserDetailBL");
                    var instance = Activator.CreateInstance(type) as TabEditor;
                    instance.Entity = user;
                    return instance;
                });
        }
    }
}
