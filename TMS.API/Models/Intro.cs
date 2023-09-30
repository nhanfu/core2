using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Intro
    {
        public string Id { get; set; }
        public string FeatureId { get; set; }
        public string FieldName { get; set; }
        public string Label { get; set; }
        public string Order { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}
