using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class BrandShip
    {
        public int Id { get; set; }
        public int? BranchId { get; set; }
        public int? ShipId { get; set; }
        public string Code { get; set; }
        public string OldCode { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
