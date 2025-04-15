using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Feature
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Label { get; set; }

    public string ParentId { get; set; }

    public int? Order { get; set; }

    public string ClassName { get; set; }

    public string Style { get; set; }

    public string StyleSheet { get; set; }

    public string Script { get; set; }

    public string Events { get; set; }

    public string Icon { get; set; }

    public bool IsDevider { get; set; }

    public bool IsGroup { get; set; }

    public bool IsMenu { get; set; }

    public bool IsPublic { get; set; }

    public bool StartUp { get; set; }

    public string ViewClass { get; set; }

    public string EntityId { get; set; }

    public string Description { get; set; }

    public bool? Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public bool IsSystem { get; set; }

    public bool IgnoreEncode { get; set; }

    public bool InheritParentFeature { get; set; }

    public bool DeleteTemp { get; set; }

    public bool CustomNextCell { get; set; }

    public bool LoadEntity { get; set; }

    public bool IsLock { get; set; }

    public bool IsFlow { get; set; }

    public string CodeId { get; set; }

    public string AppId { get; set; }
}
