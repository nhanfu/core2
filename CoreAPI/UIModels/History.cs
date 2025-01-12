using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class History
{
    public string Id { get; set; }

    public string TextContent { get; set; }

    public string Value { get; set; }

    public string OldValue { get; set; }

    public string ComponentId { get; set; }

    public string RecordId { get; set; }

    public string TableName { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public string created_by { get; set; }
}
