using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Vendor
    {
        public Vendor()
        {
            ErrorLog = new HashSet<ErrorLog>();
            User = new HashSet<User>();
            VendorContact = new HashSet<VendorContact>();
            VendorLocation = new HashSet<VendorLocation>();
            VendorService = new HashSet<VendorService>();
        }

        public int Id { get; set; }
        public bool IsContract { get; set; }
        public int? BranchId { get; set; }
        public int? UserId { get; set; }
        public int? RegionId { get; set; }
        public int? CommodityId { get; set; }
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
        public int? TypeId { get; set; }
        public int? TotalStar { get; set; }
        public int? TotalCountStar { get; set; }
        public int? TotalProduct { get; set; }
        public decimal ReturnRate { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public string NameReport { get; set; }
        public string AddressReport { get; set; }
        public string PhoneNumberReport { get; set; }
        public bool IsBought { get; set; }
        public int? CustomerTypeId { get; set; }
        public int? ParentId { get; set; }
        public int? RouteId { get; set; }
        public int? Length { get; set; }
        public string StaffName { get; set; }
        public int? ParentVendorId { get; set; }
        public string PositionName { get; set; }
        public string ClassifyName { get; set; }
        public string BankNo { get; set; }
        public string BankName { get; set; }
        public string CityName { get; set; }
        public int? DepartmentId { get; set; }
        public int? SaleId { get; set; }
        public string NameSys { get; set; }

        public virtual ICollection<ErrorLog> ErrorLog { get; set; }
        public virtual ICollection<User> User { get; set; }
        public virtual ICollection<VendorContact> VendorContact { get; set; }
        public virtual ICollection<VendorLocation> VendorLocation { get; set; }
        public virtual ICollection<VendorService> VendorService { get; set; }
    }
}
