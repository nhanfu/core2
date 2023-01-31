using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Expense
    {
        public int Id { get; set; }
        public int? TransportationId { get; set; }
        public int? ExpenseTypeId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Vat { get; set; }
        public bool IsVat { get; set; }
        public bool IsCollectOnBehaft { get; set; }
        public decimal TotalPriceBeforeTax { get; set; }
        public decimal TotalPriceAfterTax { get; set; }
        public string Notes { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? BranchId { get; set; }
        public int? AllotmentId { get; set; }
        public bool IsReturn { get; set; }
        public int? RouteId { get; set; }
        public int? ShipId { get; set; }
        public string Trip { get; set; }
        public DateTime? StartShip { get; set; }
        public int? ContainerTypeId { get; set; }
        public string ContainerNo { get; set; }
        public string SealNo { get; set; }
        public int? BossId { get; set; }
        public int? CommodityId { get; set; }
        public int? TransportationTypeId { get; set; }
        public bool IsWet { get; set; }
        public int? CommodityId2 { get; set; }
        public bool IsBought { get; set; }
        public string InsuranceFeeNotes { get; set; }
        public int? CustomerTypeId { get; set; }
        public bool IsPurchasedInsurance { get; set; }
        public decimal? InsuranceFeeRate { get; set; }
        public DateTime? DatePurchasedInsurance { get; set; }
        public int? JourneyId { get; set; }
        public string CommodityValueNotes { get; set; }
        public int? ClosingId { get; set; }
        public int? SaleId { get; set; }
        public bool IsCompany { get; set; }
        public decimal? CommodityValue { get; set; }
        public string NotesInsuranceFees { get; set; }
        public bool IsClosing { get; set; }
        public int? RequestChangeId { get; set; }
        public int? StatusId { get; set; }
        public bool isDelete { get; set; }
        public bool SteamingTerms { get; set; }
        public bool BreakTerms { get; set; }
        public DateTime? ClosingDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public virtual Allotment Allotment { get; set; }
        public virtual Transportation Transportation { get; set; }
    }
}
