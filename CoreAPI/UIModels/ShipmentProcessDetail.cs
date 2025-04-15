using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ShipmentProcessDetail
{
    public string Id { get; set; }

    public string ProcessId { get; set; }

    public int? TypeId { get; set; }

    public string JobName { get; set; }

    public int? ModeId { get; set; }

    public bool IsTracing { get; set; }

    public bool IsDefault { get; set; }

    public int? BaseId { get; set; }

    public int? DeadLineNumer { get; set; }

    public int? AlertNumer { get; set; }

    public bool IsLockShipment { get; set; }

    public bool IsCompleteShipment { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
