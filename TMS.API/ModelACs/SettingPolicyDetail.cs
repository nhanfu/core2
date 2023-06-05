using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class SettingPolicyDetail
    {
        public int Id { get; set; }
        public int? SettingPolicyId { get; set; }
        public int? ComponentId { get; set; }
        public int? OperatorId { get; set; }
        public string Value { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual SettingPolicy SettingPolicy { get; set; }
    }
}
