using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class History
    {
        public long Id { get; set; }
        public string TanentCode { get; set; }
        public int EntityId { get; set; }
        public int RecordId { get; set; }
        public string ReasonOfChange { get; set; }
        public string JsonHistory { get; set; }
        public string TextHistory { get; set; }
        public string ValueText { get; set; }
        public string OldValueText { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
