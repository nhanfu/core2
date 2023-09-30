using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.API.Models
{
    public partial class Component
    {
        [NotMapped]
        public bool IsPivot { get; set; }
    }
}
