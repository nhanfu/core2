using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class TaskNotification
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string EntityId { get; set; }
        public string RecordId { get; set; }
        public DateTimeOffset Deadline { get; set; }
        public string StatusId { get; set; }
        public string Attachment { get; set; }
        public string AssignedId { get; set; }
        public string RoleId { get; set; }
        public decimal TimeConsumed { get; set; }
        public decimal TimeRemained { get; set; }
        public decimal Progress { get; set; }
        public double? RemindBefore { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string Badge { get; set; }

        public virtual Entity Entity { get; set; }
        public virtual Role Role { get; set; }
    }
}
