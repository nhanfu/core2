using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class TaskNotification
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? EntityId { get; set; }
        public int? RecordId { get; set; }
        public DateTime Deadline { get; set; }
        public int? StatusId { get; set; }
        public string Attachment { get; set; }
        public int? AssignedId { get; set; }
        public int? RoleId { get; set; }
        public decimal TimeConsumed { get; set; }
        public decimal TimeRemained { get; set; }
        public decimal Progress { get; set; }
        public double? RemindBefore { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? Badge { get; set; }

        public virtual Entity Entity { get; set; }
        public virtual Role Role { get; set; }
    }
}
