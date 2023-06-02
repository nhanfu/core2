using Bridge.Html5;
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

        public void ChangeType()
        {
            var dialog = new ConfirmDialog()
            {
                Title = $"Chuyển cộng tác viên",
                Content = $"Vui lòng nhập thời gian áp dụng",
                NeedAnswer = true,
                ComType = nameof(Datepicker)
            };
            dialog.Render();
            dialog.YesConfirmed += async () =>
            {
                var value = dialog.Datepicker.Value;
                var rs = await new Client(nameof(Vendor)).PostAsync<bool>(UserEntity, "BackToSale?dateTime=" + value);
                if (rs)
                {
                    Toast.Success("Thay đổi thành công");
                }
                else
                { 
                    Toast.Warning("Thay đổi thất bại");
                }
            };
        }

        public void ChangeType1()
        {
            var dialog = new ConfirmDialog()
            {
                Title = $"Chuyển sale chỉnh thức",
                Content = $"Vui lòng nhập thời gian áp dụng",
                NeedAnswer = true,
                ComType = nameof(Datepicker)
            };
            dialog.Render();
            dialog.YesConfirmed += async () =>
            {
                var value = dialog.Datepicker.Value;
                var rs = await new Client(nameof(Vendor)).PostAsync<bool>(UserEntity, "ReturnToSale?dateTime=" + value);
                if (rs)
                {
                    Toast.Success("Thay đổi thành công");
                }
                else
                {
                    Toast.Warning("Thay đổi thất bại");
                }
            };
        }

        public void PrintDebitCredit()
        {
            var preview = this.FindComponentByName<Section>("ViewA4");
            var print = Window.Open("", "_blank");
            var shtml = "<html>";
            shtml += "<link rel='stylesheet' type='text/css' href='./css/styleprint.css' />";
            shtml += "<link href='./css/font-awesome.css' rel='stylesheet' />";
            shtml += "<link href='./css/metro-all.css' rel='stylesheet' />";
            shtml += "<link href='./css/main.css' rel='stylesheet' />";
            shtml += "<link href='./css/LineIcons.css' rel='stylesheet' />";
            shtml += "<body onload=\"window.print();\">";
            shtml += "<div style='padding:7pt'>";
            shtml += preview.ParentElement.InnerHTML;
            shtml += "</div>";
            shtml += "</body>";
            shtml += "</html>";
            print.Document.Write(shtml);
            print.Document.Close();
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
