namespace Core.Models
{
    public partial class ApprovalConfig
    {
        public string Id { get; set; }
        public int? Level { get; set; }
        public string TitleSend { get; set; }
        public string DescriptionSend { get; set; }
        public string TitleApproved { get; set; }
        public string DescriptionApproved { get; set; }
        public string TitleDecline { get; set; }
        public string DescriptionDecline { get; set; }
        public string UserId { get; set; }
        public string RoleId { get; set; }
        public string TableName { get; set; }
        public string SqlSendUser { get; set; }
        public string SqlApprovedUser { get; set; }
        public string SqlRecord { get; set; }
        public bool Active { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}
