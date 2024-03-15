using Core.Clients;

namespace Core.ViewModels
{
    public class LoginVM
    {
        public string CompanyName { get; set; } = Client.Tenant;
        public string ConnKey { get; set; } = Client.MetaConn;
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool AutoSignIn { get; set; }
        public string RecoveryToken { get; set; }
        public string Env { get; set; } = Client.Env;
    }
}
