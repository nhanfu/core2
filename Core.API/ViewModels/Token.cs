using Core.Models;
using System;
using System.Collections.Generic;

namespace Core.ViewModels
{
    [Serializable]
    public partial class Token
    {

        public int UserId { get; set; }
        public int? CostCenterId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string Avatar { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExp { get; set; }
        public DateTime RefreshTokenExp { get; set; }
        public string HashPassword { get; set; }
        public string Recovery { get; set; }
        public string SysName { get; set; } = "TMS";
        public string TenantCode { get; set; }
        public Vendor Vendor { get; set; }
        public List<int> RoleIds { get; set; }
        public List<string> RoleNames { get; set; }
        public List<int> AllRoleIds { get; set; }
        public List<int> CenterIds { get; set; }
        public string Ssn { get; set; }
        public string PhoneNumber { get; set; }
        public int? TeamId { get; set; }
        public string PartnerId { get; set; }
        public int? RegionId { get; set; }
        public object Additional { get; set; }
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
