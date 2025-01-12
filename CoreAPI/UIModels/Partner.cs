using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Partner
{
    public string Id { get; set; }

    public int? ServiceId { get; set; }

    public int? TypeId { get; set; }

    public string Name { get; set; }

    public string FullName { get; set; }

    public string DebitName { get; set; }

    public string Address { get; set; }

    public string PhoneNumber { get; set; }

    public string Mail { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public string Code { get; set; }

    public string TaxCode { get; set; }

    public string GroupId { get; set; }

    public string GenderId { get; set; }

    public DateTime? IssuedDate { get; set; }

    public string Note { get; set; }

    public string GenderContactId { get; set; }

    public string ContactName { get; set; }

    public string ContactEmail { get; set; }

    public string ContactPhoneNumber { get; set; }

    public int? DebitDay { get; set; }

    public decimal? DebitAmount { get; set; }

    public decimal? CreditLimit { get; set; }

    public string DebitAccountId { get; set; }

    public bool IsPublic { get; set; }

    public string Web { get; set; }

    public int? SeqKey { get; set; }

    public string SaleId { get; set; }

    public string Attachment { get; set; }

    public string Email { get; set; }

    public string SourseId { get; set; }

    public string RaitingId { get; set; }

    public DateTime? Dob { get; set; }

    public string PicId { get; set; }

    public string CustomerTypeId { get; set; }

    public string CompanyName { get; set; }

    public string ResidenceTypeId { get; set; }

    public string ResidenceTypeIdText { get; set; }

    public string Description { get; set; }

    public string CompanyNameInv { get; set; }

    public string EmailInv { get; set; }

    public string AssignmentDebitId { get; set; }

    public string AssignmentInvId { get; set; }

    public string AccountNumber { get; set; }

    public string AccountName { get; set; }

    public string BankId { get; set; }

    public string AccountBranch { get; set; }

    public string SwiftCode { get; set; }

    public string OpenedAt { get; set; }

    public string IdCode { get; set; }

    public string Password { get; set; }

    public string Industry { get; set; }

    public string Po { get; set; }

    public int? RushMonth { get; set; }

    public string Rating { get; set; }

    public DateTime? PoDate { get; set; }

    public decimal? MinProfitMonth { get; set; }

    public string ToastWarning { get; set; }

    public int? ActionId { get; set; }

    public string RegularShippingFrom { get; set; }

    public string ConditionId { get; set; }

    public string DistributeInformationIds { get; set; }

    public string DistributeInformationIdsText { get; set; }

    public string ZipCode { get; set; }

    public string TrackingURL { get; set; }

    public bool IsNoDebt { get; set; }

    public string PartnerTypeIds { get; set; }

    public string AdditionTypeIds { get; set; }

    public string PartnerTypeIdsText { get; set; }

    public string AdditionTypeIdsText { get; set; }

    public string FormatChat { get; set; }

    public string AddressInv { get; set; }

    public string AssignId { get; set; }

    public string History { get; set; }

    public DateTime? Birthday { get; set; }

    public string Logo { get; set; }

    public string WebSite { get; set; }

    public string Fax { get; set; }

    public string ReceivesAccountNumber { get; set; }

    public string ReceivesAccountName { get; set; }

    public string ReceivesBankId { get; set; }

    public string ReceivesSwiftCode { get; set; }
}
