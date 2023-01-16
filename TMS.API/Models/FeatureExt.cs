using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.API.Models
{
    public partial class Feature
    {
        [NotMapped]
        public ICollection<Component> Component { get; set; } = new List<Component>();
    }
}
