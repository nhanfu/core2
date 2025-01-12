using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class EntityProcessDetail
{
    public string Id { get; set; }

    public string EntityProcessId { get; set; }

    public string TaskName { get; set; }

    public bool IsAuto { get; set; }

    public bool IsTracing { get; set; }

    public bool IsDefaultShow { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
