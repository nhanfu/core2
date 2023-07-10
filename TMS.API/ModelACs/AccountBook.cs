using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class AccountBook
    {
        public int Id { get; set; }
        public DateTime? OriginRefDate { get; set; }
        public string VoucherNo { get; set; }
        public string Note { get; set; }
        public int? AccountNo { get; set; }
        public int? PartnerId { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? DebitBalance { get; set; }
        public decimal? CreditBalance { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
