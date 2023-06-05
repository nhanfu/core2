using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class DeleteHistory
    {
        public int Id { get; set; }
        public int? EntityId { get; set; }
        public string Value { get; set; }
        public int? RecordId { get; set; }
        public DateTime? InseredDate { get; set; }
    }
}
