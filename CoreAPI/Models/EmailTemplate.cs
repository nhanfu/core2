namespace CoreAPI.Models
{
    public class EmailTemplate
    {
        public string Id { get; set; }
        public string FeatureId { get; set; }
        public DateTime? DailyDate { get; set; }
        public int? ReminderSettingId { get; set; }
        public int? NotificationNumber { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Template { get; set; }
        public DateTime? LastNotificationDate { get; set; }
        public bool Active { get; set; } = false;
        public string InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
