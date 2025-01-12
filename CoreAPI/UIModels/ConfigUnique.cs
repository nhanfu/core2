using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ConfigUnique
{
    public string Id { get; set; }

    public bool IsSave { get; set; }

    public string ObjectId { get; set; }

    public string ObjectText { get; set; }

    public string ComponentIds { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
