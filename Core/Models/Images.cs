using System;

namespace Core.Models
{
    public partial class Images
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string Url { get; set; }
        public bool IsAvatar { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}
