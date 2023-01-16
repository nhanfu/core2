using Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Core.Extensions
{
    public class EntityService
    {
        public Dictionary<int, Entity> Entities { get; set; }
        public Entity GetEntity(int id) => Entities[id];
        public Entity GetEntity(string name) => Entities.Values.First(x => x.Name == name);
    }
}
