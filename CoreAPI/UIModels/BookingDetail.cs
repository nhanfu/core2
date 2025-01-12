using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class BookingDetail
{
    public string Id { get; set; }

    public string BookingId { get; set; }

    public string FromId { get; set; }

    public string ToId { get; set; }

    public string Flight { get; set; }

    public DateTime? EtdDate { get; set; }

    public DateTime? EtaDate { get; set; }

    public string GroupFee { get; set; }

    public bool OtherFee { get; set; }

    public string VendorId { get; set; }

    public string DescriptionId { get; set; }

    public int? Order { get; set; }

    public bool IsContainer { get; set; }

    public bool IsCBM { get; set; }

    public bool IsFreight { get; set; }

    public bool IsKGS { get; set; }

    public bool IsGW { get; set; }

    public bool IsGrossWeight { get; set; }

    public decimal? Quantity { get; set; }

    public string UnitId { get; set; }

    public decimal? TotalAmountTax { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? AmountTax { get; set; }

    public string CurrencyId { get; set; }

    public string CurrencyCode { get; set; }

    public decimal? ExchangeRateVND { get; set; }

    public decimal? ExchangeRateUSD { get; set; }

    public decimal? Vat { get; set; }

    public decimal? Amount { get; set; }

    public bool IsBuying { get; set; }

    public bool IsObh { get; set; }

    public bool IsCM { get; set; }

    public string Note { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public bool IsLogistics { get; set; }

    public bool IsTrucking { get; set; }

    public bool IsAdv { get; set; }

    public bool IsAdvCustomer { get; set; }

    public bool IsAdvProvider { get; set; }
}
