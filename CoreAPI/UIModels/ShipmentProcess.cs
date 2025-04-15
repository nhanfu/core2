using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ShipmentProcess
{
    public string Id { get; set; }

    public int? TypeId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
