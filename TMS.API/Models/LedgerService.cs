using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class LedgerService
    {
        public int Id { get; set; }
        public int? InvoiceId { get; set; }
        public int? TargetInvoiceId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual Ledger Invoice { get; set; }
        public virtual Ledger TargetInvoice { get; set; }
    }
}
