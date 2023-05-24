using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class TransportationPlan
    {
        public int Id { get; set; }
        public bool IsLocation { get; set; }
        public bool IsTransportation { get; set; }
        public int? ExportListId { get; set; }
        public int? BranchId { get; set; }
        public DateTime? PlanDate { get; set; }
        public int? UserId { get; set; }
        public int? RouteId { get; set; }
        public int? BossId { get; set; }
        public int? CommodityId { get; set; }
        public int? ContainerTypeId { get; set; }
        public bool IsContract { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int? ReceivedId { get; set; }
        public int? TotalContainer { get; set; }
        public int? TotalContainerUsing { get; set; }
        public int? TotalContainerRemain { get; set; }
        public string Notes { get; set; }
        public string NotesContract { get; set; }
        public string Files { get; set; }
        public string Name { get; set; }
        public decimal? CommodityValue { get; set; }
        public bool IsWet { get; set; }
        public bool IsBought { get; set; }
        public int? CustomerTypeId { get; set; }
        public int? TransportationTypeId { get; set; }
        public bool IsCompany { get; set; }
        public int? JourneyId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? RequestChangeId { get; set; }
        public string RequestChangeNote { get; set; }
        public int? StatusId { get; set; }
        public int? CountContract { get; set; }
        public bool IsSettingsInsurance { get; set; }
        public int? ListId { get; set; }
        public int? Contact2Id { get; set; }
        public int? ReturnId { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string ReturnNotes { get; set; }
        public bool IsQuotation { get; set; }
        public bool SteamingTerms { get; set; }
        public bool BreakTerms { get; set; }
        public int? Cont40Text { get; set; }
        public int? Cont20Text { get; set; }
        public string ReasonChange { get; set; }
        public string CommodityValueNotes { get; set; }
        public string ReasonOfChange { get; set; }
        public string TextOfChange { get; set; }
        public int? User2Id { get; set; }
    }
}
