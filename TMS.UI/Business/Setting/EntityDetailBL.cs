using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Setting
{
    class EntityDetailBL : PopupEditor
    {
        public EntityDetailBL() : base(nameof(TMS.API.Models.Entity))
        {
            Entity = new Entity();
            Name = "Entity Detail";
            Title = "Entity Detail";
        }
    }
}
