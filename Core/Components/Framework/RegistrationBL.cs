using Core.Components.Forms;
using Core.Models;
using Core.ViewModels;
using System.Threading.Tasks;

namespace Core.Components.Framework
{
    public class RegistrationBL : PopupEditor
    {
        public RegistrationVM VM => Entity as RegistrationVM;
        public RegistrationBL() : base(nameof(User))
        {
            Entity = new RegistrationVM();
            Name = "Registration";
            Title = Name;
            Public = true;
        }
        
        public async Task Register()
        {
            var isValid = await IsFormValid();
            if (!isValid)
            {
                return;
            }
            var result = await Client.PostAsync<bool>(VM, "Register", true);
            if (result)
            {
                ShowDialog();
            }
        }

        private void ShowDialog()
        {
            _confirm = new ConfirmDialog()
            {
                Content = $"Bạn đã đăng ký success, 1 email đã được gởi vào địa chỉ {VM.Email}.<br />" +
                                $"Vui lòng kiểm tra email để xác nhận việc đăng ký.<br />" +
                                $"Email gởi đến có thể nằm trong thùng rác.<br />" +
                                $"Xin lưu ý, duyệt tài khoản của bạn có thể diễn ra trong 1 - 3 ngày làm việc."
            };
            _confirm.YesText = "Đồng ý";
            _confirm.YesConfirmed += Dispose;
            _confirm.Canceled += Dispose;
            _confirm.IgnoreNoButton = true;
            _confirm.Render();
        }

        public override void Dispose()
        {
            _confirm?.Dispose();
            base.Dispose();
        }
    }
}
