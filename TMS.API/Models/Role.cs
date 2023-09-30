using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Role
    {
        public Role()
        {
            FeaturePolicy = new HashSet<FeaturePolicy>();
            InverseParentRole = new HashSet<Role>();
            TaskNotification = new HashSet<TaskNotification>();
            UserRole = new HashSet<UserRole>();
            UserSetting = new HashSet<UserSetting>();
        }

        public string Id { get; set; }
        public string VendorId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public string ParentRoleId { get; set; }
        public string CostCenterId { get; set; }
        public string Level { get; set; }
        public string Path { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string AccRoleId { get; set; }
        public string Length { get; set; }

        public virtual Role ParentRole { get; set; }
        public virtual ICollection<FeaturePolicy> FeaturePolicy { get; set; }
        public virtual ICollection<Role> InverseParentRole { get; set; }
        public virtual ICollection<TaskNotification> TaskNotification { get; set; }
        public virtual ICollection<UserRole> UserRole { get; set; }
        public virtual ICollection<UserSetting> UserSetting { get; set; }
    }
}
