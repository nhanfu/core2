using System;
using System.Collections.Generic;

namespace TMS.API.Models
{

    public partial class UserRole
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public string RoleId { get; set; }

        public bool Active { get; set; }

        public DateTimeOffset? EffectiveDate { get; set; }

        public DateTimeOffset? ExpiredDate { get; set; }

        public DateTimeOffset InsertedDate { get; set; }

        public string InsertedBy { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Role Role { get; set; }

        public virtual User User { get; set; }
    }
}
