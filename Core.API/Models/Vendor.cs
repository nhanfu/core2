using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class Vendor
    {
        public Vendor()
        {
            InverseParent = new HashSet<Vendor>();
            InverseTenant = new HashSet<Vendor>();
            User = new HashSet<User>();
            VendorBranch = new HashSet<VendorBranch>();
        }

        public int Id { get; set; }
        public int? VendorTypeId { get; set; }
        public string Description { get; set; }
        public string Avatar { get; set; }
        public int? CustomerGroupId { get; set; }
        public int SaleId { get; set; }
        public int? UserId { get; set; }
        public int? CustomerStateId { get; set; }
        public DateTime? LastContactDate { get; set; }
        public bool IsSelf { get; set; }
        public bool IsInternal { get; set; }
        public string PhoneNumber { get; set; }
        public string PhoneNumber2 { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string Note { get; set; }
        public string Email { get; set; }
        public string Fax { get; set; }
        public string Skype { get; set; }
        public string Zalo { get; set; }
        public string Viber { get; set; }
        public string OtherContact { get; set; }
        public string TaxCode { get; set; }
        public string CompanyLocalShortName { get; set; }
        public string CompanyInterShortName { get; set; }
        public string CompanyLocalFullName { get; set; }
        public string CompanyInterFullName { get; set; }
        public string CompanyLocalAddress { get; set; }
        public string CompanyInterAddress { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? RoleId { get; set; }
        public bool IsTenant { get; set; }
        public int? ParentId { get; set; }
        public int? TenantId { get; set; }
        public string ConStr { get; set; }
        public int? NationalityId { get; set; }
        public int? SeqKey { get; set; }
        public bool DebtCountFinished { get; set; }
        public int? DebtDate { get; set; }
        public int? DebtDateTypeId { get; set; }
        public int? DebtExportDate { get; set; }
        public int? OwnerId { get; set; }
        public int? CollectOnBehalfDay { get; set; }
        public string GlobalId { get; set; }
        public string PartnerId { get; set; }
        public int? StateId { get; set; }
        public bool IsSeft { get; set; }

        public virtual Vendor Parent { get; set; }
        public virtual Vendor Tenant { get; set; }
        public virtual ICollection<Vendor> InverseParent { get; set; }
        public virtual ICollection<Vendor> InverseTenant { get; set; }
        public virtual ICollection<User> User { get; set; }
        public virtual ICollection<VendorBranch> VendorBranch { get; set; }
    }
}
