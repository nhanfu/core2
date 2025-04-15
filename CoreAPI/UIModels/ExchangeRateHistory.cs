using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ExchangeRateHistory
{
    public string Id { get; set; }

    public string Code { get; set; }

    public string FileIds { get; set; }

    public DateTime? PostDate { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string CompanyId { get; set; }

    public decimal? RateUSD { get; set; }

    public decimal? RateVND { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
