using System.ComponentModel.DataAnnotations;

namespace Core.ViewModels
{
    public class LoginVM
    {
        [Display(Description = "Công ty")]
        public string CompanyName { get; set; }
        [Display(Description = "Tên đăng nhập")]
        [Required(ErrorMessage = "{0} không thể để trống")]
        public string UserName { get; set; }
        [Display(Description = "Mật khẩu")]
        [Required(ErrorMessage = "{0} không thể để trống")]
        public string Password { get; set; }
        public bool AutoSignIn { get; set; }
        public string RecoveryToken { get; set; }
        public string Env { get; set; }
    }
}
