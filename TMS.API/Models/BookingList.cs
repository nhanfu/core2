using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class BookingList
    {
        public int Id { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? RouteId { get; set; }
        public int? BrandShipId { get; set; }
        public int? ExportListId { get; set; }
        public int? ShipId { get; set; }
        public int? LineId { get; set; }
        public int? SocId { get; set; }
        public string Trip { get; set; }
        public DateTime? StartShip { get; set; }
        public int? ContainerTypeId { get; set; }
        public int? PolicyId { get; set; }
        public int? Count { get; set; }
        public decimal ShipUnitPrice { get; set; }
        public decimal ShipPrice { get; set; }
        public decimal ShipPolicyPrice { get; set; }
        public decimal OrtherFeePrice { get; set; }
        public decimal? TotalFee { get; set; }
        public string Note { get; set; }
        public string InvNo { get; set; }
        public DateTime? InvDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public int? PaymentMethodId { get; set; }
        public string Note1 { get; set; }
        public bool Submit { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal ActShipPrice { get; set; }
    }
}
