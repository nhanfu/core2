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

        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string VendorId { get; set; }
        public string RegionId { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool Active { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TaxCode { get; set; }
        public string ParentId { get; set; }
        public string Path { get; set; }
        public string Level { get; set; }

        public virtual VendorBranch Parent { get; set; }
        public virtual Vendor Vendor { get; set; }
        public virtual ICollection<VendorBranch> InverseParent { get; set; }
    }
}
