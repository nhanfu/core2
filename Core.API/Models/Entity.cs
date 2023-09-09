using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class Entity
    {
        public Entity()
        {
            Component = new HashSet<Component>();
            Feature = new HashSet<Feature>();
            ComponentEntity = new HashSet<Component>();
            ComponentReference = new HashSet<Component>();
            TaskNotification = new HashSet<TaskNotification>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AliasFor { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public string RefDetailClass { get; set; }
        public string RefListClass { get; set; }
        public string Namespace { get; set; }

        public virtual ICollection<Component> Component { get; set; }
        public virtual ICollection<Feature> Feature { get; set; }
        public virtual ICollection<Component> ComponentEntity { get; set; }
        public virtual ICollection<Component> ComponentReference { get; set; }
        public virtual ICollection<TaskNotification> TaskNotification { get; set; }
    }
}
