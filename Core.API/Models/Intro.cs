using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class Intro
    {
        public int Id { get; set; }
        public int? FeatureId { get; set; }
        public int? ElementId { get; set; }
        public string FieldName { get; set; }
        public string Label { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
