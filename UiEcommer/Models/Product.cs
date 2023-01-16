using System;
using System.Collections.Generic;

namespace UiEcommer.Models
{
    public partial class Product
    {
        public Product()
        {
            PromoDetail = new HashSet<PromoDetail>();
        }

        public int Id { get; set; }
        public string Path { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int? AttributeId { get; set; }
        public int? Attribute2Id { get; set; }
        public string Name { get; set; }
        public string NameWebsite { get; set; }
        public int? Order { get; set; }
        public string Description { get; set; }
        public string MetaTitle { get; set; }
        public string MetaKeywords { get; set; }
        public string MetaDescription { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitPriceImport { get; set; }
        public decimal WholesaleUnitPrice { get; set; }
        public decimal OldUnitPrice { get; set; }
        public decimal Vat { get; set; }
        public decimal Promo { get; set; }
        public string PromoContent { get; set; }
        public decimal Weight { get; set; }
        public string Unit { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public int? CountryId { get; set; }
        public int? VendorId { get; set; }
        public int? TypeId { get; set; }
        public int? StatusId { get; set; }
        public int? ParentId { get; set; }
        public string WarrantyPhone { get; set; }
        public string WarrantyAddress { get; set; }
        public int? Warranty { get; set; }
        public string WarrantyContent { get; set; }
        public string Link { get; set; }
        public string Avatar { get; set; }
        public string Content { get; set; }
        public decimal QuantityRemain { get; set; }
        public decimal QuantityBought { get; set; }
        public decimal QuantityImport { get; set; }
        public int ClickCount { get; set; }
        public int StarCount { get; set; }
        public decimal PromoTranfer { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual Category Category { get; set; }
        public virtual ICollection<PromoDetail> PromoDetail { get; set; }
    }
}
