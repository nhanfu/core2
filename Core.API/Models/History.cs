using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class History
    {
        public string Id { get; set; }
        public string TanentCode { get; set; }
        public string EntityId { get; set; }
        public string RecordId { get; set; }
        public string ReasonOfChange { get; set; }
        public string JsonHistory { get; set; }
        public string TextHistory { get; set; }
        public string ValueText { get; set; }
        public string OldValueText { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; } = DateTimeOffset.Now;
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}
