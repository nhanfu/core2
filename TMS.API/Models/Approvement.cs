using System;
using System.Collections.Generic;

namespace TMS.API.Models;

public partial class Approvement
{
    public string Id { get; set; }

    public string ReasonOfChange { get; set; }

    public string EntityId { get; set; }

    public string LevelName { get; set; }

    public string RecordId { get; set; }

    public string StatusId { get; set; }

    public decimal Amount { get; set; }

    public string UserApproveId { get; set; }

    public bool IsEnd { get; set; }

    public int? NextLevel { get; set; }

    public int CurrentLevel { get; set; }

    public bool Approved { get; set; }

    public string ApprovedBy { get; set; }

    public DateTimeOffset? ApprovedDate { get; set; }

    public string RejectBy { get; set; }

    public DateTimeOffset? RejectDate { get; set; }

    public bool Active { get; set; }

    public DateTimeOffset InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
