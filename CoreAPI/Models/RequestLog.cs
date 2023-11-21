namespace Core.Models;

public partial class RequestLog
{
    public string Id { get; set; }

    public string TenantCode { get; set; }

    public string RequestBody { get; set; }

    public string HttpMethod { get; set; }

    public string Path { get; set; }

    public string ResponseBody { get; set; }

    public int? StatusCode { get; set; }

    public bool Active { get; set; }

    public DateTimeOffset InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
