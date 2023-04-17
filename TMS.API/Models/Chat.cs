using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Chat
    {
        public int Id { get; set; }
        public int? ConvertationId { get; set; }
        public string Context { get; set; }
        public bool IsSeft { get; set; }
        public bool IsSeen { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? FromId { get; set; }
        public int? ToId { get; set; }
    }
}
