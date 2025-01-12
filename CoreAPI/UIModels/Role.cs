using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Role
{
    public string Id { get; set; }

    public bool Hidden { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTimeOffset? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public string created_by { get; set; }
}
