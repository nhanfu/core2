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
            Component = new HashSet<Component>();
            InverseParent = new HashSet<Feature>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public string ParentId { get; set; }
        public string Order { get; set; }
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
        public string EntityName { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsSystem { get; set; }
        public bool IgnoreEncode { get; set; }
        public string RequireJS { get; set; }
        public string GuiInfo { get; set; }
        public string RoleId { get; set; }
        public bool IsPermissionInherited { get; set; }
        public string FeatureGroup { get; set; }
        public bool InheritParentFeature { get; set; }
        public string Properties { get; set; }
        public string Template { get; set; }
        public string LayoutId { get; set; }
        public string DataSourceFilter { get; set; }
        public string Gallery { get; set; }
        public bool DeleteTemp { get; set; }
        public bool CustomNextCell { get; set; }

        public virtual Entity Entity { get; set; }
        public virtual Feature Parent { get; set; }
        public virtual ICollection<ComponentGroup> ComponentGroup { get; set; }
        public virtual ICollection<FeaturePolicy> FeaturePolicy { get; set; }
        public virtual ICollection<Component> Component { get; set; }
        public virtual ICollection<Feature> InverseParent { get; set; }
    }
}
