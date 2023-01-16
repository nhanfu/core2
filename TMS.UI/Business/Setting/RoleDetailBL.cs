using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Setting
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
