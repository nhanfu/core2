using Core.Clients;
using Core.Components.Forms;
using System.Threading.Tasks;
using UserModel = Core.Models.User;

namespace Core.Fw.User
{
    public class UserDetailBL : TabEditor
    {
        public UserModel UserEntity => Entity as UserModel;
        public UserDetailBL() : base(nameof(Core.Models.User))
        {
            Entity = new UserModel();
            Name = "User Detail";
            Title = "User Detail";
        }

        public async Task ReSend()
        {
            Client client = new Client(nameof(Core.Models.User));
            var res = await client.GetAsync<string>($"/ReSendUser/{UserEntity.Id}");
            var dialog = new ConfirmDialog()
            {
                Title = $"Đổi mật khẩu cho user {UserEntity.UserName}",
                Content = $"Đổi mật khẩu cho user thành công.<br />Mật khẩu mới là {res}"
            };
            dialog.IgnoreNoButton = true;
            dialog.Render();
        }
    }
}
