using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class Staff
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public int GenderId { get; set; }
        public DateTime? DoB { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public int? DepartmentId { get; set; }
        public int? PositionId { get; set; }
        public int? BranchId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string AccountNum { get; set; }
        public string BankName { get; set; }
        public string Note { get; set; }
        public int? CountId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
