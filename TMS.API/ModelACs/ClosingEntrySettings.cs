using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class ClosingEntrySettings
    {
        public int Id { get; set; }
        public int? OriginAccountNo { get; set; }
        public int? DesAccountNo { get; set; }
        public bool OriginDebit { get; set; }
        public bool OriginCredit { get; set; }
        public bool DesDebit { get; set; }
        public bool DesCredit { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
