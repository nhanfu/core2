using ClosedXML.Excel;
using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using TMS.API.Models;
using Windows.UI.Xaml;

namespace TMS.API.Controllers
{
    public class BookingListController : TMSController<BookingList> 
    {
        public BookingListController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<BookingList>> PatchAsync([FromQuery] ODataQueryOptions<BookingList> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
            var entity = await db.BookingList.FindAsync(idInt); 
            if (patch.Changes.Any(x => x.Field == nameof(entity.Submit)) == false)
            {
                if (entity.Submit)
                {
                    throw new ApiException("Danh sách book tàu này đã bị khóa !!!") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("Default")))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Transaction = transaction;
                        command.Connection = connection;
                        var updates = patch.Changes.Where(x => x.Field != IdField).ToList();
                        var update = updates.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
                        if (disableTrigger)
                        {
                            command.CommandText += $" DISABLE TRIGGER ALL ON [{nameof(BookingList)}];";
                        }
                        else
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(BookingList)}];";
                        }
                        command.CommandText += $" UPDATE [{nameof(BookingList)}] SET {update.Combine()} WHERE Id = {idInt};";
                        //
                        if (disableTrigger)
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(BookingList)}];";
                        }
                        foreach (var item in updates)
                        {
                            command.Parameters.AddWithValue($"@{item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                        }
                        command.ExecuteNonQuery();
                        transaction.Commit();
                        await db.Entry(entity).ReloadAsync();
                        return entity;
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return entity;
                }
            }
        }

        [HttpPost("api/[Controller]/CreateAllBookingList")]
        public async Task<bool> CreateAllBookingList([FromBody] BookingList entity)
        {
            var query = db.Transportation.Where(x => x.BookingListId == null
            && x.BookingId != null).AsQueryable();
            var rs = await query.AsNoTracking().ToListAsync();
            var groupByPercentileQuery = (from tran in rs
                                          group tran by new
                                          {
                                              tran.ClosingDate.Value.Month,
                                              tran.ClosingDate.Value.Year,
                                              tran.RouteId,
                                              tran.BrandShipId,
                                              tran.ExportListId,
                                              tran.ShipId,
                                              tran.LineId,
                                              tran.SocId,
                                              tran.Trip,
                                              StartShip = tran.StartShip.Value.Date,
                                              tran.ContainerTypeId,
                                              tran.PolicyId,
                                              ShipUnitPrice = tran.ShipPrice ?? tran.ShipUnitPrice
                                          } into tranGroup
                                          select new BookingList()
                                          {
                                              Month = tranGroup.Key.Month,
                                              Year = tranGroup.Key.Year,
                                              ExportListId = tranGroup.Key.ExportListId,
                                              RouteId = tranGroup.Key.RouteId,
                                              BrandShipId = tranGroup.Key.BrandShipId,
                                              ShipId = tranGroup.Key.ShipId,
                                              LineId = tranGroup.Key.LineId,
                                              SocId = tranGroup.Key.SocId,
                                              Trip = tranGroup.Key.Trip,
                                              StartShip = tranGroup.Key.StartShip,
                                              ContainerTypeId = tranGroup.Key.ContainerTypeId,
                                              PolicyId = tranGroup.Key.PolicyId,
                                              Count = tranGroup.Count(),
                                              ShipUnitPrice = tranGroup.Key.ShipUnitPrice ?? 0,
                                              ShipPrice = tranGroup.Where(x => x.ShipUnitPrice != null).Sum(x => x.ShipUnitPrice.Value),
                                              ShipPolicyPrice = tranGroup.Where(x => x.ShipPolicyPrice != null).Sum(x => x.ShipPolicyPrice.Value),
                                              TransportationIds = tranGroup.Select(x => x.Id).Combine()
                                          }).ToList();
            SetAuditInfo(groupByPercentileQuery);
            db.AddRange(groupByPercentileQuery);
            await db.SaveChangesAsync();
            foreach (var item in groupByPercentileQuery)
            {
                var trans = await db.Transportation.Where(x => item.TransportationIds.Contains(x.Id.ToString())).ToListAsync();
                trans.ForEach(x => x.BookingListId = item.Id);
                await db.SaveChangesAsync();
            }
            return true;
        }

        [HttpPost("api/[Controller]/UpdateBookingList")]
        public async Task<bool> UpdateBookingList([FromBody] BookingList entity)
        {
            var rs = await db.Transportation.Where(x => x.StartShip.Value.Date >= entity.FromDate.Value.Date && x.StartShip.Value.Date <= entity.ToDate.Value.Date && x.BookingId != null).ToListAsync();
            if (rs == null)
            {
                return false;
            }
            var groupByPercentileQuery = (from tran in rs
                                          group tran by new
                                          {
                                              Month = tran.ClosingDate != null ? tran.ClosingDate.Value.Month : 0,
                                              Year = tran.ClosingDate != null ? tran.ClosingDate.Value.Year : 0,
                                              tran.RouteId,
                                              tran.BrandShipId,
                                              tran.ExportListId,
                                              tran.ShipId,
                                              tran.LineId,
                                              tran.SocId,
                                              tran.Trip,
                                              StartShip = tran.StartShip.Value.Date,
                                              tran.ContainerTypeId,
                                              tran.PolicyId,
                                              ShipUnitPrice = tran.ShipPrice ?? tran.ShipUnitPrice
                                          } into tranGroup
                                          select new BookingList()
                                          {
                                              Month = tranGroup.Key.Month,
                                              Year = tranGroup.Key.Year,
                                              ExportListId = tranGroup.Key.ExportListId,
                                              RouteId = tranGroup.Key.RouteId,
                                              BrandShipId = tranGroup.Key.BrandShipId,
                                              ShipId = tranGroup.Key.ShipId,
                                              LineId = tranGroup.Key.LineId,
                                              SocId = tranGroup.Key.SocId,
                                              Trip = tranGroup.Key.Trip,
                                              StartShip = tranGroup.Key.StartShip,
                                              ContainerTypeId = tranGroup.Key.ContainerTypeId,
                                              PolicyId = tranGroup.Key.PolicyId,
                                              Count = tranGroup.Count(),
                                              ShipUnitPrice = tranGroup.Key.ShipUnitPrice ?? 0,
                                              ShipPrice = tranGroup.Where(x => x.ShipUnitPrice != null).Sum(x => x.ShipUnitPrice.Value),
                                              ShipPolicyPrice = tranGroup.Where(x => x.ShipPolicyPrice != null).Sum(x => x.ShipPolicyPrice.Value),
                                              TransportationIds = tranGroup.Select(x => x.Id).Combine()
                                          }).ToList();
            SetAuditInfo(groupByPercentileQuery);
            var listAdd = new List<int>();
            var tranIds = groupByPercentileQuery.Select(x => x.TransportationIds).ToList().Combine();
            var bookingList = await db.BookingList.Where(x => x.Active && x.StartShip.Value.Date >= entity.FromDate.Value.Date && x.StartShip.Value.Date <= entity.ToDate.Value.Date).ToListAsync();
            foreach (var item in groupByPercentileQuery)
            {
                var checkBookingList = bookingList.Where(x =>
                x.Month == item.Month
                && x.Year == item.Year
                && x.RouteId == item.RouteId
                && x.ShipId == item.ShipId
                && x.LineId == item.LineId
                && x.BrandShipId == item.BrandShipId
                && x.Trip == item.Trip
                && x.StartShip == item.StartShip
                && x.ContainerTypeId == item.ContainerTypeId
                && x.SocId == item.SocId
                && x.ExportListId == item.ExportListId
                && x.PolicyId == item.PolicyId
                && x.ShipUnitPrice == item.ShipUnitPrice
                && x.Active).FirstOrDefault();
                if (checkBookingList == null)
                {
                    bookingList.Add(item);
                    db.Add(item);
                    await db.SaveChangesAsync();
                    listAdd.Add(item.Id);
                }
                else
                {
                    if ((item.Count != checkBookingList.Count || item.ShipPrice != checkBookingList.ShipPrice || item.ShipPolicyPrice != checkBookingList.ShipPolicyPrice) && checkBookingList.Submit == false)
                    {
                        checkBookingList.Count = item.Count;
                        checkBookingList.ShipPrice = item.ShipPrice;
                        checkBookingList.ShipPolicyPrice = item.ShipPolicyPrice;
                    }
                    else if (checkBookingList.Submit && (int)Math.Abs((decimal)(checkBookingList.Count - item.Count)) > 0)
                    {
                        var newBookingList = new BookingList();
                        newBookingList.CopyPropFrom(checkBookingList);
                        newBookingList.Id = 0;
                        newBookingList.Submit = false;
                        newBookingList.Count = (int)Math.Abs((decimal)(checkBookingList.Count - item.Count));
                        newBookingList.ShipPrice = Math.Abs(checkBookingList.ShipPrice - item.ShipPrice);
                        newBookingList.ShipPolicyPrice = Math.Abs(checkBookingList.ShipPolicyPrice - item.ShipPolicyPrice);
                        db.Add(newBookingList);
                        await db.SaveChangesAsync();
                        listAdd.Add(newBookingList.Id);
                    }
                    listAdd.Add(checkBookingList.Id);
                }
                var trans = rs.Where(x => item.TransportationIds.Contains(x.Id.ToString())).ToList();
                trans.ForEach(x => x.BookingListId = checkBookingList is null ? item.Id : checkBookingList.Id);
            }
            if (listAdd.Count > 0)
            {
                var bookingListSuperfluous = await db.BookingList.Where(x => x.Submit == false && x.StartShip.Value.Date >= entity.FromDate.Value.Date && x.StartShip.Value.Date <= entity.ToDate.Value.Date && !listAdd.Any(y => y == x.Id) && x.Active).ToListAsync();
                if (bookingListSuperfluous.Count > 0)
                {
                    bookingListSuperfluous.ForEach(x => { x.Active = false; });
                }
            }
            foreach (var item in bookingList)
            {
                var check = rs.Where(x => x.BookingListId == item.Id).Any();
                if (check == false)
                {
                    item.Active = false;
                }
            }
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.SaveChangesAsync();
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
            return true;
        }

        [HttpPost("api/BookingList/ExportExcelReportDataByFilter")]
        public async Task<string> ExportExcelReportDataByFilter([FromBody] List<BookingList> bookingLists)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(nameof(Transportation));
            worksheet.Style.Font.SetFontName("Times New Roman");
            worksheet.Column(1).Width = 5;
            worksheet.Column(1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Column(1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Column(1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Column(1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Column(2).Width = 35;
            worksheet.Column(2).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Column(2).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Column(2).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Column(2).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Column(3).Width = 15;
            worksheet.Column(3).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Column(3).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Column(3).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Column(3).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Column(4).Width = 15;
            worksheet.Column(4).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Column(4).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Column(4).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Column(4).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Column(5).Width = 15;
            worksheet.Column(5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Column(5).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Column(5).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Column(5).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Column(6).Width = 15;
            worksheet.Column(6).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Column(6).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Column(6).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Column(6).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Column(7).Width = 15;
            worksheet.Column(7).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Column(7).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Column(7).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Column(7).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Column(8).Width = 15;
            worksheet.Column(8).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Column(8).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Column(8).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Column(8).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Cell("A1").Value = $"STT";
            worksheet.Cell("A1").Style.Alignment.WrapText = true;
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("A1:B2").Column(1).Merge();
            worksheet.Cell("B1").Value = $"Tuyến vận chuyển";
            worksheet.Cell("B1").Style.Alignment.WrapText = true;
            worksheet.Cell("B1").Style.Font.Bold = true;
            worksheet.Cell("B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("B1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("B1:C2").Column(1).Merge();

            worksheet.Cell("C1").Value = $"Sản lượng";
            worksheet.Cell("C1").Style.Alignment.WrapText = true;
            worksheet.Cell("C1").Style.Font.Bold = true;
            worksheet.Cell("C1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("C1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("C1:D1").Row(1).Merge();

            worksheet.Cell("E1").Value = $"Thành tiền";
            worksheet.Cell("E1").Style.Alignment.WrapText = true;
            worksheet.Cell("E1").Style.Font.Bold = true;
            worksheet.Cell("E1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("E1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("E1:F1").Row(1).Merge();
            
            worksheet.Cell("G1").Value = $"Giá trung bình";
            worksheet.Cell("G1").Style.Alignment.WrapText = true;
            worksheet.Cell("G1").Style.Font.Bold = true;
            worksheet.Cell("G1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("G1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("G1:H1").Row(1).Merge();

            worksheet.Cell("C2").Value = $"Cont 20";
            worksheet.Cell("C2").Style.Alignment.WrapText = true;
            worksheet.Cell("C2").Style.Font.Bold = true;
            worksheet.Cell("C2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("C2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell("D2").Value = $"Cont 40";
            worksheet.Cell("D2").Style.Alignment.WrapText = true;
            worksheet.Cell("D2").Style.Font.Bold = true;
            worksheet.Cell("D2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("D2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell("E2").Value = $"Cont 20";
            worksheet.Cell("E2").Style.Alignment.WrapText = true;
            worksheet.Cell("E2").Style.Font.Bold = true;
            worksheet.Cell("E2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("E2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell("F2").Value = $"Cont 40";
            worksheet.Cell("F2").Style.Alignment.WrapText = true;
            worksheet.Cell("F2").Style.Font.Bold = true;
            worksheet.Cell("F2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("F2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell("G2").Value = $"Cont 20";
            worksheet.Cell("G2").Style.Alignment.WrapText = true;
            worksheet.Cell("G2").Style.Font.Bold = true;
            worksheet.Cell("G2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("G2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell("H2").Value = $"Cont 40";
            worksheet.Cell("H2").Style.Alignment.WrapText = true;
            worksheet.Cell("H2").Style.Font.Bold = true;
            worksheet.Cell("H2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("H2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            var routeIds = bookingLists.Select(y => y.RouteId).ToList();
            var routes = await db.Route.Where(x => routeIds.Contains(x.Id)).ToListAsync();
            var index = 3;
            bookingLists.Reverse();
            foreach (var item in bookingLists)
            {
                worksheet.Cell("A" + index).SetValue(index - 2);
                worksheet.Cell("B" + index).SetValue(routes.Where(x => x.Id == item.RouteId).FirstOrDefault().Name);
                worksheet.Cell("C" + index).SetValue(item.TotalCountCont20);
                if (item.TotalCountCont20 > 0) { worksheet.Cell("C" + index).Style.NumberFormat.Format = "#,##"; }
                worksheet.Cell("D" + index).SetValue(item.TotalCountCont40);
                if (item.TotalCountCont40 > 0) { worksheet.Cell("D" + index).Style.NumberFormat.Format = "#,##"; }
                worksheet.Cell("E" + index).SetValue(item.TotalTotalPriceCont20);
                if (item.TotalTotalPriceCont20 > 0) { worksheet.Cell("E" + index).Style.NumberFormat.Format = "#,##"; }
                worksheet.Cell("F" + index).SetValue(item.TotalTotalPriceCont40);
                if (item.TotalTotalPriceCont40 > 0) { worksheet.Cell("F" + index).Style.NumberFormat.Format = "#,##"; }
                worksheet.Cell("G" + index).SetValue(item.AVGTotalPriceCont20);
                if (item.AVGTotalPriceCont20 > 0) { worksheet.Cell("G" + index).Style.NumberFormat.Format = "#,##"; }
                worksheet.Cell("H" + index).SetValue(item.AVGTotalPriceCont40);
                if (item.AVGTotalPriceCont40 > 0) { worksheet.Cell("H" + index).Style.NumberFormat.Format = "#,##"; }
                index++;
            }
            var url = $"Báo cáo.xlsx";
            workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
            return url;
        }

        public async Task<List<List<Dictionary<string, object>>>> ConverSqlToDataSet(string reportQuery)
        {
            var connectionStr = _config.GetConnectionString("Default");
            using var con = new SqlConnection(connectionStr);
            var sqlCmd = new SqlCommand(reportQuery, con)
            {
                CommandType = CommandType.Text
            };
            con.Open();
            var tables = new List<List<Dictionary<string, object>>>();
            using (var reader = await sqlCmd.ExecuteReaderAsync())
            {
                do
                {
                    var table = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        table.Add(Read(reader));
                    }
                    tables.Add(table);
                } while (reader.NextResult());
            }
            return tables;
        }

        public IEnumerable<PropertyInfo> GetVariance(Expense change, Expense cutting)
        {
            foreach (PropertyInfo pi in change.GetType().GetProperties())
            {
                object valuechange = typeof(Expense).GetProperty(pi.Name).GetValue(change);
                object valuecutting = typeof(Expense).GetProperty(pi.Name).GetValue(cutting);
                valuechange = valuechange is null ? "NULL" : valuechange;
                valuecutting = valuecutting is null ? "NULL" : valuecutting;
                if (!valuechange.Equals(valuecutting))
                { yield return pi; }
            }
        }
    }
}
