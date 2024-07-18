namespace Core.Models
{
    public partial class History
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public string OldValue { get; set; }
        public string ComponentId { get; set; }
        public string RecordId { get; set; }
        public bool Active { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string CreatedBy { get; set; }
    }
}
