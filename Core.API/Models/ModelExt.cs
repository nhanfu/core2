using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public partial class Component
    {
        [NotMapped]
        public bool IsPivot { get; set; }
    }
}
