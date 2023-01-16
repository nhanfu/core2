using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class InsurancePremium
    {
        public int Id { get; set; }
        public int? RouteId { get; set; }
        public int? VendorId { get; set; }
        public int? CommodityTypeId { get; set; }
        public bool? IsWet { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
