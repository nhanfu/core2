using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class OriginAccountingVoucher
    {
        public OriginAccountingVoucher()
        {
            OriginAccountingVoucherDetails = new HashSet<OriginAccountingVoucherDetails>();
            OriginAccountingVoucherDetailsSum = new HashSet<OriginAccountingVoucherDetailsSum>();
        }

        public int Id { get; set; }
        public int? VoucherFormId { get; set; }
        public string VoucherNo { get; set; }
        public int? CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public int? ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public int? PayerId { get; set; }
        public int? DepartmentId { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string Reason { get; set; }
        public int? CashierId { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Attach { get; set; }
        public string OriginRefNo { get; set; }
        public bool LockOriginRefNo { get; set; }
        public string OriginRefNum { get; set; }
        public DateTime? OriginRefCreatedDate { get; set; }
        public DateTime? OriginRefDate { get; set; }
        public int? OriginRefCreatedBy { get; set; }
        public int? DebitAccountNo { get; set; }
        public int? CreditAccountNo { get; set; }
        public int? CurrencyId { get; set; }
        public int? ForeignCurrencyId { get; set; }
        public decimal? TotalAmountBeforeTax { get; set; }
        public decimal? TotalAmountTax { get; set; }
        public decimal? TotalAmountAfterTax { get; set; }
        public decimal? TotalAmountBeforeTaxCy { get; set; }
        public decimal? TotalAmountTaxCy { get; set; }
        public decimal? TotalAmountAfterTaxCy { get; set; }
        public int? AdvanceId { get; set; }
        public int? BankId { get; set; }
        public int? ReceiverBankId { get; set; }
        public string ReceiverBankName { get; set; }
        public string AccountNum { get; set; }
        public string ReceiverAccountNum { get; set; }
        public string AddressBank { get; set; }
        public string ReceiverAddressBank { get; set; }
        public string ReceiverSwiftCode { get; set; }
        public string Note { get; set; }
        public int? PartnerId { get; set; }
        public int? PartnerDebtId { get; set; }
        public int? ItemId { get; set; }
        public int? TaxExpenseItemId { get; set; }
        public string TaxCode { get; set; }
        public int? TaxCustomerId { get; set; }
        public string Items { get; set; }
        public string BankOriginRefNo { get; set; }
        public DateTime? BankOriginRefDate { get; set; }
        public string BankName { get; set; }
        public string VATInvoiceNo { get; set; }
        public int? VATInvoiceID { get; set; }
        public DateTime? VATInvoiceDate { get; set; }
        public int? PaymentMethod { get; set; }
        public decimal? RealTotalAmountBeforeTax { get; set; }
        public decimal? RealTotalAmountTax { get; set; }
        public decimal? RealTotalAmountTaxPlus { get; set; }
        public decimal? RealTotalAmountAfterTax { get; set; }
        public int? RealDebitAccountNo { get; set; }
        public int? RealCreditAccountNo { get; set; }
        public decimal? VarianceTotalAmountBeforeTax { get; set; }
        public decimal? VarianceTotalAmountTax { get; set; }
        public decimal? Vat { get; set; }
        public string OriginRefFiles { get; set; }
        public bool INVLocked { get; set; }
        public bool InvDemage { get; set; }
        public DateTime? SignDate { get; set; }
        public decimal? FreightPrice { get; set; }
        public bool INVLockInsert { get; set; }
        public string AdvanceNo { get; set; }
        public bool IsLocked { get; set; }
        public bool IsTypeEInv { get; set; }
        public decimal? TotalAmountOwed { get; set; }
        public decimal? TotalAmountPaid { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public string FKEYRANDOM { get; set; }

        public virtual ICollection<OriginAccountingVoucherDetails> OriginAccountingVoucherDetails { get; set; }
        public virtual ICollection<OriginAccountingVoucherDetailsSum> OriginAccountingVoucherDetailsSum { get; set; }
    }
}
