using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class VendorService
    {
        public int Id { get; set; }
        public int? VendorId { get; set; }
        public int? ServiceId { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual Vendor Vendor { get; set; }
    }
}
