using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public partial class Feature
    {
        [NotMapped]
        public ICollection<Component> GridPolicy { get; set; } = new List<Component>();
    }
}
