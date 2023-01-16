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
    }
}
