using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using UserModel = TMS.API.Models.User;

namespace TMS.UI.Business.User
{
    public class UserDetailBL : PopupEditor
    {
        public UserModel UserEntity => Entity as UserModel;
        public GridView gridView;
        public UserDetailBL() : base(nameof(API.Models.User))
        {
            Entity = new UserModel();
            Name = "User Detail";
        }

        public async Task ReSend()
        {
            Client client = new Client(nameof(API.Models.User));
            var res = await client.GetAsync<string>($"/ReSendUser/{UserEntity.Id}");
            var dialog = new ConfirmDialog()
            {
                Title = $"Đổi mật khẩu cho user {UserEntity.UserName}",
                Content = $"Đổi mật khẩu cho user success.<br />Mật khẩu mới là {res}"
            };
            dialog.IgnoreNoButton = true;
            dialog.Render();
        }

        public void Check_User(UserRole userRole, Role role)
        {
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault();
            if (UserEntity.UserRole.Any(x => x.Id != userRole.Id && x.RoleId == role.Id))
            {
                Toast.Warning("Tài khoản này đã tồn tại !!!");
                gridView.RemoveRow(userRole);
            }
        }
    }
}
