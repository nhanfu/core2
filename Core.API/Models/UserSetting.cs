using System;

namespace Core.Models
{
    public partial class UserSetting
    {
        public int Id { get; set; }
        public int? RoleId { get; set; }
        public int? UserId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public int? ParentId { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual Role Role { get; set; }

        internal object ToDictionary(Func<object, object> p)
        {
            throw new NotImplementedException();
        }
    }
}
