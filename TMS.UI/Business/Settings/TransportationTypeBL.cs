using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class TransportationTypeBL : TabEditor
    {
        public TransportationTypeBL() : base(nameof(SettingPolicy))
        {
            Name = "TransportationType List";
        }

        public async Task EditTransportationType(SettingPolicy entity)
        {
            await this.OpenPopup(
                featureName: "TransportationType Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.TransportationTypeEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa cấu hình loại vận chuyển";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddTransportationType()
        {
            await this.OpenPopup(
                featureName: "TransportationType Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.TransportationTypeEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới cấu hình loại vận chuyển";
                    instance.Entity = new BrandShip();            
                    return instance;
                });
        }

        public void SetTypeId(SettingPolicy settingPolicy)
        {
            settingPolicy.TypeId = 2;
        }
    }
}
