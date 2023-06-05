using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class SettingTransportation
    {
        public int Id { get; set; }
        public int? RouteId { get; set; }
        public int? BranchShipId { get; set; }
        public int? Day { get; set; }
        public DateTime? StartDate { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
