using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class SettingPolicy
    {
        public SettingPolicy()
        {
            SettingPolicyDetail = new HashSet<SettingPolicyDetail>();
        }

        public int Id { get; set; }
        public int? BrandShipId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? PolicyId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public decimal? UnitPrice { get; set; }
        public int? TypeId { get; set; }
        public int? TransportationTypeId { get; set; }
        public string Name { get; set; }
        public int? ExportListId { get; set; }
        public bool CheckAll { get; set; }

        public virtual ICollection<SettingPolicyDetail> SettingPolicyDetail { get; set; }
    }
}
