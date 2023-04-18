using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using System;
using TMS.API.Enums;
using Core.Clients;
using Core.ViewModels;

namespace TMS.UI.Business.Manage
{
    public class ContainerBetManagerListBL : TabEditor
    {
        public ContainerBetManagerListBL() : base(nameof(Transportation))
        {
            Name = "ContainerBetManager List";
        }

        public async Task EditTransportation(Transportation entity)
        {
            await this.OpenPopup(
                featureName: "Transportation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa danh sách vận chuyển";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task AddTransportation()
        {
            await this.OpenPopup(
                featureName: "Transportation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới danh sách vận chuyển";
                    instance.Entity = new Transportation();
                    return instance;
                });
        }

        public async Task UpdateBetAmount()
        {
            await this.OpenPopup(
                featureName: "UpdateBetAmount",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Setting.UpdateBetAmountBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa số tiền cược hãng tàu";
                    return instance;
                });
        }

        public async Task LoadTransportationReport()
        {
            var gridView = this.FindComponentByName<GridView>(nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            var gridViewReport = this.FindComponentByName<GridView>("TransportationReport");
            var selecteds = (await gridView.GetRealTimeSelectedRows()).Select(x => x[IdField].ToString()).ToList();
            gridViewReport.DataSourceFilter = $"?$filter=Active eq true and Id in ({selecteds.Combine()}) and ShipDate ne null&$orderby=StartShip desc";
            await gridViewReport.ApplyFilter(true);
        }

        public async Task RequestUnClosing(Transportation transportation, PatchUpdate patch)
        {
            var tran = new TransportationListAccountantBL();
            await tran.RequestUnClosing(transportation, patch, this);
        }
    }
}