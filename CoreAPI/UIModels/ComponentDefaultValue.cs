using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ComponentDefaultValue
{
    public string Id { get; set; }

    public string ComponentId { get; set; }

    public string Value { get; set; }

    public string UserId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
