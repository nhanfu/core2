using System;
using System.Threading.Tasks;
using Core.Models;
using Core.Components.Extensions;
using Core.Components.Forms;

namespace Core.Fw.Setting
{
    public class MasterDataBL : TabEditor
    {
        public MasterDataBL() : base(nameof(MasterData))
        {
            Name = "Master Data";
            Title = Name;
        }

        public async Task EditMasterData(MasterData masterData)
        {
            await this.OpenPopup(
                featureName: "MasterData Detail",
                factory: () =>
                {
                    var type = Type.GetType("Core.Fw.Setting.MasterDataDetailsBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = masterData is null ? "Thêm tham chiếu mới" : "Cập nhật tham chiếu";
                    instance.Entity = masterData ?? new MasterData();
                    return instance;
                });
        }
    }
}
