using System;
using System.Collections.Generic;

namespace TMS.API.Models;

public partial class UserLogin
{
    public string Id { get; set; }

    public string TenantCode { get; set; }

    public string UserId { get; set; }

    public string IpAddress { get; set; }

    public DateTimeOffset? SignInDate { get; set; }

    public string RefreshToken { get; set; }

    public DateTimeOffset? ExpiredDate { get; set; }
}
