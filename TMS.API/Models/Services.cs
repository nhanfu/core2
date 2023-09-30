using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Services
    {
        public string Id { get; set; }
        public string ComId { get; set; }
        public bool IsServer { get; set; }
        public bool IsBg { get; set; }
        public string CmdType { get; set; }
        public string Content { get; set; }
        public string History { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsPersit { get; set; }
        public string Environment { get; set; }
        public string Address { get; set; }
        public string Path { get; set; }
    }
}
