using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class PaymentHistory
    {
        public int Id { get; set; }
        public int? VATInvoiceId { get; set; }
        public int? VATInvoiceDetailsId { get; set; }
        public int? OriginAccountingVoucherId { get; set; }
        public int? OriginAccountingVoucherDetailsId { get; set; }
        public decimal? TotalAmountOwed { get; set; }
        public decimal? TotalAmountPaid { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
