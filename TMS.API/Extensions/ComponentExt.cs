using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.API.Models
{
    public partial class GridPolicy
    {
        [NotMapped]
        public string DataSourceOptimized { get; set; }
        [NotMapped]
        public string ReferenceName { get; set; }
    }

    public partial class Component
    {
        [NotMapped]
        public bool IsPivot { get; set; }
    }
}
