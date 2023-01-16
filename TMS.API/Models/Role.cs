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

        public int Id { get; set; }
        public int? VendorId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public int? ParentRoleId { get; set; }
        public int? CostCenterId { get; set; }
        public int Level { get; set; }
        public string Path { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int? AccRoleId { get; set; }
        public int? Length { get; set; }

        public virtual Role ParentRole { get; set; }
        public virtual ICollection<FeaturePolicy> FeaturePolicy { get; set; }
        public virtual ICollection<Role> InverseParentRole { get; set; }
        public virtual ICollection<TaskNotification> TaskNotification { get; set; }
        public virtual ICollection<UserRole> UserRole { get; set; }
        public virtual ICollection<UserSetting> UserSetting { get; set; }
    }
}
