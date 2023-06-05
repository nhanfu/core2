using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class OriginAccountingVoucherDetails
    {
        public int Id { get; set; }
        public int? VoucherFormId { get; set; }
        public int? AccountingVoucherId { get; set; }
        public string Note { get; set; }
        public int? DebitAccountNo { get; set; }
        public int? CreditAccountNo { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? OriginalCurrency { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? Vat { get; set; }
        public decimal? VatReduction { get; set; }
        public decimal? TotalAmountBeforeTax { get; set; }
        public decimal? TotalAmountTax { get; set; }
        public decimal? TotalAmountAfterTax { get; set; }
        public decimal? TotalAmountBeforeTaxCy { get; set; }
        public decimal? TotalAmountTaxCy { get; set; }
        public decimal? TotalAmountAfterTaxCy { get; set; }
        public string SeriNo { get; set; }
        public string VATInvoiceNo { get; set; }
        public int? VATInvoiceID { get; set; }
        public DateTime? VATInvoiceDate { get; set; }
        public int? VATDebitAccountNo { get; set; }
        public int? VATCreditAccountNo { get; set; }
        public int? VATAccountNo { get; set; }
        public string Items { get; set; }
        public int? ItemId { get; set; }
        public int? TaxExpenseItemId { get; set; }
        public string TaxCode { get; set; }
        public int? TaxCustomerId { get; set; }
        public int? PartnerId { get; set; }
        public int? PartnerDebtId { get; set; }
        public int? ContainerTypeId { get; set; }
        public string ContainerNo { get; set; }
        public string SealNo { get; set; }
        public int? ShippingLine { get; set; }
        public int? VesselName { get; set; }
        public string Voyage { get; set; }
        public int? RouteId { get; set; }
        public int? DepartmentId { get; set; }
        public decimal? RealTotalAmountBeforeTax { get; set; }
        public decimal? RealTotalAmountTax { get; set; }
        public decimal? RealTotalAmountTaxPlus { get; set; }
        public decimal? RealTotalAmountAfterTax { get; set; }
        public int? RealDebitAccountNo { get; set; }
        public int? RealCreditAccountNo { get; set; }
        public decimal? VarianceTotalAmountBeforeTax { get; set; }
        public decimal? VarianceTotalAmountTax { get; set; }
        public int? RealVATAccountNo { get; set; }
        public string CalculationUnit { get; set; }
        public int? CurrencyId { get; set; }
        public string Unit { get; set; }
        public decimal? FreightPrice { get; set; }
        public int? CommodityId { get; set; }
        public decimal? StockQuantity { get; set; }
        public string LotNo { get; set; }
        public DateTime? LotDate { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual OriginAccountingVoucher AccountingVoucher { get; set; }
    }
}
