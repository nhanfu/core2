using System;
using System.Collections.Generic;

namespace Core.Models;

public partial class Services
{
    public string Id { get; set; }

    public string System { get; set; }

    public string TenantCode { get; set; }

    public string ComId { get; set; }

    public string Action { get; set; }

    public string Content { get; set; }

    public string Env { get; set; }

    public string ConnKey { get; set; }

    public string RoleIds { get; set; }

    public bool IsPublicInTenant { get; set; }

    public bool Anonymous { get; set; }

    public bool Active { get; set; }

    public DateTimeOffset InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
