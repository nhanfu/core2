using System;
using System.Threading.Tasks;
using TMS.API.Models;
using Core.Components.Extensions;
using Core.Components.Forms;

namespace TMS.UI.Business.Setting
{
    public class SettingDataBL : TabEditor
    {
        public SettingDataBL() : base(nameof(Entity))
        {
            Name = "Data List";
            Title = Name;
        }

        public async Task EditEntity(Entity entity)
        {
            await this.OpenPopup(
                featureName: "EntityDetail",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Setting.EntityDetailBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = entity is null ? "Thêm cot du lieu moi" : "Update cot du lieu";
                    instance.Entity = entity ?? new Entity();
                    return instance;
                });
        }

        public async Task EditGridPolicy(GridPolicy gridPolicy)
        {
            await this.OpenPopup(
                featureName: "GridDetail",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Setting.GridPolicyDetailBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = gridPolicy is null ? "Thêm cot du lieu moi" : "Update cot du lieu";
                    instance.Entity = gridPolicy ?? new GridPolicy();
                    return instance;
                });
        }
    }
}
