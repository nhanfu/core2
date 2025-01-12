using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Config
{
    public string Id { get; set; }

    public string Description { get; set; }

    public string P1 { get; set; }

    public string P2 { get; set; }

    public string P3 { get; set; }

    public string P4 { get; set; }

    public string P5 { get; set; }

    public int? Increment { get; set; }

    public string TableName { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTime InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string ResetTypeId { get; set; }

    public string RecordId { get; set; }

    public string InsertedByIds { get; set; }

    public string RoleIds { get; set; }

    public string GroupKey { get; set; }

    public int? EnumId { get; set; }
}
