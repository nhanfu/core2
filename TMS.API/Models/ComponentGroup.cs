using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class ComponentGroup
    {
        public ComponentGroup()
        {
            Component = new HashSet<Component>();
            InverseParent = new HashSet<ComponentGroup>();
        }

        public string Id { get; set; }
        public string FeatureId { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string ClassName { get; set; }
        public bool IsTab { get; set; }
        public string TabGroup { get; set; }
        public bool IsVertialTab { get; set; }
        public bool Responsive { get; set; }
        public string Events { get; set; }
        public string Width { get; set; }
        public string Style { get; set; }
        public string Column { get; set; }
        public string Row { get; set; }
        public string PolicyId { get; set; }
        public bool Hidden { get; set; }
        public string ParentId { get; set; }
        public string Order { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool Disabled { get; set; }
        public string XsCol { get; set; }
        public string SmCol { get; set; }
        public string LgCol { get; set; }
        public string XlCol { get; set; }
        public string XxlCol { get; set; }
        public string OuterColumn { get; set; }
        public string XsOuterColumn { get; set; }
        public string SmOuterColumn { get; set; }
        public string LgOuterColumn { get; set; }
        public string XlOuterColumn { get; set; }
        public string XxlOuterColumn { get; set; }
        public string RoleId { get; set; }
        public string Icon { get; set; }
        public bool IgnoreSync { get; set; }
        public bool IsPrivate { get; set; }
        public string BadgeMonth { get; set; }
        public bool? IsCollapsible { get; set; }
        public string DisabledExp { get; set; }
        public bool IsDropDown { get; set; }
        public bool DefaultCollapsed { get; set; }

        public virtual Feature Feature { get; set; }
        public virtual ComponentGroup Parent { get; set; }
        public virtual ICollection<Component> Component { get; set; }
        public virtual ICollection<ComponentGroup> InverseParent { get; set; }
    }
}
