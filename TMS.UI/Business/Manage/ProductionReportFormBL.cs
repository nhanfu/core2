using Core.Clients;
using Core.Components.Forms;
using Core.Extensions;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.UI.Business.Manage
{
    public class ProductionReportFormBL : PopupEditor
    {
        public ReportGroupVM EReportGroupVM => Entity as ReportGroupVM;
        public ProductionReportFormBL() : base(nameof(Transportation))
        {
            Name = "Production Report";
            Entity = new ReportGroupVM();
        }

        public async Task ExportExcel()
        {
            if (!await IsFormValid())
            {
                return;
            }
            var path = await new Client(nameof(Transportation)).PostAsync<string>(EReportGroupVM, "ExportProductionReport");
            Client.Download($"/excel/Download/{path.EncodeSpecialChar()}");
            Toast.Success("Xuất file thành công");
        }

        public async Task ExportTruckMaintenance()
        {
            if (!await IsFormValid())
            {
                return;
            }
            var path = await new Client(nameof(Transportation)).PostAsync<string>(EReportGroupVM, "ExportTruckMaintenance");
            Client.Download($"/excel/Download/{path.EncodeSpecialChar()}");
            Toast.Success("Xuất file thành công");
        }
    }
}