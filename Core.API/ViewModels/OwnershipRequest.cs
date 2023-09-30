using Core.Enums;
using Core.Models;

namespace Core.ViewModels
{
    public class OwnershipRequest
    {
        public Entity EntityType { get; set; }
        public string[] RecordIds { get; set; }
    }
}
