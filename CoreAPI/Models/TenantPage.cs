namespace Core.Models
{
    public partial class TenantPage
    {
        public string Id { get; set; }

        public string TenantEnvId { get; set; }

        public string Area { get; set; }
        
        public string  Template { get; set; }

        public bool Active { get; set; }

        public DateTimeOffset InsertedDate { get; set; }

        public string InsertedBy { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; }
        
        public string SvcId { get; set; }
    }
}