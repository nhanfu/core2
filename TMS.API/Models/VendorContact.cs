using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class VendorContact
    {
        public int Id { get; set; }
        public int? LocationId { get; set; }
        public int? BossId { get; set; }
        public string ContactName { get; set; }
        public string ContactPhoneNumber { get; set; }
        public string ContactUser { get; set; }
        public string Note { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual Vendor Boss { get; set; }
        public virtual Location Location { get; set; }
    }
}
