using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using TMS.API.Models;

namespace TMS.UI.Business.Setting
{
    public class MasterDataBL : TabEditor
    {
        public MasterDataBL() : base(nameof(MasterData))
        {
            Name = "Master Data";
        }

        public void EditMasterData(MasterData masterData)
        {
            var task = this.OpenPopup(
                featureName: "MasterData Detail",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Setting.MasterDataDetailsBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm tham chiếu mới";
                    instance.Entity = masterData ?? new MasterData();
                    return instance;
                });
            Client.ExecTask(task);
        }

        public void CreateMasterData()
        {
            EditMasterData(null);
        }

        public void UpdatePath()
        {
            var task = new Client(nameof(MasterData)).PostAsync<bool>(null, $"UpdatePath");
            Client.ExecTask(task);
        }

        public void EditMasterDataParent(MasterData parent)
        {
            var masterDataTask = new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>(
                $"?$filter=Id eq '{parent.ParentId}'");
            Client.ExecTask(masterDataTask, masterData =>
            {
                var task = this.OpenPopup(
                featureName: "MasterData Detail",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Setting.MasterDataDetailsBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Update tham chiếu";
                    instance.Entity = masterData;
                    return instance;
                });
                Client.ExecTask(task);
            });
        }
    }
}
