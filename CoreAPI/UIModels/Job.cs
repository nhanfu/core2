﻿using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Job
{
    public long Id { get; set; }

    public long? StateId { get; set; }

    public string StateName { get; set; }

    public string InvocationData { get; set; }

    public string Arguments { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpireAt { get; set; }

    public virtual ICollection<JobParameter> JobParameter { get; set; } = new List<JobParameter>();

    public virtual ICollection<State> State { get; set; } = new List<State>();
}
