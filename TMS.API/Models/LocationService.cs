using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class LocationService
    {
        public int Id { get; set; }
        public int? LocationId { get; set; }
        public int? ServiceId { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual Location Location { get; set; }
    }
}
