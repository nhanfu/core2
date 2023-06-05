using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class InsuranceFeesRate
    {
        public int Id { get; set; }
        public int? TransportationTypeId { get; set; }
        public bool IsWet { get; set; }
        public int? JourneyId { get; set; }
        public bool IsBought { get; set; }
        public bool? IsVAT { get; set; }
        public bool? IsSOC { get; set; }
        public decimal Rate { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
