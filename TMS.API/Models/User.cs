using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class User
    {
        public User()
        {
            ErrorLog = new HashSet<ErrorLog>();
            UserRole = new HashSet<UserRole>();
            UserRoute = new HashSet<UserRoute>();
        }

        public int Id { get; set; }
        public int? BranchId { get; set; }
        public int VendorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string FullName { get; set; }
        public DateTime? DoB { get; set; }
        public string Ssn { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public int? NationalityId { get; set; }
        public string Avatar { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
        public string Salt { get; set; }
        public int? LoginFailedCount { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? LastFailedLogin { get; set; }
        public string Email { get; set; }
        public int GenderId { get; set; }
        public string Recover { get; set; }
        public bool HasVerifiedEmail { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? CreatedRoleId { get; set; }
        public int? RouteId { get; set; }
        public string ContactId { get; set; }
        public int? Length { get; set; }
        public int? ActUserId { get; set; }

        public virtual Vendor Vendor { get; set; }
        public virtual ICollection<ErrorLog> ErrorLog { get; set; }
        public virtual ICollection<UserRole> UserRole { get; set; }
        public virtual ICollection<UserRoute> UserRoute { get; set; }
    }
}
