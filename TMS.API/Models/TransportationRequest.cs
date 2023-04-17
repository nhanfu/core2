using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class TransportationRequest
    {
        public int Id { get; set; }
        public int? TransportationId { get; set; }
        public bool IsRequestUnLockAll { get; set; }
        public bool IsRequestUnLockExploit { get; set; }
        public bool IsRequestUnLockAccountant { get; set; }
        public bool IsRequestUnLockShip { get; set; }
        public bool IsRequestUnLockRevenue { get; set; }
        public string ReasonUnLockAll { get; set; }
        public string ReasonUnLockExploit { get; set; }
        public string ReasonUnLockAccountant { get; set; }
        public string ReasonUnLockShip { get; set; }
        public string ReasonUnLockRevenue { get; set; }
        public int? StatusId { get; set; }
        public string ReasonReject { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual Transportation Transportation { get; set; }
    }
}
