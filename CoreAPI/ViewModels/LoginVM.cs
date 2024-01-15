using Core.Extensions;
using System.Text.Json.Serialization;

namespace Core.ViewModels
{
    public class LoginVM
    {
        public string Env { get; set; }
        public string CompanyName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool AutoSignIn { get; set; }
        [JsonIgnore]
        public string CachedConnStr { get; internal set; }
        public string ConnKey { get; internal set; } = Utils.ConnKey;
    }
}
