using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class ComponentGroup
    {
        public ComponentGroup()
        {
            Component = new HashSet<Component>();
            InverseParent = new HashSet<ComponentGroup>();
        }

        public int Id { get; set; }
        public int FeatureId { get; set; }
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
        public int Column { get; set; }
        public int Row { get; set; }
        public int? PolicyId { get; set; }
        public bool Hidden { get; set; }
        public int? ParentId { get; set; }
        public int? Order { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public bool Disabled { get; set; }
        public int? XsCol { get; set; }
        public int? SmCol { get; set; }
        public int? LgCol { get; set; }
        public int? XlCol { get; set; }
        public int? XxlCol { get; set; }
        public int? OuterColumn { get; set; }
        public int? XsOuterColumn { get; set; }
        public int? SmOuterColumn { get; set; }
        public int? LgOuterColumn { get; set; }
        public int? XlOuterColumn { get; set; }
        public int? XxlOuterColumn { get; set; }
        public int? RoleId { get; set; }
        public string Icon { get; set; }
        public bool IgnoreSync { get; set; }
        public bool IsPrivate { get; set; }
        public int? BadgeMonth { get; set; }
        public bool? IsCollapsible { get; set; }
        public string ShowExp { get; set; }
        public string DisabledExp { get; set; }
        public bool IsDropDown { get; set; }

        public virtual Feature Feature { get; set; }
        public virtual ComponentGroup Parent { get; set; }
        public virtual ICollection<Component> Component { get; set; }
        public virtual ICollection<ComponentGroup> InverseParent { get; set; }
    }
}
