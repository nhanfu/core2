using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class AccountTransferSetup
{
    public string Id { get; set; }

    public string Code { get; set; }

    public int? Priority { get; set; }

    public string FromAccountNoId { get; set; }

    public string ToAccountNoId { get; set; }

    public int? TransferSideId { get; set; }

    public string Description { get; set; }

    public string CompanyId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
