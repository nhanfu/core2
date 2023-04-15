using System;
using System.Collections.Generic;

namespace TMS.API.Models;

public partial class TransportationRequestDetails
{
    public int Id { get; set; }

    public int? TransportationRequestId { get; set; }

    public int? GridPolicyId { get; set; }

    public string OldValue { get; set; }

    public string NewValue { get; set; }

    public bool Active { get; set; }

    public DateTime InsertedDate { get; set; }

    public int InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int? UpdatedBy { get; set; }
}
