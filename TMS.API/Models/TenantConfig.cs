using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class TenantConfig
    {
        public string Id { get; set; }

        public string TenantCode { get; set; }

        public string  Env { get; set; }
        
        public string  ConnKey { get; set; }

        public string ConnStr { get; set; }

        public bool Active { get; set; }

        public DateTimeOffset InsertedDate { get; set; }

        public string InsertedBy { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; }
    }
}