using Core.Components.Forms;
using Core.Extensions;
using Core.Models;

namespace Core.Components.Framework
{
    public class HeaderManageBL : PopupEditor
    {
        public Feature FeatureComponent { get; set; }
        public HeaderManageBL() : base(nameof(Component))
        {
            Name = "Header Manage";
            Title = "Header Manage Editor";
        }
    }
}
