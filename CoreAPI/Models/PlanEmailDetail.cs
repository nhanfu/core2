namespace CoreAPI.Models
{
    public class PlanEmailDetail
    {
        public string Id { get; set; }
        public string PlanEmailId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? StatusId { get; set; }
        public string ResposeBody { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public bool Active { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}
