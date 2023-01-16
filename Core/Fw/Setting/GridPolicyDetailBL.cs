using Core.Components.Forms;
using Core.Models;

namespace Core.Fw.Setting
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
