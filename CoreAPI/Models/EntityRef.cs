namespace Core.Models
{
    public partial class EntityRef
    {
        public string Id { get; set; }

        public string TenantCode { get; set; }

        public string ComId { get; set; }

        public string HeaderId { get; set; }

        public string SectionId { get; set; }

        public string TargetComId { get; set; }

        public string TargetFieldName { get; set; }

        public bool Active { get; set; }

        public DateTime InsertedDate { get; set; }

        public string InsertedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public string MenuText { get; set; }

        public string ViewClass { get; set; }

        public string FieldName { get; set; }

        public string FeatureId { get; set; }

        public virtual Component Com { get; set; }
    }
}
