using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class InvoiceForm
    {
        public int Id { get; set; }
        public string InvoiceFormId { get; set; }
        public string InvoiceID { get; set; }
        public string Description { get; set; }
        public string Sign { get; set; }
        public decimal? StartInvoiceNo { get; set; }
        public decimal? EndInvoiceNo { get; set; }
        public int? InvSize { get; set; }
        public int? Used { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
