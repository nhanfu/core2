using System.ComponentModel.DataAnnotations;

namespace Core.ViewModels
{
    public class RegistrationVM
    {
        [Display(Description = "Email")]
        [Required(ErrorMessage = "{0} không thể để trống")]
        public string Email { get; set; }

        [Display(Description = "Mật khẩu")]
        [Required(ErrorMessage = "{0} không thể để trống")]
        public string Password { get; set; }

        [Display(Description = "Xác Nhận Mật khẩu")]
        [Required(ErrorMessage = "{0} không thể để trống")]
        public string ConfirmPassword { get; set; }

        [Display(Description = "Họ và Tên")]
        [Required(ErrorMessage = "{0} không thể để trống")]
        public string FullName { get; set; }


        [Display(Description = "Địa chỉ")]
        public string Address { get; set; }

        public bool IsVendor { get; set; }

        [Display(Description = "Mã số thuế")]
        public string TaxCode { get; set; }

        [Display(Description = "Tên viết tắt")]
        public string LocalShortName { get; set; }

        [Display(Description = "Tên đầy đủ")]
        public string LocalFullName { get; set; }

        [Display(Description = "Tên quốc tế viết tắt")]
        public string InterShortName { get; set; }
        
        [Display(Description = "Tên quốc tế đầy đủ")]
        public string InterFullName { get; set; }

    }
} 
