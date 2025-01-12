using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class EntityDimension
{
    public string Id { get; set; }

    public string TableName { get; set; }

    public string EntityId { get; set; }

    public decimal? Length { get; set; }

    public decimal? Width { get; set; }

    public decimal? Height { get; set; }

    public decimal? Quantity { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
