using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class VendorLocation
    {
        public int Id { get; set; }
        public int? VendorId { get; set; }
        public int? LocationId { get; set; }
        public int? TypeId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public string ContactName { get; set; }
        public string ContactName1 { get; set; }

        public virtual Location Location { get; set; }
        public virtual Vendor Vendor { get; set; }
    }
}
