using Core.Extensions;
using Core.Models;

namespace Core.ViewModels
{
    [Serializable]
    public partial class Token
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string DepartmentId { get; set; }
        public string Code { get; set; }
        public string PositionId { get; set; }
        public string Address { get; set; }
        public string Avatar { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExp { get; set; }
        public DateTime RefreshTokenExp { get; set; }
        public Partner Vendor { get; set; }
        public List<string> RoleIds { get; set; }
        public List<string> RoleNames { get; set; }
        public List<string> CenterIds { get; set; }
        public string Ssn { get; set; }
        public string PhoneNumber { get; set; }
        public string TeamId { get; set; }
        public string PartnerId { get; set; }
        public string RegionId { get; set; }
        public DateTime SigninDate { get; set; }
        public string TenantCode { get; set; }
        public string Env { get; set; }
        public string ConnKey { get; set; }
    }

    public class RefreshVM
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public class CreateHtmlVM
    {
        public string ComId { get; set; }
        public string FileName { get; set; }
        public string PathTemplate { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}
