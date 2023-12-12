using Core.Extensions;

namespace Core.ViewModels
{
    public class LoginVM
    {
        public string Env { get; set; }
        public string CompanyName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool AutoSignIn { get; set; }
        public string ConnStr { get; internal set; }
        public string ConnKey { get; internal set; } = Utils.ConnKey;
    }
}
