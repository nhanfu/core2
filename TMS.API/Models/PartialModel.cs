using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.API.Models
{
    public partial class Booking
    {
        [NotMapped]
        public decimal TotalDate => (decimal)(Convert.ToDateTime(BookingExpired) == null ? 0 : (Convert.ToDateTime(BookingExpired) - Convert.ToDateTime(DateTime.Now)).TotalHours);

        [NotMapped]
        public bool IsLock => Convert.ToDateTime(BookingExpired) != null && Convert.ToDateTime(BookingExpired) < Convert.ToDateTime(DateTime.Now);
    }

    public partial class TransportationContract
    {
        [NotMapped]
        public int CountDate => EndDate is null ? 0 : Convert.ToInt32((EndDate.Value.Date - DateTime.Now.Date).TotalDays);
    }

    public partial class Transportation
    {
        [NotMapped]
        public DateTime? FromDate { get; set; }
        [NotMapped]
        public DateTime? ToDate { get; set; }
        [NotMapped]
        public List<int> RouteIds { get; set; }
        [NotMapped]
        public DateTime? ShipDateNew { get; set; }
        [NotMapped]
        public int? PortLiftNewId { get; set; }
    }

    public partial class BookingList
    {
        [NotMapped]
        public DateTime? FromDate { get; set; }
        [NotMapped]
        public DateTime? ToDate { get; set; }
        [NotMapped]
        public string TransportationIds { get; set; }
        [NotMapped]
        public decimal AVGTotalPrice { get; set; }
        [NotMapped]
        public decimal TotalCount { get; set; }
        [NotMapped]
        public decimal TotalTotalPrice { get; set; }
        [NotMapped]
        public decimal AVGTotalPriceCont20 { get; set; }
        [NotMapped]
        public decimal TotalCountCont20 { get; set; }
        [NotMapped]
        public decimal TotalTotalPriceCont20 { get; set; }
        [NotMapped]
        public decimal AVGTotalPriceCont40 { get; set; }
        [NotMapped]
        public decimal TotalCountCont40 { get; set; }
        [NotMapped]
        public decimal TotalTotalPriceCont40 { get; set; }
    }

    public partial class Expense
    {
        [NotMapped]
        public DateTime? FromDate { get; set; }
        [NotMapped]
        public DateTime? ToDate { get; set; }
        [NotMapped]
        public string TransportationIds { get; set; }
    }

    public partial class QuotationUpdate
    {
        [NotMapped]
        public List<int?> PackingIds { get; set; }
        [NotMapped]
        public List<int?> Packing1Ids { get; set; }
        [NotMapped]
        public List<int?> BossIds { get; set; }
        [NotMapped]
        public List<int?> Boss1Ids { get; set; }
        [NotMapped]
        public List<int?> RegionIds { get; set; }
        [NotMapped]
        public List<int?> Region1Ids { get; set; }
    }

    public partial class CheckFeeHistory
    {
        [NotMapped]
        public List<int> RouteIds { get; set; }
    }

    public partial class Revenue
    {
        [NotMapped]
        public bool IsLotNo { get; set; }
        [NotMapped]
        public bool IsLotDate { get; set; }
        [NotMapped]
        public bool IsInvoinceNo { get; set; }
        [NotMapped]
        public bool IsInvoinceDate { get; set; }
        [NotMapped]
        public bool IsUnitPriceBeforeTax { get; set; }
        [NotMapped]
        public bool IsUnitPriceAfterTax { get; set; }
        [NotMapped]
        public bool IsReceivedPrice { get; set; }
        [NotMapped]
        public bool IsCollectOnBehaftPrice { get; set; }
        [NotMapped]
        public bool IsVat { get; set; }
        [NotMapped]
        public bool IsTotalPriceBeforTax { get; set; }
        [NotMapped]
        public bool IsVatPrice { get; set; }
        [NotMapped]
        public bool IsTotalPrice { get; set; }
        [NotMapped]
        public bool IsNotePayment { get; set; }
        [NotMapped]
        public bool IsVendorVatId { get; set; }
        [NotMapped]
        public bool IsAll { get; set; }
    }

    public partial class Route
    {
        [NotMapped]
        public int Order { get; set; }
    }

    public partial class Vendor
    {
        [NotMapped]
        public bool IsUpdate { get; set; }
    }
}
