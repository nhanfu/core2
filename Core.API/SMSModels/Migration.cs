using System;
using System.Collections.Generic;

namespace Core.SMSModels
{
    public partial class Migration
    {
        public string Id { get; set; }
        public string SystemId { get; set; }
        public string VersionId { get; set; }
        public string Up { get; set; }
        public string Down { get; set; }
        public bool? Active { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? InsertedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
    }
}
