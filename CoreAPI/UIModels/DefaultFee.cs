using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class DefaultFee
{
    public string Id { get; set; }

    public string GroupFee { get; set; }

    public string InquiryDetailId { get; set; }

    public string ServiceId { get; set; }

    public string VendorId { get; set; }

    public string DescriptionId { get; set; }

    public decimal? Quantity { get; set; }

    public string UnitId { get; set; }

    public decimal? MinUnitPrice { get; set; }

    public string CurrencyId { get; set; }

    public string CurrencyCode { get; set; }

    public decimal? Vat { get; set; }

    public decimal? Tax { get; set; }

    public decimal? TotalAmountTax { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? AmountTax { get; set; }

    public decimal? Amount { get; set; }

    public bool IsObh { get; set; }

    public string CmId { get; set; }

    public string Note { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string InquiryId { get; set; }

    public decimal? ExchangeRate { get; set; }

    public string OtherUnitId { get; set; }

    public int? Order { get; set; }

    public bool IsContainer { get; set; }

    public bool IsCBM { get; set; }

    public bool IsFreight { get; set; }

    public bool IsLogistics { get; set; }

    public bool IsTrucking { get; set; }

    public bool IsKGS { get; set; }

    public bool IsGW { get; set; }

    public bool IsGrossWeight { get; set; }

    public decimal? ExchangeRateVND { get; set; }

    public decimal? ExchangeRateUSD { get; set; }
}
