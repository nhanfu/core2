using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Voucher
{
    public string Id { get; set; }

    public int? TypeId { get; set; }

    public int? VoucherTypeId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? VoucherDate { get; set; }

    public string Code { get; set; }

    public string DepartmentId { get; set; }

    public string PositionId { get; set; }

    public int? ActionId { get; set; }

    public int? AutoProgressId { get; set; }

    public decimal? Amount { get; set; }

    public string CurrencyId { get; set; }

    public string PaymentMethodId { get; set; }

    public string AmountText { get; set; }

    public string AttachedFile { get; set; }

    public DateTime? DeadlineDate { get; set; }

    public string Note { get; set; }

    public decimal? AdvanceAmount { get; set; }

    public decimal? SettlementAmount { get; set; }

    public decimal? RemainingAmount { get; set; }

    public string UserId { get; set; }

    public string UserCode { get; set; }

    public int? SeqKey { get; set; }

    public string ParentId { get; set; }

    public string VoucherId { get; set; }

    public string UserViewIds { get; set; }

    public string UserApprovedIds { get; set; }

    public string ForwardId { get; set; }

    public int? StatusId { get; set; }

    public string FormatChat { get; set; }

    public string VoucherNo { get; set; }
}
