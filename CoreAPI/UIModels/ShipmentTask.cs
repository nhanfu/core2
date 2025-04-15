using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ShipmentTask
{
    public string Id { get; set; }

    public string ShipmentId { get; set; }

    public string JobName { get; set; }

    public DateTime? JobDate { get; set; }

    public DateTime? EstimateFinishDate { get; set; }

    public DateTime? FinishDate { get; set; }

    public bool IsFinish { get; set; }

    public string Notes { get; set; }

    public string UserId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public bool IsTracing { get; set; }

    public virtual Shipment Shipment { get; set; }
}
