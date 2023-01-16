using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Route
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Used { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? Click { get; set; }
        public int? Length { get; set; }
        public int? BranchId { get; set; }
    }
}
