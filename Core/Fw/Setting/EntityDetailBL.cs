using Core.Components.Forms;
using Core.Models;

namespace Core.Fw.Setting
{
    class EntityDetailBL : PopupEditor
    {
        public EntityDetailBL() : base(nameof(Core.Models.Entity))
        {
            Entity = new Entity();
            Name = "Entity Detail";
            Title = "Entity Detail";
        }
    }
}
