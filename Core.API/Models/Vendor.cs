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

        public string Id { get; set; }
        public string VendorTypeId { get; set; }
        public string Description { get; set; }
        public string Avatar { get; set; }
        public string CustomerGroupId { get; set; }
        public string SaleId { get; set; }
        public string UserId { get; set; }
        public string CustomerStateId { get; set; }
        public DateTimeOffset? LastContactDate { get; set; }
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
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string RoleId { get; set; }
        public bool IsTenant { get; set; }
        public string ParentId { get; set; }
        public string TenantId { get; set; }
        public string ConStr { get; set; }
        public string NationalityId { get; set; }
        public string SeqKey { get; set; }
        public bool DebtCountFinished { get; set; }
        public string DebtDate { get; set; }
        public string DebtDateTypeId { get; set; }
        public string DebtExportDate { get; set; }
        public string OwnerId { get; set; }
        public string CollectOnBehalfDay { get; set; }
        public string GlobalId { get; set; }
        public string PartnerId { get; set; }
        public string StateId { get; set; }
        public DateTimeOffset? LastOrderState { get; set; }
        public bool IsSeft { get; set; }

        public virtual Vendor Parent { get; set; }
        public virtual Vendor Tenant { get; set; }
        public virtual ICollection<Vendor> InverseParent { get; set; }
        public virtual ICollection<Vendor> InverseTenant { get; set; }
        public virtual ICollection<User> User { get; set; }
        public virtual ICollection<VendorBranch> VendorBranch { get; set; }
    }
}
