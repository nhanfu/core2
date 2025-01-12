using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ShipmentInvoiceDetail
{
    public string Id { get; set; }

    public int? TypeId { get; set; }

    public string ShipmentInvoiceId { get; set; }

    public string PaymentRequestId { get; set; }

    public string ShipmentId { get; set; }

    public string VendorId { get; set; }

    public string DescriptionId { get; set; }

    public decimal? TotalAmountTax { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? AmountTax { get; set; }

    public decimal? Amount { get; set; }

    public decimal? Quantity { get; set; }

    public string UnitId { get; set; }

    public decimal? Vat { get; set; }

    public string CurrencyId { get; set; }

    public string CurrencyCode { get; set; }

    public decimal? Tax { get; set; }

    public string Notes { get; set; }

    public string Docs { get; set; }

    public bool IsObh { get; set; }

    public string ObhId { get; set; }

    public bool IsNoDocs { get; set; }

    public decimal? ExchangeRateVND { get; set; }

    public decimal? ExchangeRateUSD { get; set; }

    public decimal? ExchangeRateINV { get; set; }

    public decimal? ExSaleUSD { get; set; }

    public decimal? ExSaleVND { get; set; }

    public decimal? ExProfitUSD { get; set; }

    public decimal? ExProfitVND { get; set; }

    public bool IsContainer { get; set; }

    public bool IsCBM { get; set; }

    public bool IsFreight { get; set; }

    public bool IsLogistics { get; set; }

    public bool IsTrucking { get; set; }

    public bool IsKGS { get; set; }

    public bool IsGW { get; set; }

    public int? Order { get; set; }

    public decimal? ExchangeRate { get; set; }

    public string SettlementNo { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public string BasedId { get; set; }

    public string PmTypeId { get; set; }

    public string ShipmentFeeId { get; set; }

    public bool IsLock { get; set; }

    public bool IsPayment { get; set; }

    public decimal? Payable { get; set; }

    public decimal? Receivable { get; set; }
}
