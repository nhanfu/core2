using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class ApprovalConfig
    {
        public string Id { get; set; }
        public int Level { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
        public string RoleId { get; set; }
        public string DataSource { get; set; }
        public string EntityId { get; set; }
        public string WorkflowId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string RoleLevel { get; set; }
        public bool IsSameCostCenter { get; set; }
        public bool IsSupervisor { get; set; }
        public string CostCenterId { get; set; }
    }
}
