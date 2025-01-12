using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class PartnerBankAccount
{
    public string Id { get; set; }

    public string PartnerId { get; set; }

    public string AccountName { get; set; }

    public string AccountNumber { get; set; }

    public string BankId { get; set; }

    public string AccountBranch { get; set; }

    public string OpenedAt { get; set; }

    public string SwiftCode { get; set; }

    public bool IsMain { get; set; }

    public bool IsReceives { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
