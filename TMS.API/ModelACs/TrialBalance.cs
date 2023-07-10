using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class TrialBalance
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public int? Level { get; set; }
        public int? VoucherNoId { get; set; }
        public decimal? DebitOpeningBalance { get; set; }
        public decimal? CreditOpeningBalance { get; set; }
        public decimal? DebitArisingAmount { get; set; }
        public decimal? CreditArisingAmount { get; set; }
        public decimal? DebitClosingBalance { get; set; }
        public decimal? CreditClosingBalance { get; set; }
        public string VoucherNoIds { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
