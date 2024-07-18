namespace Core.Models
{
    public partial class UserRole
    {
        public string Id { get; set; }

        public string TenantCode { get; set; }

        public string UserId { get; set; }

        public string RoleId { get; set; }

        public bool Active { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime? ExpiredDate { get; set; }

        public DateTime InsertedDate { get; set; }

        public string InsertedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Role Role { get; set; }

        public virtual User User { get; set; }
    }
}