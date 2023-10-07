using System;
using System.Collections.Generic;

namespace TMS.API.Models;

public partial class Entity
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string AliasFor { get; set; }

    public bool Active { get; set; }

    public DateTimeOffset InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public string RefDetailClass { get; set; }

    public string RefListClass { get; set; }

    public string Namespace { get; set; }

    public virtual ICollection<Component> Component { get; set; } = new List<Component>();

    public virtual ICollection<Feature> Feature { get; set; } = new List<Feature>();

    public virtual ICollection<TaskNotification> TaskNotification { get; set; } = new List<TaskNotification>();
}
