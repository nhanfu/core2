using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class EntityTransit
{
    public string Id { get; set; }

    public string TableName { get; set; }

    public string FromId { get; set; }

    public string ToId { get; set; }

    public string PortTransitId { get; set; }

    public DateTime? EtdTsDate { get; set; }

    public DateTime? EtaTsDate { get; set; }

    public string TransitNo { get; set; }

    public string EntityId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
