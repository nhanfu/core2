using Core.Components.Forms;
using Core.Models;

namespace Core.Fw.Setting
{
    class ComponentDetailBL : PopupEditor
    {
        public ComponentDetailBL() : base(nameof(Component))
        {
            Entity = new Component();
            Name = "GridDetail";
            Title = "Grid Detail";
        }
    }
}
