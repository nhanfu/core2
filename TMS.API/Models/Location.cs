using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Location
    {
        public Location()
        {
            LocationService = new HashSet<LocationService>();
            Quotation = new HashSet<Quotation>();
            VendorContact = new HashSet<VendorContact>();
            VendorLocation = new HashSet<VendorLocation>();
        }

        public int Id { get; set; }
        public int? BranchId { get; set; }
        public int? RegionId { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DescriptionEn { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? Click { get; set; }
        public int? Length { get; set; }
        public string Description1 { get; set; }
        public string Description2 { get; set; }
        public string Description3 { get; set; }
        public string Description4 { get; set; }
        public string Description5 { get; set; }
        public bool? IsUse { get; set; }
        public int? ParentLocationId { get; set; }

        public virtual ICollection<LocationService> LocationService { get; set; }
        public virtual ICollection<Quotation> Quotation { get; set; }
        public virtual ICollection<VendorContact> VendorContact { get; set; }
        public virtual ICollection<VendorLocation> VendorLocation { get; set; }
    }
}
