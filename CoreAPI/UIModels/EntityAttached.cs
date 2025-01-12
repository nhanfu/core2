using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class EntityAttached
{
    public string Id { get; set; }

    public string TableName { get; set; }

    public string EntityId { get; set; }

    public string Name { get; set; }

    public string File { get; set; }

    public int? TypeId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
