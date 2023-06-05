using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class ApprovalConfig
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public string Description { get; set; }
        public int? UserId { get; set; }
        public int? RoleId { get; set; }
        public string DataSource { get; set; }
        public int EntityId { get; set; }
        public int? WorkflowId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? RoleLevel { get; set; }
        public bool IsSameCostCenter { get; set; }
        public bool IsSupervisor { get; set; }
        public int? CostCenterId { get; set; }
    }
}
