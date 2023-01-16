using Core.Components.Forms;
using Core.Models;

namespace Core.Fw.Setting
{
    public class MasterDataDetailsBL: PopupEditor
    {
        public MasterData masEntity => Entity as MasterData;

        public MasterDataDetailsBL() : base(nameof(MasterData))
        {
            Name = "MasterData Detail";
            Title = "MasterData Detail";
            Entity = new MasterData();
        }
    }
}
