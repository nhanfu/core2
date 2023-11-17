using System;

namespace Core.Models
{
    public partial class UserSetting
    {
        public string Id { get; set; }
        public string TenantCode { get; set; }
        public string RoleId { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string ParentId { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}
