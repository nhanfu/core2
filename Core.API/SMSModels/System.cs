using System;
using System.Collections.Generic;

namespace Core.SMSModels
{
    public partial class System
    {
        public System()
        {
            Tenant = new HashSet<Tenant>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public bool? Active { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? InsertedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }

        public virtual ICollection<Tenant> Tenant { get; set; }
    }
}
