namespace Core.Models
{
    public partial class User
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string Avatar { get; set; }
        public string Ssn { get; set; }
        public string PhoneNumber { get; set; }
        public string TeamId { get; set; }
        public string PartnerId { get; set; }
        public bool Active { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? Dob { get; set; }
        public string GenderId { get; set; }
        public DateTime? SsnDate { get; set; }
        public string TaxCode { get; set; }
        public int? SeqKey { get; set; }
        public int? LoginFailedCount { get; set; }
        public DateTime? LastFailedLogin { get; set; }
        public DateTime? LastLogin { get; set; }
        public string Recover { get; set; }
        public string JointDate { get; set; }
        public string RoleIds { get; set; }
        public string RoleIdsText { get; set; }
        public string DepartmentId { get; set; }
    }
}