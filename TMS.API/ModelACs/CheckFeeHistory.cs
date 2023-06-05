using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class CheckFeeHistory
    {
        public int Id { get; set; }
        public int? ClosingId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? TypeId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
