using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class TeusListBL : TabEditor
    {
        public GridView gridView;
        public TeusListBL() : base(nameof(Teus))
        {
            Name = "Teus List";
        }

        public async Task EditTeus(Teus entity)
        {
            await this.OpenPopup(
                featureName: "Teus Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TeusEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa Teus";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddTeus()
        {
            await this.OpenPopup(
                featureName: "Teus Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TeusEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới Teus";
                    instance.Entity = new Teus();
                    return instance;
                });
        }

        public async Task CalcTeus(Teus Teus)
        {
            gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (Teus.Teus20Using > Teus.Teus20)
            {
                Toast.Warning("Số teus20 đóng không được lớn hơn số teus cấp");
                Teus.Teus20Using = Teus.Teus20;
            }
            if (Convert.ToDecimal(Teus.Teus40Using) > Convert.ToDecimal(Teus.Teus40))
            {
                Toast.Warning("Số teus40 đóng không được lớn hơn số teus cấp");
                Teus.Teus40Using = Teus.Teus40;
            }
            Teus.Teus20Remain = Teus.Teus20 - Teus.Teus20Using;
            Teus.Teus40Remain = Teus.Teus40 - Teus.Teus40Using;
            await gridView.AddOrUpdateRow(Teus);
        }
    }
}