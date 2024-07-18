namespace Core.Models
{
    public partial class Role
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
    }
}