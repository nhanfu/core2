namespace Core.Models;

public partial class Dictionary
{
    public string Id { get; set; }

    public string LangCode { get; set; }

    public string Key { get; set; }

    public string Value { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTime InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string TenantCode { get; set; }
}
