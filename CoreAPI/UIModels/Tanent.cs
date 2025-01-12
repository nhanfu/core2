using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Tanent
{
    public string Id { get; set; }

    public string Password { get; set; }

    public string CompanyName { get; set; }

    public string TaxCode { get; set; }

    public string TanentCode { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTimeOffset? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }
}
