namespace Core.ViewModels
{
    public class LoginVM
    {
        public string Env { get; set; }
        public string CompanyName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public string System { get; set; }
        public bool AutoSignIn { get; set; }
        public string RecoveryToken { get; set; }
    }
}
