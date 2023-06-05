using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class AccountingVoucherDetailsSum
    {
        public int Id { get; set; }
        public int? VoucherFormId { get; set; }
        public int? AccountingVoucherId { get; set; }
        public int? PartnerId { get; set; }
        public int? VATInvoiceID { get; set; }
        public string Items { get; set; }
        public string VATInvoiceNo { get; set; }
        public decimal? Amount { get; set; }
        public int? DebitAccountNo { get; set; }
        public int? CreditAccountNo { get; set; }
        public string VoucherNo { get; set; }
        public DateTime? OriginRefDate { get; set; }
        public DateTime? VATInvoiceDate { get; set; }
        public string Note { get; set; }
        public int? OriginRefCreatedBy { get; set; }
        public decimal? AriseDebit { get; set; }
        public decimal? AriseCredit { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual AccountingVoucher AccountingVoucher { get; set; }
    }
}
