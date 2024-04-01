namespace Core.Models
{
    public partial class Resource
    {
        public string Id { get; set; }
        public string TenantCode { get; set; }
        public string Path { get; set; }
        public string  Content { get; set; }
        public string ContentType { get; set; }
        public bool Annonymous { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}