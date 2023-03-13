using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Quotation
    {
        public Quotation()
        {
            QuotationExpense = new HashSet<QuotationExpense>();
            InverseParent = new HashSet<Quotation>();
        }

        public int Id { get; set; }
        public int? BranchId { get; set; }
        public int? TypeId { get; set; }
        public int? RouteId { get; set; }
        public int? ContainerTypeId { get; set; }
        public int? PackingId { get; set; }
        public int? BossId { get; set; }
        public int? LocationId { get; set; }
        public int? PolicyTypeId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitPrice1 { get; set; }
        public decimal UnitPrice2 { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Note { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? QuotationUpdateId { get; set; }
        public decimal UnitPrice3 { get; set; }
        public int? ParentId { get; set; }
        public bool IsParent { get; set; }
        public int? RegionId { get; set; }
        public int? DistrictId { get; set; }
        public int? ProvinceId { get; set; }
        public int? ExportListId { get; set; }

        public virtual Quotation Parent { get; set; }
        public virtual Location Location { get; set; }
        public virtual QuotationUpdate QuotationUpdate { get; set; }
        public virtual ICollection<Quotation> InverseParent { get; set; }
        public virtual ICollection<QuotationExpense> QuotationExpense { get; set; }
    }
}
