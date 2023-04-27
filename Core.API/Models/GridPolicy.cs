using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class GridPolicy
    {
        public int Id { get; set; }
        public Guid? UniqueId { get; set; }
        public int? FeatureId { get; set; }
        public int EntityId { get; set; }
        public string FieldName { get; set; }
        public int? Order { get; set; }
        public int? GroupOrder { get; set; }
        public string ShortDesc { get; set; }
        public string Description { get; set; }
        public bool IsPivot { get; set; }
        public int? ReferenceId { get; set; }
        public string FilterTemplate { get; set; }
        public string RefClass { get; set; }
        public string DataSource { get; set; }
        public string GroupName { get; set; }
        public string FormatCell { get; set; }
        public string FormatRow { get; set; }
        public string PlainText { get; set; }
        public string Width { get; set; }
        public string MinWidth { get; set; }
        public string MaxWidth { get; set; }
        public int? Precision { get; set; }
        public string Validation { get; set; }
        public string TextAlign { get; set; }
        public bool Frozen { get; set; }
        public bool Hidden { get; set; }
        public string Events { get; set; }
        public string ClassName { get; set; }
        public string Style { get; set; }
        public string ChildStyle { get; set; }
        public string Icon { get; set; }
        public bool AdvancedSearch { get; set; }
        public bool HasFilter { get; set; }
        public bool Editable { get; set; }
        public bool Disabled { get; set; }
        public string ComponentType { get; set; }
        public string PopulateField { get; set; }
        public string CascadeField { get; set; }
        public string Summary { get; set; }
        public int? SummaryColSpan { get; set; }
        public int? ComponentId { get; set; }
        public string DefaultVal { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? RoleId { get; set; }
        public bool IgnoreSync { get; set; }
        public bool AutoFit { get; set; }
        public bool IsPrivate { get; set; }
        public bool BasicSearch { get; set; }
        public string FormatExcell { get; set; }
        public string Query { get; set; }
        public string RefName { get; set; }
        public string DisabledExp { get; set; }
        public bool FocusSearch { get; set; }
        public string Template { get; set; }
        public string System { get; set; }
        public bool? UpperCase { get; set; }
        public bool? VirtualScroll { get; set; }
        public bool DisplayNone { get; set; }
        public string Migration { get; set; }
        public string ListClass { get; set; }
        public bool IsExport { get; set; }
        public int? OrderExport { get; set; }
        public bool IsRealtime { get; set; }
        public string GroupBy { get; set; }
        public string GroupFormat { get; set; }
        public bool? IsSumary { get; set; }
        public string ExcelFieldName { get; set; }
        public int? Row { get; set; }
        public bool FilterEq { get; set; }

        public virtual Entity Entity { get; set; }
        public virtual Feature Feature { get; set; }
        public virtual Entity Reference { get; set; }
    }
}
