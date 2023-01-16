using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class Approvement
    {
        public int Id { get; set; }
        public string ReasonOfChange { get; set; }
        public int? EntityId { get; set; }
        public string LevelName { get; set; }
        public int? RecordId { get; set; }
        public int? StatusId { get; set; }
        public decimal Amount { get; set; }
        public int UserApproveId { get; set; }
        public bool IsEnd { get; set; }
        public int? NextLevel { get; set; }
        public int CurrentLevel { get; set; }
        public bool Approved { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? RejectBy { get; set; }
        public DateTime? RejectDate { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
