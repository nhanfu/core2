using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.MVVM;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.UI.Business.Manage
{
    public class ListShipBookBL : TabEditor
    {
        public string idtb => "rp" + GetHashCode();
        public HTMLElement bodyhTMLElement;
        public ListShipBookBL() : base(nameof(Transportation))
        {
            Name = "List Ship Book";
            DOMContentLoaded += async () =>
            {
                await LoadReport();
                /*@
                 const createResizableTable = function (table) {
                    if (table == null) return;
                    const cols = table.querySelectorAll('th');
                    [].forEach.call(cols, function (col) {
                        // Add a resizer element to the column
                        const resizer = document.createElement('div');
                        resizer.classList.add('resizer');

                        // Set the height
                        resizer.style.height = `100%`;

                        col.appendChild(resizer);

                        createResizableColumn(col, resizer);
                    });
                };

                const createResizableColumn = function (col, resizer) {
                    let x = 0;
                    let w = 0;

                    const mouseDownHandler = function (e) {
                        e.preventDefault();
                        x = e.clientX;

                        const styles = window.getComputedStyle(col);
                        w = parseInt(styles.width, 10);

                        document.addEventListener('mousemove', mouseMoveHandler);
                        document.addEventListener('mouseup', mouseUpHandler);

                        resizer.classList.add('resizing');
                    };

                    const mouseMoveHandler = function (e) {
                        e.preventDefault();
                        const dx = e.clientX - x;
                        col.style.width = `${w + dx}px`;
                        col.style.minWidth = `${w + dx}px`;
                        col.style.maxWidth = `0px`;
                    };

                    const mouseUpHandler = function () {
                        resizer.classList.remove('resizing');
                        document.removeEventListener('mousemove', mouseMoveHandler);
                        document.removeEventListener('mouseup', mouseUpHandler);
                    };

                    resizer.addEventListener('mousedown', mouseDownHandler);
                };

                createResizableTable(document.getElementById(this.idtb));*/
            };
        }

        public async Task LoadReport()
        {
            var data = this.FindComponentByName<Section>("Category");
            var element = data.Element;
            element.InnerHTML = "";
            Spinner.AppendTo(element);
            var getReport = await new Client(nameof(Transportation)).PostAsync<List<TranGroupVM>>(Entity, "ReportGroupBy");
            var index = 1;
            var routeIds = getReport.Where(x => x.RouteId != null).Select(x => x.RouteId.Value).ToList();
            var brandShipId = getReport.Where(x => x.BrandShipId != null).Select(x => x.BrandShipId.Value).ToList();
            var lineIds = getReport.Where(x => x.LineId != null).Select(x => x.LineId.Value).ToList();
            var shipIds = getReport.Where(x => x.ShipId != null).Select(x => x.ShipId.Value).ToList();
            var containerTypeIds = getReport.Where(x => x.ContainerTypeId != null).Select(x => x.ContainerTypeId.Value).ToList();
            var policyIds = getReport.Where(x => x.PolicyId != null).Select(x => x.PolicyId.Value).ToList();
            var exportListIds = getReport.Where(x => x.ExportListId != null).Select(x => x.ExportListId.Value).ToList();
            var routeOdata = new Client(nameof(Route)).GetRawListById<Route>(routeIds);
            var brandShipOdata = new Client(nameof(Vendor)).GetRawListById<Vendor>(brandShipId);
            var exportListOdata = new Client(nameof(Vendor)).GetRawListById<Vendor>(exportListIds);
            var lineOdata = new Client(nameof(Route)).GetRawListById<Vendor>(lineIds);
            var shipOdata = new Client(nameof(Ship)).GetRawListById<Ship>(shipIds);
            var containerTypeOdata = new Client(nameof(MasterData)).GetRawListById<MasterData>(containerTypeIds);
            var policyOdata = new Client(nameof(MasterData)).GetRawListById<MasterData>(policyIds);
            await Task.WhenAll(routeOdata, brandShipOdata, lineOdata, shipOdata, containerTypeOdata, policyOdata, exportListOdata);
            var dirroute = routeOdata.Result.ToDictionary(x => x.Id);
            var dirline = lineOdata.Result.ToDictionary(x => x.Id);
            var dirbrandShip = brandShipOdata.Result.ToDictionary(x => x.Id);
            var dirship = shipOdata.Result.ToDictionary(x => x.Id);
            var dircontainerType = containerTypeOdata.Result.ToDictionary(x => x.Id);
            var direxportList = exportListOdata.Result.ToDictionary(x => x.Id);
            var dirpolicy = policyOdata.Result.ToDictionary(x => x.Id);
            var html = Html.Take(element).Div.ClassName("grid-wrapper sticky").Style("max-height: calc(100vh - 231px)").Id(idtb).Div.ClassName("table-wrapper").Table.ClassName("table")
                .Thead
                .TRow
                    .Th.IText("STT").End
                    .Th.Style("width: 70px; min-width: 70px; max-width: 0px;").IText("Tháng").End
                    .Th.Style("width: 70px; min-width: 70px; max-width: 0px;").IText("Năm").End
                    .Th.Style("width: 200px; min-width: 200px; max-width: 0px;").IText("List xuất").End
                    .Th.Style("width: 200px; min-width: 200px; max-width: 0px;").IText("Tuyến vận chuyển").End
                    .Th.Style("width: 200px; min-width: 200px; max-width: 0px;").IText("Hãng tàu").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Line khác").End
                    .Th.Style("width: 200px; min-width: 200px; max-width: 0px;").IText("Tên tàu").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Số chuyến").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Ngày tàu chạy").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Loại Container").End
                    .Th.Style("width: 200px; min-width: 100px; max-width: 0px;").IText("Chính sách").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Số lượng").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Đơn giá cước tàu").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Thành tiền").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Phí khác").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Tổng cộng").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Ghi chú Chính sách").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Số H.Đơn").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Ngày H.Đơn").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Ngày thanh toán").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Phương thức thanh toán").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Ghi chú").End
                    .Th.Style("width: 100px; min-width: 100px; max-width: 0px;").IText("Xác nhận").EndOf(Core.MVVM.ElementType.thead)
                .TBody;
            foreach (var item in getReport)
            {
                var route = dirroute.GetValueOrDefault(item.RouteId ?? 0);
                var brandShip = dirbrandShip.GetValueOrDefault(item.BrandShipId ?? 0);
                var ship = dirship.GetValueOrDefault(item.ShipId ?? 0);
                var containerType = dircontainerType.GetValueOrDefault(item.ContainerTypeId ?? 0);
                var policy = dirpolicy.GetValueOrDefault(item.PolicyId ?? 0);
                var line = dirline.GetValueOrDefault(item.LineId ?? 0);
                var exportList = direxportList.GetValueOrDefault(item.ExportListId ?? 0);
                var price = item.ShipPrice == 0 ? item.ShipUnitPrice : item.ShipPrice;
                html.TRow
                        .TData.ClassName("text-left").IText(index.ToString()).End
                        .TData.ClassName("text-left").IText(item.Month.ToString()).End
                        .TData.ClassName("text-left").IText(item.Year.ToString()).End
                        .TData.ClassName("text-left").IText(exportList is null ? "" : exportList.Name).End
                        .TData.ClassName("text-left").IText(route is null ? "" : route.Name).End
                        .TData.ClassName("text-left").IText(brandShip is null ? "" : brandShip.Name).End
                        .TData.ClassName("text-left").IText(line is null ? "" : line.Name).End
                        .TData.ClassName("text-left").IText(ship is null ? "" : ship.Name).End
                        .TData.ClassName("text-left").IText(item.Trip).End
                        .TData.ClassName("text-left").IText(item.StartShip is null ? "" : item.StartShip.Value.ToString("dd/MM/yyyy")).End
                        .TData.ClassName("text-left").IText(containerType is null ? "" : containerType.Description).End
                        .TData.ClassName("text-left").IText(policy is null ? "" : policy.Description).End
                        .TData.ClassName("text-right").IText(item.Count.ToString()).End
                        .TData.ClassName("text-right").IText(price.ToString("N0")).End
                        .TData.IText((item.Count.Value * price).ToString("N0")).End
                        .TData.IText("").End
                        .TData.IText("").End
                        .TData.IText("").End
                        .TData.IText("").End
                        .TData.IText("").End
                        .TData.IText("").End
                        .TData.IText("").End
                        .TData.IText("").End
                        .TData.IText("").End
                        .TData.IText("").EndOf(Core.MVVM.ElementType.tr);
                index++;
            }
            Spinner.Hide();
        }
    }
}
