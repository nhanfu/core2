using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class InvoiceConfig
{
    public string Id { get; set; }

    public string Description { get; set; }

    public string Form { get; set; }

    public string SeriNo { get; set; }

    public bool IsMultiple { get; set; }

    public int? StartNumer { get; set; }

    public int? EndNumer { get; set; }

    public string CompanyId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
