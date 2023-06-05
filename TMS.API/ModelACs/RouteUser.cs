using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class RouteUser
    {
        public int Id { get; set; }
        public int? RouteId { get; set; }
        public int? UserId { get; set; }
        public decimal? Used { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
