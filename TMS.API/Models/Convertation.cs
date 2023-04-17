using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Convertation
    {
        public int Id { get; set; }
        public string FromName { get; set; }
        public string ToName { get; set; }
        public int? FromId { get; set; }
        public int? ToId { get; set; }
        public string LastContext { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
