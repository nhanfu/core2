using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Revenue
    {
        public int Id { get; set; }
        public int? TransportationId { get; set; }
        public string LotNo { get; set; }
        public DateTime? LotDate { get; set; }
        public string InvoinceNo { get; set; }
        public DateTime? InvoinceDate { get; set; }
        public decimal? Vat { get; set; }
        public decimal? UnitPriceBeforeTax { get; set; }
        public decimal? UnitPriceAfterTax { get; set; }
        public decimal? ReceivedPrice { get; set; }
        public decimal? CollectOnBehaftPrice { get; set; }
        public decimal? TotalPriceBeforTax { get; set; }
        public decimal? VatPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public int? VendorVatId { get; set; }
        public string NotePayment { get; set; }
        public string Note { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual Transportation Transportation { get; set; }
    }
}
