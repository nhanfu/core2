using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class FeeType
{
    public string Id { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string GroupId { get; set; }

    public string GroupName { get; set; }

    public bool IsFreight { get; set; }

    public string OtherUnitId { get; set; }

    public string CurrencyId { get; set; }

    public bool IsObh { get; set; }

    public decimal? UnitPrice { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public int? SeqKey { get; set; }

    public bool IsTrucking { get; set; }

    public bool IsLogistics { get; set; }

    public bool IsAdv { get; set; }

    public bool IsAdvCustomer { get; set; }

    public bool IsAdvProvider { get; set; }
}
