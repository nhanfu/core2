using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class EntityContainer
{
    public string Id { get; set; }

    public string TableName { get; set; }

    public string EntityId { get; set; }

    public decimal? Quantity { get; set; }

    public string ContainerTypeId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public string ContainerNo { get; set; }

    public string SealNo { get; set; }

    public decimal? PKG { get; set; }

    public decimal? Weight { get; set; }

    public decimal? CBM { get; set; }

    public decimal? Tare { get; set; }

    public string Description { get; set; }

    public string HSCode { get; set; }

    public bool IsPartial { get; set; }

    public string UnitId { get; set; }
}
