using Core.Models;

namespace Core.ViewModels
{
    [Serializable]
    public partial class Token
    {

        public string UserId { get; set; }
        public string CostCenterId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string Avatar { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTimeOffset AccessTokenExp { get; set; }
        public DateTimeOffset RefreshTokenExp { get; set; }
        public string TenantCode { get; set; }
        public Vendor Vendor { get; set; }
        public List<string> RoleIds { get; set; }
        public List<string> RoleNames { get; set; }
        public List<string> CenterIds { get; set; }
        public string Ssn { get; set; }
        public string PhoneNumber { get; set; }
        public string TeamId { get; set; }
        public string PartnerId { get; set; }
        public string RegionId { get; set; }
        public DateTimeOffset SigninDate { get; set; }
        public string ConnKey { get; internal set; }
    }

    [Serializable]
    public class GPS
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    public class RefreshVM
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
