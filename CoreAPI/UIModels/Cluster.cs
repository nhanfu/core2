using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Cluster
{
    public string Id { get; set; }

    public byte[] TenantCode { get; set; }

    public string Env { get; set; }

    public string Host { get; set; }

    public int? Port { get; set; }

    public string Scheme { get; set; }

    public string ClusterRole { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
