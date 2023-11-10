using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class User
    {
        public string Id { get; set; }

        public string TenantCode { get; set; }


        public string BranchId { get; set; }

        public string VendorId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string MiddleName { get; set; }

        public string FullName { get; set; }

        public DateTimeOffset? DoB { get; set; }

        public string Ssn { get; set; }

        public string Address { get; set; }

        public string PhoneNumber { get; set; }

        public string NationalityId { get; set; }

        public string Avatar { get; set; }

        public string Password { get; set; }

        public string UserName { get; set; }

        public string Salt { get; set; }

        public int? LoginFailedCount { get; set; }

        public DateTimeOffset? LastLogin { get; set; }

        public DateTimeOffset? LastFailedLogin { get; set; }

        public string Email { get; set; }

        public string GenderId { get; set; }

        public string Recover { get; set; }

        public bool HasVerifiedEmail { get; set; }

        public bool Active { get; set; }

        public DateTimeOffset InsertedDate { get; set; }

        public string InsertedBy { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public string CreatedRoleId { get; set; }

        public string RouteId { get; set; }

        public string ContactId { get; set; }

        public int? Length { get; set; }

        public string ActUserId { get; set; }

        public string StateId { get; set; }

        public virtual ICollection<UserRole> UserRole { get; set; } = new List<UserRole>();

        public virtual Vendor Vendor { get; set; }
    }
}