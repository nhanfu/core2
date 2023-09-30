using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class User
    {
        public User()
        {
            InverseSupervisor = new HashSet<User>();
            UserRole = new HashSet<UserRole>();
        }

        public string Id { get; set; }
        public string VendorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string FullName { get; set; }
        public bool IsInternal { get; set; }
        public DateTimeOffset? DoB { get; set; }
        public string Ssn { get; set; }
        public string Passport { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string PhoneNumber { get; set; }
        public string PhoneNumber2 { get; set; }
        public string NationalityId { get; set; }
        public string ContractId { get; set; }
        public string BankId { get; set; }
        public string BankBranchId { get; set; }
        public string BankAccountNo { get; set; }
        public string BankAccountName { get; set; }
        public string DepartmentId { get; set; }
        public string Avatar { get; set; }
        public string SupervisorId { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
        public string Salt { get; set; }
        public string LoginFailedCount { get; set; }
        public DateTimeOffset? LastLogin { get; set; }
        public DateTimeOffset? LastFailedLogin { get; set; }
        public string Email { get; set; }
        public string GenderId { get; set; }
        public string Recover { get; set; }
        public bool Reported { get; set; }
        public bool HasVerifiedEmail { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string CreatedRoleId { get; set; }
        public string CostCenterId { get; set; }
        public string TeamId { get; set; }
        public string ContactId { get; set; }
        public string StateId { get; set; }

        public virtual User Supervisor { get; set; }
        public virtual Vendor Vendor { get; set; }
        public virtual ICollection<User> InverseSupervisor { get; set; }
        public virtual ICollection<UserRole> UserRole { get; set; }
    }
}
