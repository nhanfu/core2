using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class FreightRate
    {
        public int Id { get; set; }
        public int? BossId { get; set; }
        public int? UserId { get; set; }
        public int? TransportationTypeId { get; set; }
        public int? ReceivedId { get; set; }
        public int? ReturnId { get; set; }
        public decimal? UnitPriceCont20 { get; set; }
        public decimal? UnitPriceNoVatCont20 { get; set; }
        public decimal? UnitPriceCont40 { get; set; }
        public decimal? UnitPriceNoVatCont40 { get; set; }
        public decimal? UnitPriceTon { get; set; }
        public decimal? UnitPriceNoVatTon { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Notes { get; set; }
        public int? RouteId { get; set; }
        public bool IsClosing { get; set; }
        public bool IsApproveClosing { get; set; }
        public bool IsChange { get; set; }
        public int? RequestChangeId { get; set; }
        public bool? IsLocation { get; set; }
        public string Reason { get; set; }
        public int? ContainerTypeId { get; set; }
        public int? RegionReceivedId { get; set; }
        public int? RegionReturnId { get; set; }
        public bool IsEmptyCombination { get; set; }
        public int? TypeId { get; set; }
        public decimal? ReceivedCVCUnitPrice { get; set; }
        public decimal? ReturnCVCUnitPrice { get; set; }
        public decimal? ReceivedUnitPriceMax { get; set; }
        public decimal? ReturnUnitPriceMax { get; set; }
        public decimal? ReceivedUnitPriceAVG { get; set; }
        public decimal? ReturnUnitPriceAVG { get; set; }
        public decimal? ShipUnitPriceMax { get; set; }
        public decimal? ShipUnitPriceAVG { get; set; }
        public decimal? ReceivedReturnUnitPriceMax { get; set; }
        public decimal? ReceivedReturnUnitPriceAVG { get; set; }
        public decimal? InsuranceFee { get; set; }
        public decimal? OrtherUnitPrice { get; set; }
        public decimal? ProfitUnitPrice { get; set; }
        public decimal? TotalPriceMax { get; set; }
        public decimal? TotalPriceAVG { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
