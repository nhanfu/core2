using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class MarkupPricing
{
    public string Id { get; set; }

    public string TypeId { get; set; }

    public string Description { get; set; }

    public string ServiceId { get; set; }

    public string PartnerId { get; set; }

    public string PolId { get; set; }

    public string PodId { get; set; }

    public string PriceTypeId { get; set; }

    public decimal? MarkupLevel { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
