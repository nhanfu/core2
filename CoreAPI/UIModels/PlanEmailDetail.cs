using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class PlanEmailDetail
{
    public string Id { get; set; }

    public string PlanEmailId { get; set; }

    public string TableName { get; set; }

    public string Email { get; set; }

    public string RecordId { get; set; }

    public DateTime? DailyDate { get; set; }

    public DateTime? NextStartDate { get; set; }

    public string Template { get; set; }

    public bool IsPause { get; set; }

    public bool IsStart { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
