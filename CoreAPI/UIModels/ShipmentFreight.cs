using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ShipmentFreight
{
    public string Id { get; set; }

    public string ShipmentId { get; set; }

    public int? PmTypeId { get; set; }

    public decimal? WeightCharge { get; set; }

    public decimal? ValuationCharge { get; set; }

    public decimal? Tax { get; set; }

    public decimal? TotalDueAgent { get; set; }

    public decimal? TotalDueCarrier { get; set; }

    public decimal? Total { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
