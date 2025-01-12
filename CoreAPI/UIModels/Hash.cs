using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Hash
{
    public string Key { get; set; }

    public string Field { get; set; }

    public string Value { get; set; }

    public DateTime? ExpireAt { get; set; }
}
