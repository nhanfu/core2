using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class FeaturePolicy
    {
        public int Id { get; set; }
        public int? FeatureId { get; set; }
        public int? RoleId { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanDelete { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public bool CanDeactivate { get; set; }
        public int? LockDeleteAfterCreated { get; set; }
        public int? LockUpdateAfterCreated { get; set; }
        public int? EntityId { get; set; }
        public int RecordId { get; set; }
        public int? UserId { get; set; }
        public bool CanShare { get; set; }
        public string Desc { get; set; }
        public bool CanDeleteAll { get; set; }
        public bool CanWriteAll { get; set; }

        public virtual Feature Feature { get; set; }
        public virtual Role Role { get; set; }
    }
}
