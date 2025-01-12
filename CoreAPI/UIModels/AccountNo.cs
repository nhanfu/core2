using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class AccountNo
{
    public string Id { get; set; }

    public string ParentId { get; set; }

    public int? No { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public string NameEnglish { get; set; }

    public string TypeId { get; set; }

    public string Reason { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
