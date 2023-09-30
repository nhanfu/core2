using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Component
    {
        public Component()
        {
            EntityRef = new HashSet<EntityRef>();
        }

        public string Id { get; set; }
        public string FieldName { get; set; }
        public string Order { get; set; }
        public string ComponentType { get; set; }
        public string ComponentGroupId { get; set; }
        public string DataSourceFilter { get; set; }
        public string ReferenceId { get; set; }
        public string FormatData { get; set; }
        public string FormatEntity { get; set; }
        public string PlainText { get; set; }
        public string Column { get; set; }
        public string Offset { get; set; }
        public string Row { get; set; }
        public bool CanSearch { get; set; }
        public bool CanCache { get; set; }
        public string Precision { get; set; }
        public string GroupBy { get; set; }
        public string GroupFormat { get; set; }
        public string Label { get; set; }
        public bool ShowLabel { get; set; }
        public string Icon { get; set; }
        public string ClassName { get; set; }
        public string Style { get; set; }
        public string ChildStyle { get; set; }
        public string HotKey { get; set; }
        public string RefClass { get; set; }
        public string Events { get; set; }
        public bool Disabled { get; set; }
        public bool Visibility { get; set; }
        public bool Hidden { get; set; }
        public string Validation { get; set; }
        public bool Focus { get; set; }
        public string Width { get; set; }
        public string PopulateField { get; set; }
        public string CascadeField { get; set; }
        public string GroupEvent { get; set; }
        public string XsCol { get; set; }
        public string SmCol { get; set; }
        public string LgCol { get; set; }
        public string XlCol { get; set; }
        public string XxlCol { get; set; }
        public string DefaultVal { get; set; }
        public string DateTimeField { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string RoleId { get; set; }
        public bool IgnoreSync { get; set; }
        public bool CanAdd { get; set; }
        public bool ShowAudit { get; set; }
        public bool IsPrivate { get; set; }
        public string IdField { get; set; }
        public string DescFieldName { get; set; }
        public string DescValue { get; set; }
        public string MonthCount { get; set; }
        public bool? IsDoubleLine { get; set; }
        public string Query { get; set; }
        public bool IsRealtime { get; set; }
        public string RefName { get; set; }
        public bool TopEmpty { get; set; }
        public bool IsCollapsible { get; set; }
        public string Template { get; set; }
        public string System { get; set; }
        public string PreQuery { get; set; }
        public string DisabledExp { get; set; }
        public bool FocusSearch { get; set; }
        public bool IsSumary { get; set; }
        public string FormatSumaryField { get; set; }
        public string OrderBySumary { get; set; }
        public bool ShowHotKey { get; set; }
        public string DefaultAddStart { get; set; }
        public string DefaultAddEnd { get; set; }
        public bool UpperCase { get; set; }
        public bool VirtualScroll { get; set; }
        public string Migration { get; set; }
        public string ListClass { get; set; }
        public string ExcelFieldName { get; set; }
        public bool LiteGrid { get; set; }
        public bool ShowDatetimeField { get; set; }
        public bool ShowNull { get; set; }
        public bool AddDate { get; set; }
        public bool FilterEq { get; set; }
        public string HeaderHeight { get; set; }
        public string BodyItemHeight { get; set; }
        public string FooterHeight { get; set; }
        public string ScrollHeight { get; set; }
        public string ScriptValidation { get; set; }
        public bool FilterLocal { get; set; }
        public bool HideGrid { get; set; }
        public string GroupReferenceId { get; set; }
        public string GroupReferenceName { get; set; }
        public string JoinTable { get; set; }
        public string GroupName { get; set; }
        public string ShortDesc { get; set; }
        public string Description { get; set; }
        public string FeatureId { get; set; }
        public string EntityId { get; set; }
        public string ComponentId { get; set; }
        public string TextAlign { get; set; }
        public bool HasFilter { get; set; }
        public bool Frozen { get; set; }
        public string FilterTemplate { get; set; }
        public bool Editable { get; set; }
        public string FormatExcell { get; set; }
        public string DatabaseName { get; set; }
        public string Summary { get; set; }
        public string SummaryColSpan { get; set; }
        public bool BasicSearch { get; set; }
        public bool IsExport { get; set; }
        public string OrderExport { get; set; }
        public string ShowExp { get; set; }
        public string MinWidth { get; set; }
        public string MaxWidth { get; set; }
        public bool AdvancedSearch { get; set; }
        public bool AutoFit { get; set; }
        public bool DisplayNone { get; set; }

        public virtual ComponentGroup ComponentGroup { get; set; }
        public virtual Entity Reference { get; set; }
        public virtual ICollection<EntityRef> EntityRef { get; set; }
    }
}
