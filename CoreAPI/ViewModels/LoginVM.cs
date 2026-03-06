using Core.Extensions;
using System.Text.Json.Serialization;

namespace Core.ViewModels
{
    public class LoginVM
    {
        public string TenantCode { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool AutoSignIn { get; set; }
    }
}
