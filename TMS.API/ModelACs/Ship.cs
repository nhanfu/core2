using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class Ship
    {
        public Ship()
        {
            Booking = new HashSet<Booking>();
        }

        public int Id { get; set; }
        public int? BrandId { get; set; }
        public string Code { get; set; }
        public string OldCode { get; set; }
        public int? BrandShipId { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? Click { get; set; }
        public int? Length { get; set; }
        public int? ParentShipId { get; set; }

        public virtual ICollection<Booking> Booking { get; set; }
    }
}
