namespace Core.Models
{
    public partial class User
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string CompanyId { get; set; }
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
        public int? TypeId { get; set; }
        public int? LoginFailedCount { get; set; }
        public DateTime? LastFailedLogin { get; set; }
        public DateTime? LastLogin { get; set; }
        public string Recover { get; set; }
        public DateTime? JointDate { get; set; }
        public string RoleIds { get; set; }
        public string RoleIdsText { get; set; }
        public string DepartmentId { get; set; }
        public DateTime? IdentityCardDate { get; set; }
        public string IdentityCard { get; set; }
        public string PlaceIssue { get; set; }
        public string NickName { get; set; }
        public string Knowledge { get; set; }
        public decimal? TargetLocalCurr { get; set; }
        public decimal? TargetUSDCurr { get; set; }
        public string TypeBonusId { get; set; }
        public decimal? SalaryAmount { get; set; }
        public decimal? SalaryCoefficient { get; set; }
        public decimal? InsuranceAmount { get; set; }
        public string TypeContractId { get; set; }
        public int? Dependents { get; set; }
        public string AccNumber { get; set; }
        public string AccName { get; set; }
        public string BankId { get; set; }
        public string PassEmail { get; set; }
        public string Ip { get; set; }
        public Partner Company { get; set; }
        public bool IsDepartment { get; set; }
        public bool IsTeam { get; set; }
        public string PositionId { get; set; }
    }
}