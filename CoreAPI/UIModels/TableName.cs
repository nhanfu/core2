using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class TableName
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string TableTrigger { get; set; }

    public string Duplicate { get; set; }

    public string TanentId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
