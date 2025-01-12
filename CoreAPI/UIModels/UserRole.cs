using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class UserRole
{
    public string Id { get; set; }

    public string UserId { get; set; }

    public string RoleId { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTimeOffset? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public string created_by { get; set; }
}
