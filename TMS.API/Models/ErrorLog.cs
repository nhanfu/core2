using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class ErrorLog
    {
        public int Id { get; set; }
        public int? ErrorTypeId { get; set; }
        public int? CoordinationDetailId { get; set; }
        public int? AccountableUserId { get; set; }
        public int? AccountableVendorId { get; set; }
        public int? VendorId { get; set; }
        public decimal VendorAmount { get; set; }
        public string Note { get; set; }
        public decimal Credit { get; set; }
        public int? CurrencyId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual User AccountableUser { get; set; }
        public virtual Vendor AccountableVendor { get; set; }
    }
}
