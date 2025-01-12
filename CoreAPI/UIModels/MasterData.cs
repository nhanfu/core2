using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class MasterData
{
    public string Id { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string ParentId { get; set; }

    public string Path { get; set; }

    public bool IsLocal { get; set; }

    public bool IsContainer { get; set; }

    public bool IsCBM { get; set; }

    public bool IsKGS { get; set; }

    public bool IsGW { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public int? Enum { get; set; }

    public int? GroupEnum { get; set; }

    public string CodeMn { get; set; }
}
