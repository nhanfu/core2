using System;
using System.Collections.Generic;

namespace TMS.API.Models;

public partial class Intro
{
    public int Id { get; set; }

    public int? FeatureId { get; set; }

    public string FieldName { get; set; }

    public string Label { get; set; }

    public int? Order { get; set; }

    public bool Active { get; set; }

    public DateTimeOffset InsertedDate { get; set; }

    public int InsertedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public int? UpdatedBy { get; set; }
}
