using System;

namespace Core.Models
{
    public partial class ComponentGroup
    {
        public int ItemInRow { get; set; }
    }

    public partial class Entity
    {
        public Type GetEntityType(string ns = null)
        {
            var name = AliasFor ?? Name;
            return Type.GetType((ns ?? Namespace ?? Clients.Client.ModelNamespace) + name);
        }
    }
}
