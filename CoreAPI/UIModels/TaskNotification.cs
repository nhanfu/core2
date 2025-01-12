using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class TaskNotification
{
    public string Id { get; set; }

    public bool Read { get; set; }

    public string Icon { get; set; }

    public string AssignedId { get; set; }

    public string EntityId { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public string RecordId { get; set; }

    public int? VoucherTypeId { get; set; }
}
