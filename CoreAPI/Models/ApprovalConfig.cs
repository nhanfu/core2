namespace Core.Models
{
    public partial class ApprovalConfig
    {
        public string Id { get; set; }
        public int? VoucherTypeId { get; set; }
        public int? Level { get; set; }
        public string ParentId { get; set; }
        public string NameId { get; set; }
        public string UserIds { get; set; }
        public string RoleIds { get; set; }
        public bool Active { get; set; }
        public bool IsDepartment { get; set; }
        public bool IsTeam { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}
