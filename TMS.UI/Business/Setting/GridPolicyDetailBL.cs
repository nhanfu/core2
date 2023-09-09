using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Setting
{
    class GridPolicyDetailBL : PopupEditor
    {
        public GridPolicyDetailBL() : base(nameof(Component))
        {
            Entity = new Component();
            Name = "GridDetail";
            Title = "Grid Detail";
        }
    }
}
