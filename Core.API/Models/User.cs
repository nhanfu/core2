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

        public int Id { get; set; }
        public int VendorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string FullName { get; set; }
        public bool IsInternal { get; set; }
        public DateTime? DoB { get; set; }
        public string Ssn { get; set; }
        public string Passport { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string PhoneNumber { get; set; }
        public string PhoneNumber2 { get; set; }
        public int? NationalityId { get; set; }
        public int? ContractId { get; set; }
        public int? BankId { get; set; }
        public int? BankBranchId { get; set; }
        public string BankAccountNo { get; set; }
        public string BankAccountName { get; set; }
        public int? DepartmentId { get; set; }
        public string Avatar { get; set; }
        public int? SupervisorId { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
        public string Salt { get; set; }
        public int? LoginFailedCount { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? LastFailedLogin { get; set; }
        public string Email { get; set; }
        public int GenderId { get; set; }
        public string Recover { get; set; }
        public bool Reported { get; set; }
        public bool HasVerifiedEmail { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? CreatedRoleId { get; set; }
        public int? CostCenterId { get; set; }
        public int? TeamId { get; set; }
        public string ContactId { get; set; }
        public int? StateId { get; set; }

        public virtual User Supervisor { get; set; }
        public virtual Vendor Vendor { get; set; }
        public virtual ICollection<User> InverseSupervisor { get; set; }
        public virtual ICollection<UserRole> UserRole { get; set; }
    }
}
