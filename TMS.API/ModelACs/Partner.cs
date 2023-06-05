using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class Partner
    {
        public int Id { get; set; }
        public int? BranchId { get; set; }
        public string Code { get; set; }
        public string TaxCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public int? ParentId { get; set; }
        public string StaffName { get; set; }
        public string PositionName { get; set; }
        public string ClassifyName { get; set; }
        public string BankNo { get; set; }
        public string BankName { get; set; }
        public string CityName { get; set; }
        public int? DepartmentId { get; set; }
        public int? GroupId { get; set; }
        public int? GroupSaleId { get; set; }
        public int? YearCreated { get; set; }
        public string Notes { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
