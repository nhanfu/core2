using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Chat
    {
        public string Id { get; set; }
        public string ConvertationId { get; set; }
        public string Context { get; set; }
        public bool IsSelf { get; set; }
        public bool IsSeen { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }
    }
}
