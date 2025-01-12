using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class PricingDetail
{
    public string Id { get; set; }

    public string GroupFee { get; set; }

    public string PricingId { get; set; }

    public string UnitId { get; set; }

    public string OtherUnitId { get; set; }

    public string DescriptionId { get; set; }

    public string CurrencyId { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? MinUnitPrice { get; set; }

    public decimal? Vat { get; set; }

    public string VendorId { get; set; }

    public bool IsObh { get; set; }

    public string Note { get; set; }

    public int? Order { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public decimal? ExchangeRateVND { get; set; }

    public decimal? ExchangeRateUSD { get; set; }

    public bool IsFreight { get; set; }

    public bool IsContainer { get; set; }

    public bool IsCBM { get; set; }

    public bool IsKGS { get; set; }

    public bool IsGW { get; set; }

    public bool IsGrossWeight { get; set; }

    public decimal? DemFree { get; set; }

    public decimal? DetFree { get; set; }

    public string PkEmptyId { get; set; }

    public string ReturnsId { get; set; }

    public bool IsLogistics { get; set; }

    public bool IsTrucking { get; set; }
}
