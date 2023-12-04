namespace Core.Models
{
    public partial class FeaturePolicy
    {
        public string Id { get; set; }
        public string TenantCode { get; set; }
        public string FeatureId { get; set; }
        public string RoleId { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanDelete { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool CanDeactivate { get; set; }
        public int? LockDeleteAfterCreated { get; set; }
        public int? LockUpdateAfterCreated { get; set; }
        public string EntityId { get; set; }
        public string EntityName { get; set; }
        public string RecordId { get; set; }
        public string UserId { get; set; }
        public bool CanShare { get; set; }
        public string Desc { get; set; }
        public bool CanDeleteAll { get; set; }
        public bool CanWriteAll { get; set; }
        public bool CanAdd { get; set; }
        public bool CanWriteMeta { get; set; }
        public bool CanWriteMetaAll { get; set; }
        public bool CanDeactivateAll { get; set; }
        public bool CanExport { get; set; }
        public virtual Feature Feature { get; set; }
        public virtual Role Role { get; set; }
    }
}