using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class SettingPolicyListBL : TabEditor
    {
        public SettingPolicyListBL() : base(nameof(SettingPolicy))
        {
            Name = "SettingPolicy List";
        }

        public async Task EditSettingPolicy(SettingPolicy entity)
        {
            await this.OpenPopup(
                featureName: "SettingPolicy Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.SettingPolicyEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa chi phí báo giá";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddSettingPolicy()
        {
            await this.OpenPopup(
                featureName: "SettingPolicy Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.SettingPolicyEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới chi phí báo giá";
                    instance.Entity = new SettingPolicy()
                    {
                        TypeId = 1
                    };
                    return instance;
                });
        }
    }
}