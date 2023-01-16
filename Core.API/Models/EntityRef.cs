using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class EntityRef
    {
        public int Id { get; set; }
        public int? ComId { get; set; }
        public int? HeaderId { get; set; }
        public int? SectionId { get; set; }
        public int? TargetComId { get; set; }
        public string TargetFieldName { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public string MenuText { get; set; }
        public string ViewClass { get; set; }
        public string FieldName { get; set; }

        public virtual Component Com { get; set; }
    }
}
