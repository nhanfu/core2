using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class EntityProcess
{
    public string Id { get; set; }

    public string ServiceId { get; set; }

    public int? TypeId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
