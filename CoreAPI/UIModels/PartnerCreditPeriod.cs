using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class PartnerCreditPeriod
{
    public string Id { get; set; }

    public string PartnerId { get; set; }

    public string ShippmentTypeId { get; set; }

    public string CreditPeriodTypeId { get; set; }

    public int? Days { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
