using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Vendor
    {
        public string Id { get; set; }

        public string TenantCode { get; set; }

        public bool IsContract { get; set; }

        public string BranchId { get; set; }

        public string UserId { get; set; }

        public string RegionId { get; set; }

        public string CommodityId { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string TaxCode { get; set; }

        public string PhoneNumber { get; set; }

        public string Email { get; set; }

        public string CompanyName { get; set; }

        public string Address { get; set; }

        public string Description { get; set; }

        public int? TotalFollow { get; set; }

        public string Logo { get; set; }

        public string TypeId { get; set; }

        public int? TotalStar { get; set; }

        public int? TotalCountStar { get; set; }

        public int? TotalProduct { get; set; }

        public decimal ReturnRate { get; set; }

        public bool Active { get; set; }

        public DateTimeOffset InsertedDate { get; set; }

        public string InsertedBy { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public string NameReport { get; set; }

        public string AddressReport { get; set; }

        public string PhoneNumberReport { get; set; }

        public bool IsBought { get; set; }

        public string CustomerTypeId { get; set; }

        public string ParentId { get; set; }

        public string RouteId { get; set; }

        public int? Length { get; set; }

        public string StaffName { get; set; }

        public string ParentVendorId { get; set; }

        public string PositionName { get; set; }

        public string ClassifyName { get; set; }

        public string BankNo { get; set; }

        public string BankName { get; set; }

        public string CityName { get; set; }

        public string DepartmentId { get; set; }

        public string SaleId { get; set; }

        public string NameSys { get; set; }

        public string StateId { get; set; }

        public DateTime? LastOrderState { get; set; }

        public bool IsSelf { get; set; }

        public string GroupId { get; set; }

        public string GroupSaleId { get; set; }

        public int? YearCreated { get; set; }

        public string Notes { get; set; }

        public string CountId { get; set; }

        public virtual ICollection<User> User { get; set; } = new List<User>();
    }
}