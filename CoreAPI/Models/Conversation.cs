namespace Core.Models
{
    public partial class Conversation
    {
        public string Id { get; set; }
        public string Icon { get; set; }
        public string RecordId { get; set; }
        public string EntityId { get; set; }
        public string UserIds { get; set; }
        public string Message { get; set; }
        public string FormatChat { get; set; }
        public bool Active { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string FeatureName { get; set; }
        public string Label { get; set; }
    }
}