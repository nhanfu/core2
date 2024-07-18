namespace Core.Models
{
    public partial class Tenant
    {
        public string Id { get; set; }
        public string TenantCode { get; set; }
        public string SubTenant { get; set; }
        public string  Env { get; set; }
        public string  ConnKey { get; set; }
        public string ConnStr { get; set; }
        public string Area { get; set; }
        public string Template { get; set; }
        public string SvcId { get; set; }
        public string Css { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}