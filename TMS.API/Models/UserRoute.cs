using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class UserRoute
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? RouteId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? TypeId { get; set; }

        public virtual User User { get; set; }
    }
}
