using Core.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class Component
    {
        public string Id { get; set; }
        public string TenantCode { get; set; }
        public string FieldName { get; set; }
        public int? Order { get; set; }
        public string ComponentType { get; set; }
        public string ComponentGroupId { get; set; }
        public string ReferenceId { get; set; }
        public string FormatData { get; set; }
        public string FormatEntity { get; set; }
        public string PlainText { get; set; }
        public int? Column { get; set; }
        public int? Offset { get; set; }
        public int? Row { get; set; }
        public bool CanSearch { get; set; }
        public bool CanCache { get; set; }
        public int? Precision { get; set; }
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
        public int? XsCol { get; set; }
        public int? SmCol { get; set; }
        public int? LgCol { get; set; }
        public int? XlCol { get; set; }
        public int? XxlCol { get; set; }
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
        public int? MonthCount { get; set; }
        public bool? IsDoubleLine { get; set; }
        public string Query { get; set; }
        public string LocalQuery { get; set; }
        public bool IsRealtime { get; set; }
        public string RefName { get; set; }
        public bool TopEmpty { get; set; }
        public bool IsCollapsible { get; set; }
        public string Template { get; set; }
        public string Renderer { get; set; }
        public string PreQuery { get; set; }
        public string DisabledExp { get; set; }
        public bool FocusSearch { get; set; }
        public bool IsSumary { get; set; }
        public string FormatSumaryField { get; set; }
        public string OrderBySumary { get; set; }
        public bool ShowHotKey { get; set; }
        public int? DefaultAddStart { get; set; }
        public int? DefaultAddEnd { get; set; }
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
        public int? HeaderHeight { get; set; }
        public int? BodyItemHeight { get; set; }
        public int? FooterHeight { get; set; }
        public int? ScrollHeight { get; set; }
        public string ScriptValidation { get; set; }
        public bool FilterLocal { get; set; }
        public bool HideGrid { get; set; }
        public string GroupReferenceId { get; set; }
        public string GroupReferenceName { get; set; }
        public string GroupName { get; set; }
        public string ShortDesc { get; set; }
        public string Description { get; set; }
        public string FeatureId { get; set; }
        public string EntityId { get; set; }
        public string EntityName { get; set; }
        public string ComponentId { get; set; }
        public string TextAlign { get; set; }
        public bool HasFilter { get; set; }
        public bool Frozen { get; set; }
        public string FilterTemplate { get; set; }
        public bool Editable { get; set; }
        public string FormatExcell { get; set; }
        public string DatabaseName { get; set; }
        public string Summary { get; set; }
        public int? SummaryColSpan { get; set; }
        public bool BasicSearch { get; set; }
        public bool IsExport { get; set; }
        public int? OrderExport { get; set; }
        public string ShowExp { get; set; }
        public string MinWidth { get; set; }
        public string MaxWidth { get; set; }
        public bool AdvancedSearch { get; set; }
        public bool AutoFit { get; set; }
        public bool DisplayNone { get; set; }
        public string FieldText { get; set; }
        public string OrderBy { get; set; }
        public string QueueName { get; set; }
        public string ConnKey { get; set; }
        public bool ShouldSaveText { get; set; }
        public string CacheName { get; set; }

        [DbIgnore]
        public TextAlign? TextAlignEnum { get; set; }
        [DbIgnore]
        public bool IsPivot { get; set; }
        [DbIgnore]
        public int PostOrder { get; set; }
        [DbIgnore]
        public List<object> LocalData { get; set; }
        [DbIgnore]
        public List<Component> LocalHeader { get; set; }
        [DbIgnore]
        public bool StatusBar { get; set; }
        [DbIgnore]
        public bool SimpleText { get; set; }
        [DbIgnore]
        public bool LocalRender { get; set; }
        [DbIgnore]
        public bool IgnoreConfirmHardDelete { get; set; }

        [DbIgnore]
        public virtual ComponentGroup ComponentGroup { get; set; }
        [DbIgnore]
        public virtual Entity Reference { get; set; }
    }
}
