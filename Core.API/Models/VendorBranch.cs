using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class VendorBranch
    {
        public VendorBranch()
        {
            InverseParent = new HashSet<VendorBranch>();
        }

        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public int? VendorId { get; set; }
        public int? RegionId { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime InsertedDate { get; set; }
        public int? InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public bool Active { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TaxCode { get; set; }
        public int? ParentId { get; set; }
        public string Path { get; set; }
        public int? Level { get; set; }

        public virtual VendorBranch Parent { get; set; }
        public virtual Vendor Vendor { get; set; }
        public virtual ICollection<VendorBranch> InverseParent { get; set; }
    }
}
