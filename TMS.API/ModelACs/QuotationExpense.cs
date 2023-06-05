using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class QuotationExpense
    {
        public QuotationExpense()
        {
            QuotationExpenseRoute = new HashSet<QuotationExpenseRoute>();
        }

        public int Id { get; set; }
        public int? BranchId { get; set; }
        public int? QuotationId { get; set; }
        public int? BrandShipId { get; set; }
        public int? ExpenseTypeId { get; set; }
        public string VSC { get; set; }
        public int? ContainerTypeId { get; set; }
        public decimal VS20UnitPrice { get; set; }
        public decimal VS40UnitPrice { get; set; }
        public decimal DOUnitPrice { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? StartDate { get; set; }
        public int? RouteId { get; set; }

        public virtual Quotation Quotation { get; set; }
        public virtual ICollection<QuotationExpenseRoute> QuotationExpenseRoute { get; set; }
    }
}
