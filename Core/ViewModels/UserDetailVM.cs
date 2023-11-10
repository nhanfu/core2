using Core.Models;

namespace Core.ViewModels
{
    public class UserProfileVM : User
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmedPassword { get; set; }
    }
}
