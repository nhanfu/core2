using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class QuotationUpdate
    {
        public QuotationUpdate()
        {
            Quotation = new HashSet<Quotation>();
        }

        public int Id { get; set; }
        public bool IsAdd { get; set; }
        public int? ContainerId { get; set; }
        public DateTime? StartDate { get; set; }
        public int? RegionId { get; set; }
        public decimal UnitPrice { get; set; }
        public int? TypeId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual ICollection<Quotation> Quotation { get; set; }
    }
}
