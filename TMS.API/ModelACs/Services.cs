using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class Services
    {
        public int Id { get; set; }
        public int? ComId { get; set; }
        public bool IsServer { get; set; }
        public bool IsBg { get; set; }
        public string CmdType { get; set; }
        public string Content { get; set; }
        public string History { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsPersit { get; set; }
        public string Environment { get; set; }
        public string Address { get; set; }
        public string Path { get; set; }
    }
}
