using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ExchangeRateHistoryDetail
{
    public string Id { get; set; }

    public string HistoryId { get; set; }

    public string CurrencyId { get; set; }

    public string CurrencyCode { get; set; }

    public decimal? RateUSD { get; set; }

    public decimal? RateVND { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
