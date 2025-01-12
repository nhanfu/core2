using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ExchangeRate
{
    public string Id { get; set; }

    public string CurrencyId { get; set; }

    public string CurrencyCode { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public decimal? RateUSD { get; set; }

    public decimal? RateVND { get; set; }

    public decimal? RateSaleUSD { get; set; }

    public decimal? RateSaleVND { get; set; }

    public decimal? RateProfitUSD { get; set; }

    public decimal? RateProfitVND { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
