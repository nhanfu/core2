namespace Core.Models
{
    public partial class FeaturePolicy
    {
        public string Id { get; set; }
        public string FeatureId { get; set; }
        public string RoleId { get; set; }
        public bool CanRead { get; set; }
        public bool CanReadAll { get; set; }
        public bool CanWrite { get; set; }
        public bool CanWriteAll { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDeleteAll { get; set; }
        public bool CanCopy { get; set; }
        public bool CanCopyAll { get; set; }
        public bool CanDeactivate { get; set; }
        public bool CanDeactivateAll { get; set; }
        public string RecordId { get; set; }
        public string UserId { get; set; }
        public string TableName { get; set; }
        public bool Active { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}