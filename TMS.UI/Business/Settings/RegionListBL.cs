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
    public class RegionListBL : TabEditor
    {
        public RegionListBL() : base(nameof(MasterData))
        {
            Name = "Region List";
        }

        public async Task EditRegion(MasterData entity)
        {
            await this.OpenPopup(
                featureName: "Region Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.RegionEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa khu vực";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddRegion()
        {
            GridView gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (gridView.Name.Contains("RegionLevel2"))
            {
                await this.OpenPopup(
                featureName: "Region Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.RegionEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới khu vực";
                    instance.Entity = new MasterData();
                    return instance;
                });
            }
            else
            {
                await this.OpenPopup(
                featureName: "Region Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.RegionEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới khu vực";
                    instance.Entity = new MasterData() { ParentId = 7569 };
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
