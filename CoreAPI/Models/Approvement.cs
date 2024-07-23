namespace Core.Models
{
    public partial class Approvement
    {
        public string Id { get; set; }
        public string ReasonOfChange { get; set; }
        public string Name { get; set; }
        public string RecordId { get; set; }
        public int? StatusId { get; set; }
        public string UserApproveId { get; set; }
        public bool IsEnd { get; set; }
        public int? NextLevel { get; set; }
        public int? CurrentLevel { get; set; }
        public bool Approved { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public bool Active { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}
