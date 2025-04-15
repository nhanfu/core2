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

    public bool IsSimple { get; set; }

    public string CompanyId { get; set; }

    public string Notes { get; set; }

    public string Description { get; set; }

    public string CompanyAddress { get; set; }

    public string Person { get; set; }

    public string VendorId { get; set; }

    public string VendorName { get; set; }

    public string PartnerId { get; set; }

    public string AccId { get; set; }

    public decimal? ExchangeRateINV { get; set; }

    public decimal? ExchangeRateINV2 { get; set; }

    public string ReferencesIds { get; set; }

    public string ReportCodeId { get; set; }

    public int? PayDay { get; set; }

    public DateTime? DueDate { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidDate { get; set; }

    public bool IsLock { get; set; }

    public bool IsInvoice { get; set; }

    public decimal? TotalAmountTax { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? AmountTax { get; set; }

    public decimal? VatInv { get; set; }

    public string EinvoiceLink { get; set; }

    public string EinvoiceCode { get; set; }

    public string InvoiceConfigId { get; set; }

    public string InvoiceNo { get; set; }

    public string SeriNo { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public DateTime? PostDate { get; set; }

    public string VendorIds { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string DateFieldId { get; set; }

    public string InvoiceIds { get; set; }

    public string ServiceIds { get; set; }

    public string FileIds { get; set; }

    public decimal? Vat { get; set; }

    public int? ShipmentStatusId { get; set; }

    public int? DebitTypeId { get; set; }

    public int? TaxInvoiceId { get; set; }

    public int? ChargePaidId { get; set; }

    public string FeeTypeId { get; set; }

    public string DescriptionIds { get; set; }

    public string HblIds { get; set; }

    public bool IsMultiple { get; set; }

    public string VatAccId { get; set; }

    public string Form { get; set; }

    public string InvNotes { get; set; }

    public int? PaymentAccTypeId { get; set; }

    public int? DebtTypeId { get; set; }

    public string DebtAccId { get; set; }

    public string PaymentAccId { get; set; }

    public string AdvId { get; set; }

    public bool IsDescription { get; set; }

    public string ReId { get; set; }

    public string InternalRefId { get; set; }

    public string FeatureName { get; set; }

    public string CurrencyCode { get; set; }
}
