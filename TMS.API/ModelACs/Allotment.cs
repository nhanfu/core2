using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class Allotment
    {
        public Allotment()
        {
            Expense = new HashSet<Expense>();
        }

        public int Id { get; set; }
        public int? ExpenseTypeId { get; set; }
        public bool IsFull { get; set; }
        public decimal UnitPrice { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? BranchId { get; set; }
        public bool IsVat { get; set; }
        public bool IsCollectOnBehaft { get; set; }
        public string Notes { get; set; }

        public virtual ICollection<Expense> Expense { get; set; }
    }
}
