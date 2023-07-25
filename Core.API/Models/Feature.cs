using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class Feature
    {
        public Feature()
        {
            ComponentGroup = new HashSet<ComponentGroup>();
            FeaturePolicy = new HashSet<FeaturePolicy>();
            GridPolicy = new HashSet<GridPolicy>();
            InverseParent = new HashSet<Feature>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public int? ParentId { get; set; }
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
        public int? EntityId { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsSystem { get; set; }
        public bool IgnoreEncode { get; set; }
        public string RequireJS { get; set; }
        public string GuiInfo { get; set; }
        public int RoleId { get; set; }
        public bool IsPermissionInherited { get; set; }
        public string FeatureGroup { get; set; }
        public bool InheritParentFeature { get; set; }
        public string Properties { get; set; }
        public string Template { get; set; }
        public int? LayoutId { get; set; }
        public string DataSource { get; set; }
        public string Gallery { get; set; }
        public bool DeleteTemp { get; set; }

        public virtual Entity Entity { get; set; }
        public virtual Feature Parent { get; set; }
        public virtual ICollection<ComponentGroup> ComponentGroup { get; set; }
        public virtual ICollection<FeaturePolicy> FeaturePolicy { get; set; }
        public virtual ICollection<GridPolicy> GridPolicy { get; set; }
        public virtual ICollection<Feature> InverseParent { get; set; }
    }
}
