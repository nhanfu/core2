using Core.Components.Forms;
using Core.Models;

namespace Core.Fw.Setting
{
    class RoleDetailBL : PopupEditor
    {
        public RoleDetailBL() : base(nameof(Role))
        {
            Entity = new Role();
            Name = "Role Detail";
            Title = "Chi tiết loại quyền hạn";
        }
    }
}
