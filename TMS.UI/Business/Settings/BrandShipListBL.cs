using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class BrandShipListBL : TabEditor
    {
        public BrandShipListBL() : base(nameof(BrandShip))
        {
            Name = "BrandShip List";
        }

        public async Task EditBrandShip(BrandShip entity)
        {
            await this.OpenPopup(
                featureName: "BrandShip Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.BrandShipEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa hãng tàu";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddBrandShip()
        {
            await this.OpenPopup(
                featureName: "BrandShip Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.BrandShipEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới hãng tàu";
                    instance.Entity = new BrandShip();            
                    return instance;
                });
        }
    }
}
