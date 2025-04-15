using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ShipmentInvoice
{
    public string Id { get; set; }

    public int? TypeId { get; set; }

    public string ShipmentId { get; set; }

    public string VendorIds { get; set; }

    public string VendorIdsText { get; set; }

    public string Code { get; set; }

    public string OtherCode { get; set; }

    public int? ActionId { get; set; }

    public bool IsSendPartner { get; set; }

    public DateTime? PostDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public DateTime? PostOtherDate { get; set; }

    public DateTime? RevisedDate { get; set; }

    public int? InvoiceTypeId { get; set; }

    public string HblIds { get; set; }

    public string FileIds { get; set; }

    public string MblNos { get; set; }

    public string HblNo { get; set; }

    public string FileNo { get; set; }

    public int? ObhTypeId { get; set; }

    public string FeeTypeId { get; set; }

    public int? IncludeChargeId { get; set; }

    public decimal? ExchangeRateVND { get; set; }

    public decimal? ExchangeRateUSD { get; set; }

    public decimal? ExchangeRateINV { get; set; }

    public decimal? ExchangeRateINV2 { get; set; }

    public string Remark { get; set; }

    public string Description { get; set; }

    public string RequestTypeId { get; set; }

    public string ReceiverIds { get; set; }

    public int? SeqKey { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? DeadlineDate { get; set; }

    public int? ProgressId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int? DateModeId { get; set; }

    public string ServiceIds { get; set; }

    public string DescriptionId { get; set; }

    public string DocsNo { get; set; }

    public int? ChargePaidId { get; set; }

    public int? NoDocsId { get; set; }

    public decimal? DebitAmount { get; set; }

    public string CurrencyId { get; set; }

    public string CurrencyCode { get; set; }

    public int? PaymentMethodId { get; set; }

    public int? StatusId { get; set; }

    public int? VoucherTypeId { get; set; }

    public int? TaxInvoiceId { get; set; }

    public string DateFieldId { get; set; }

    public string UserViewIds { get; set; }

    public string UserApprovedIds { get; set; }

    public string ForwardId { get; set; }

    public string FormatChat { get; set; }

    public int? PaymentId { get; set; }

    public string AttachedFile { get; set; }

    public string TaxCode { get; set; }

    public string CompanyIdText { get; set; }

    public string CompanyId { get; set; }

    public string CompanyAddress { get; set; }

    public string BuyerName { get; set; }

    public string CustomerId { get; set; }

    public string CustomerIdText { get; set; }

    public bool IsRevenue { get; set; }

    public string InvoiceNo { get; set; }

    public string SeriNo { get; set; }

    public string Notes { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidDate { get; set; }

    public bool IsReceiptPayment { get; set; }

    public bool IsSend { get; set; }

    public bool NoApproved { get; set; }

    public int? ShipmentStatusId { get; set; }

    public decimal? TotalAmountTax { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? AmountTax { get; set; }

    public decimal? VatInv { get; set; }

    public string EinvoiceLink { get; set; }

    public string EinvoiceCode { get; set; }

    public string AmountText { get; set; }

    public string InvoiceConfigId { get; set; }

    public bool IsMultiple { get; set; }

    public int? DebitTypeId { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public int? DocsTypeId { get; set; }

    public bool IsSendToPartner { get; set; }

    public string ShipmentInvoiceId { get; set; }

    public string PaymentAccId { get; set; }

    public string DebtAccId { get; set; }

    public string InvoiceIds { get; set; }

    public string IssueInvoiceId { get; set; }

    public string VatInvoiceId { get; set; }
}
