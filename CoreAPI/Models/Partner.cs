namespace Core.Models
{
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
        public string CreatedBy { get; set; }
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
        public string DebitAccountId { get; set; }
        public bool IsInternal { get; set; }
        public bool IsIndividual { get; set; }
        public string Web { get; set; }
        public int? SeqKey { get; set; }
        public string SaleId { get; set; }
        public string Attachment { get; set; }
        public string Email { get; set; }
        public string SourseId { get; set; }
        public string RaitingId { get; set; }
        public DateTime? Dob { get; set; }
        public string PicId { get; set; }
    }
}