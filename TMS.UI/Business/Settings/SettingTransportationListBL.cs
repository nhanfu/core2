using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class SettingTransportationListBL : TabEditor
    {
        public SettingTransportationListBL() : base(nameof(SettingTransportation))
        {
            Name = "SettingTransportation List";
        }

        public async Task EditSettingTransportation(SettingTransportation entity)
        {
            await this.OpenPopup(
                featureName: "SettingTransportation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.SettingTransportationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa cấu hình vận chuyển";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddSettingTransportation()
        {
            await this.OpenPopup(
                featureName: "SettingTransportation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.SettingTransportationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới cấu hình vận chuyển";
                    instance.Entity = new SettingTransportation();            
                    return instance;
                });
        }
    }
}
