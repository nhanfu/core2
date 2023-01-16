using System;
using System.Collections.Generic;

namespace Core.SMSModels
{
    public partial class Migration
    {
        public int Id { get; set; }
        public int? SystemId { get; set; }
        public int? VersionId { get; set; }
        public string Up { get; set; }
        public string Down { get; set; }
        public bool? Active { get; set; }
        public int? InsertedBy { get; set; }
        public DateTimeOffset? InsertedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
    }
}
