namespace Core.Models;

public partial class Cluster
{
    public string Id { get; set; }
    public string TenantCode { get; set; }
    public string Env { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public string Scheme { get; set; }
    public string ClusterRole { get; set; }

    public bool Active { get; set; }
    public DateTimeOffset InsertedDate { get; set; }
    public string InsertedBy { get; set; }
    public DateTimeOffset? UpdatedDate { get; set; }
    public string UpdatedBy { get; set; }
}
