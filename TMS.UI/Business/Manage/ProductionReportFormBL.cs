using Core.Clients;
using Core.Components.Forms;
using Core.Extensions;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class ProductionReportFormBL : PopupEditor
    {
        public ProductionReportFormBL() : base(nameof(Transportation))
        {
            Name = "Production Report";
        }

        public async Task ExportExcel()
        {
            var path = await new Client(nameof(Transportation)).PostAsync<string>(null, "ExportProductionReport");
            Client.Download($"/excel/Download/{path.EncodeSpecialChar()}");
            Toast.Success("Xuất file thành công");
        }
    }
}