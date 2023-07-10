using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class AccountingVoucherDetailsService
    {
        public int Id { get; set; }
        public int? AccountingVoucherDetailsId { get; set; }
        public int? TransportationId { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int? BossId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual OriginAccountingVoucherDetails AccountingVoucherDetails { get; set; }
    }
}
