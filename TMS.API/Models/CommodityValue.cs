using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class CommodityValue
    {
        public int Id { get; set; }
        public int? SaleId { get; set; }
        public int? BossId { get; set; }
        public int? CommodityId { get; set; }
        public int? ContainerId { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsWet { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public string Notes { get; set; }
        public bool IsBought { get; set; }
        public int? CustomerTypeId { get; set; }
        public int? JourneyId { get; set; }
        public bool SteamingTerms { get; set; }
        public bool BreakTerms { get; set; }
    }
}
