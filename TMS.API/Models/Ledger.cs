using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Ledger
    {
        public Ledger()
        {
            LedgerServiceInvoice = new HashSet<LedgerService>();
            LedgerServiceTargetInvoice = new HashSet<LedgerService>();
        }

        public int Id { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public decimal? OriginDebit { get; set; }
        public decimal? OriginCredit { get; set; }
        public int? CurrencyId { get; set; }
        public decimal? ExchangeRate { get; set; }
        public int AccountTypeId { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public int? InvoiceFormId { get; set; }
        public int? TypeId { get; set; }
        public int? EntityId { get; set; }
        public int? RecordId { get; set; }
        public int? FieldId { get; set; }
        public string Note { get; set; }
        public bool Lock { get; set; }
        public int? CostCenterId { get; set; }
        public int? ParentId { get; set; }
        public string Attachments { get; set; }
        public int? ObjectId { get; set; }
        public int? ObjectTypeId { get; set; }
        public int? DebitAccId { get; set; }
        public int? DebitAccNo { get; set; }
        public int? CreditAccId { get; set; }
        public int? CreditAccNo { get; set; }
        public decimal? OriginPriceBeforeTax { get; set; }
        public decimal? PriceBeforeTax { get; set; }
        public decimal? OriginPriceAfterTax { get; set; }
        public decimal? PriceAfterTax { get; set; }
        public string SerialNo { get; set; }
        public int? Quantity { get; set; }
        public decimal? OriginUnitPrice { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Vat { get; set; }
        public decimal? OriginVatAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public string Taxcode { get; set; }
        public int? ItemsId { get; set; }
        public int? TaxExpenseItemsId { get; set; }
        public int? RouteId { get; set; }
        public int? DebitAccVatId { get; set; }
        public int? DebitAccVatNo { get; set; }
        public int? CreditAccVatId { get; set; }
        public int? CreditAccVatNo { get; set; }
        public string Items { get; set; }
        public string TaxVendor { get; set; }
        public int? BrandShipId { get; set; }
        public int? ShipId { get; set; }
        public int? ContainerTypeId { get; set; }
        public string Trip { get; set; }
        public string BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public int? DepartmentId { get; set; }
        public bool IsMakeUp { get; set; }
        public decimal? OriginMakeUpPrice { get; set; }
        public decimal? MakeUpPrice { get; set; }
        public decimal? VatMakeUp { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Address { get; set; }
        public int? VendorId { get; set; }
        public bool IsAllPaid { get; set; }
        public int? CaseId { get; set; }
        public string BankNo { get; set; }
        public string BankUserName { get; set; }
        public int? BankId { get; set; }
        public decimal? OriginTotalPrice { get; set; }
        public decimal? OriginRealTotalPrice { get; set; }
        public decimal? OriginReturnTotalPrice { get; set; }
        public string BankName { get; set; }
        public string Attach { get; set; }
        public int? UserId { get; set; }
        public int? BetDeadline { get; set; }
        public int? ObjectHasId { get; set; }
        public string ContainerNo { get; set; }
        public string SealNo { get; set; }
        public int? FixedAssetsId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual ICollection<LedgerService> LedgerServiceInvoice { get; set; }
        public virtual ICollection<LedgerService> LedgerServiceTargetInvoice { get; set; }
    }
}
