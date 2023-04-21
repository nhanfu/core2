using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class CommodityListBL : TabEditor
    {
        public CommodityListBL() : base(nameof(MasterData))
        {
            Name = "Commodity List";
        }

        public async Task EditCommodity(MasterData entity)
        {
            await this.OpenPopup(
                featureName: "Commodity Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.CommodityEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa vật tư hàng hóa";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddCommodity()
        {
            GridView gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (gridView.Name.Contains("CommodityLevel2"))
            {
                await this.OpenPopup(
                featureName: "Commodity Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.CommodityEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới vật tư hàng hóa";
                    instance.Entity = new MasterData() { Path = @"\7651\" };
                    return instance;
                });
            }
            else
            {
                await this.OpenPopup(
                featureName: "Commodity Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.CommodityEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới vật tư hàng hóa";
                    instance.Entity = new MasterData() { ParentId = 7651, Path = @"\7651\" };
                    return instance;
                });
            }
        }

        public void BeforeMasterData(MasterData masterData)
        {
            masterData.ParentId = 7651;
            masterData.Path = @"\7651\";
            masterData.Level = 1;
        }
    }
}
