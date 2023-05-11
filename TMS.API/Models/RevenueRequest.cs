using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class RevenueRequest
    {
        public int Id { get; set; }
        public int? RevenueId { get; set; }
        public int? StatusId { get; set; }
        public string Reason { get; set; }
        public string ReasonReject { get; set; }
        public int? TransportationId { get; set; }
        public int? TransportationRequestId { get; set; }
        public string LotNo { get; set; }
        public DateTime? LotDate { get; set; }
        public decimal? InvoinceNo { get; set; }
        public DateTime? InvoinceDate { get; set; }
        public decimal? Vat { get; set; }
        public decimal? UnitPriceBeforeTax { get; set; }
        public decimal? UnitPriceAfterTax { get; set; }
        public decimal? ReceivedPrice { get; set; }
        public decimal? CollectOnBehaftPrice { get; set; }
        public decimal? TotalPriceBeforTax { get; set; }
        public decimal? VatPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? RevenueAdjustment { get; set; }
        public int? VendorVatId { get; set; }
        public string NotePayment { get; set; }
        public string Note { get; set; }
        public string Name { get; set; }
        public int? BossId { get; set; }
        public string ContainerNo { get; set; }
        public string SealNo { get; set; }
        public int? ContainerTypeId { get; set; }
        public DateTime? ClosingDate { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? UserUpdate1 { get; set; }
        public int? UserUpdate2 { get; set; }
    }
}
