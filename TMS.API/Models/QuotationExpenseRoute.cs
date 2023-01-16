using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class QuotationExpenseRoute
    {
        public int Id { get; set; }
        public int? QuotationExpenseId { get; set; }
        public int? Route { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual QuotationExpense QuotationExpense { get; set; }
    }
}
