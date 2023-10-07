using System;
using System.Collections.Generic;

namespace TMS.API.Models;

public partial class Role
{
    public string Id { get; set; }

    public string VendorId { get; set; }

    public string RoleName { get; set; }

    public string Description { get; set; }

    public string ParentRoleId { get; set; }

    public string CostCenterId { get; set; }

    public int Level { get; set; }

    public string Path { get; set; }

    public bool Active { get; set; }

    public DateTimeOffset InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public string AccRoleId { get; set; }

    public int? Length { get; set; }

    public virtual ICollection<FeaturePolicy> FeaturePolicy { get; set; } = new List<FeaturePolicy>();

    public virtual ICollection<Role> InverseParentRole { get; set; } = new List<Role>();

    public virtual Role ParentRole { get; set; }

    public virtual ICollection<TaskNotification> TaskNotification { get; set; } = new List<TaskNotification>();

    public virtual ICollection<UserRole> UserRole { get; set; } = new List<UserRole>();

    public virtual ICollection<UserSetting> UserSetting { get; set; } = new List<UserSetting>();
}
