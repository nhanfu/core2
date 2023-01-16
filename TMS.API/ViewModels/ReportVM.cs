using System;

namespace TMS.API.ViewModels
{
    public class TranGroupVM
    {
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

    }
}
