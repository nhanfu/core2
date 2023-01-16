using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Setting
{
    class GridPolicyDetailBL : PopupEditor
    {
        public GridPolicyDetailBL() : base(nameof(GridPolicy))
        {
            Entity = new GridPolicy();
            Name = "GridDetail";
            Title = "Grid Detail";
        }
    }
}
