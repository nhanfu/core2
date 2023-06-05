using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class QuotationService
    {
        public int Id { get; set; }
        public int? QuotationId { get; set; }
        public int? ServiceId { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public DateTime InsertDate { get; set; }
        public int InsertBy { get; set; }
        public int? UpdateDate { get; set; }
        public DateTime? UpdateBy { get; set; }
    }
}
