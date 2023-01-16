using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class CommodityValueOfCompanyBL : TabEditor
    {
        public CommodityValueOfCompanyBL() : base(nameof(SettingPolicy))
        {
            Name = "CommodityValueOfCompany List";
        }

        public async Task EditCommodityValueOfCompany(SettingPolicy entity)
        {
            await this.OpenPopup(
                featureName: "CommodityValueOfCompany Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.CommodityValueOfCompanyEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa cấu hình GTHH theo mức công ty";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public void SetTypeId(SettingPolicy settingPolicy)
        {
            settingPolicy.TypeId = 3;
        }
    }
}
