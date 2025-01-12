using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Terminal
{
    public string Id { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public string Address { get; set; }

    public bool IsLocal { get; set; }

    public string CountryId { get; set; }

    public bool Active { get; set; }

    public string ServiceText { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int? SeqKey { get; set; }

    public string CodeMn { get; set; }
}
