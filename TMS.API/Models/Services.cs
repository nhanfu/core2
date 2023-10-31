using System;
using System.Collections.Generic;

namespace TMS.API.Models;

public partial class Services
{
    public string Id { get; set; }

    public string TenantCode { get; set; }

    public string ComId { get; set; }

    public string Address { get; set; }

    public string CmdType { get; set; }

    public string Content { get; set; }

    public string Env { get; set; }

    public string Path { get; set; }

    public string VendorId { get; set; }

    public string VendorName { get; set; }

    public string ResHeaders { get; set; }

    public bool Active { get; set; }

    public DateTimeOffset InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
