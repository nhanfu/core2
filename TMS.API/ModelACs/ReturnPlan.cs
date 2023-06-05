using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class ReturnPlan
    {
        public int Id { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ShipDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public string NotifiNo { get; set; }
        public int? ShipId { get; set; }
        public string Trip { get; set; }
        public string ContNo { get; set; }
        public string SealNo { get; set; }
        public int? BossId { get; set; }
        public int? SaleId { get; set; }
        public int? CommodityId { get; set; }
        public int? ContainerTypeId { get; set; }
        public decimal Cont20 { get; set; }
        public decimal Cont40 { get; set; }
        public int? LocationReturnId { get; set; }
        public string Notes { get; set; }
        public int? BrandShipId { get; set; }
        public int? ReturnSupplierId { get; set; }
        public int? ReturnUserId { get; set; }
        public int? ReturnDriverId { get; set; }
        public int? ReturnTruckId { get; set; }
        public decimal Dem { get; set; }
        public bool IsKt { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
