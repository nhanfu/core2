namespace CoreAPI.Models
{
    public class PartnerCare
    {
        public string Id { get; set; }
        public string PartnerId { get; set; }
        public string TaskName { get; set; }
        public string CustomerName { get; set; }
        public int TypeId { get; set; }
        public int ServiceId { get; set; }
        public string PriorityId { get; set; }
        public string CategoryId { get; set; }
        public DateTime? IssuedDate { get; set; }
        public string ActionId { get; set; }
        public DateTime? Deadline { get; set; }
        public string AssigneeId { get; set; }
        public DateTime? NextDate { get; set; }
        public int? ReminderSettingId { get; set; }
        public int? NotificationNumber { get; set; }
        public string Notes { get; set; }
        public string Attachment { get; set; }
        public bool Active { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? LastNotificationDate { get; set; }
    }
}
