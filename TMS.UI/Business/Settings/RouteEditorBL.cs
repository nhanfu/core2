using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class RouteEditorBL : PopupEditor
    {
        public RouteEditorBL() : base(nameof(Route))
        {
            Name = "Route Editor";
        }

    }
}
