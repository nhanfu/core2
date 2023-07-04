using Aspose.Cells;
using ClosedXML.Excel;
using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Hangfire;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TMS.API.Models;
using TMS.API.Services;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class TransportationController : TMSController<Transportation>
    {
        public readonly TransportationService _transportationService;
        public TransportationController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor, TransportationService transportationService) : base(context, entityService, httpContextAccessor)
        {
            _transportationService = transportationService;
        }

        public override async Task<ActionResult<Transportation>> PatchAsync([FromQuery] ODataQueryOptions<Transportation> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
            var entity = await db.Transportation.FindAsync(idInt);
            if (entity.IsLocked && !patch.Changes.Any(x => x.Field == nameof(entity.ContainerNo)))
            {
                throw new ApiException("DSVC này đã được khóa (Hệ thống). Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
            }


            if (entity.IsLocked && !patch.Changes.Any(x => x.Field == nameof(entity.IsLocked)
                                                    || x.Field == nameof(entity.Notes)
                                                    || x.Field == nameof(entity.IsLockedRevenue)
                                                    || x.Field == nameof(entity.IsSubmit)))
            {
                throw new ApiException("DSVC này đã được khóa (Hệ thống). Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
            }
            if (entity.LockShip && !patch.Changes.Any(x => x.Field == nameof(entity.LockShip)))
            {
                if (patch.Changes.Any(x => (x.Field == nameof(entity.ShipPrice) && x.Value != $"{entity.ShipPrice:N0}")
                || (x.Field == nameof(entity.PolicyId) && x.Value != $"{entity.PolicyId}")
                || (x.Field == nameof(entity.ShipPolicyPrice) && x.Value != $"{entity.ShipPolicyPrice:N0}")
                || (x.Field == nameof(entity.Trip) && x.Value != entity.Trip)
                || (x.Field == nameof(entity.StartShip) && x.Value != $"{entity.StartShip:yyyy/MM/dd hh:mm:ss}")
                || (x.Field == nameof(entity.ContainerTypeId) && x.Value != $"{entity.ContainerTypeId}")
                || (x.Field == nameof(entity.SocId) && x.Value != $"{entity.SocId}")
                || (x.Field == nameof(entity.ShipNotes) && x.Value != entity.ShipNotes)
                || (x.Field == nameof(entity.BookingId) && x.Value != $"{entity.BookingId}")))
                {
                    throw new ApiException("DSVC này đã được khóa (Cước tàu). Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if (entity.IsKt && !patch.Changes.Any(x => x.Field == nameof(entity.IsKt)))
            {
                if (patch.Changes.Any(x => (x.Field == nameof(entity.Trip) && x.Value != entity.Trip)
                || (x.Field == nameof(entity.BookingId) && x.Value != $"{entity.BookingId}")
                || (x.Field == nameof(entity.ContainerTypeId) && x.Value != $"{entity.ContainerTypeId}")
                || (x.Field == nameof(entity.ContainerNo) && x.Value != $"{entity.ContainerNo}")
                || (x.Field == nameof(entity.SealNo) && x.Value != $"{entity.SealNo}")
                || (x.Field == nameof(entity.CommodityId) && x.Value != $"{entity.CommodityId}")
                || (x.Field == nameof(entity.Weight) && x.Value != $"{entity.Weight}")
                || (x.Field == nameof(entity.FreeText2) && x.Value != $"{entity.FreeText2}")
                || (x.Field == nameof(entity.ShipDate) && x.Value != $"{entity.ShipDate:yyyy/MM/dd hh:mm:ss}")
                || (x.Field == nameof(entity.ReturnDate) && x.Value != $"{entity.ReturnDate:yyyy/MM/dd hh:mm:ss}")
                || (x.Field == nameof(entity.ReturnId) && x.Value != $"{entity.ReturnId}")
                || (x.Field == nameof(entity.FreeText3) && x.Value != entity.FreeText3)))
                {
                    throw new ApiException("DSVC này đã được khóa (Khai thác). Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
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
                        if (patch.Changes.Any(x => x.Field == nameof(Transportation.ShipDate) && !x.Value.IsNullOrWhiteSpace()))
                        {
                            patch.Changes.Add(new PatchUpdateDetail() { Field = nameof(Transportation.ExportListReturnId), Value = VendorId.ToString() });
                            patch.Changes.Add(new PatchUpdateDetail() { Field = nameof(Transportation.UserReturnId), Value = UserId.ToString() });
                        }
                        var updates = patch.Changes.Where(x => x.Field != IdField).ToList();
                        var update = updates.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
                        if (disableTrigger)
                        {
                            command.CommandText += $" DISABLE TRIGGER ALL ON [{nameof(Transportation)}];";
                        }
                        else
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(Transportation)}];";
                        }
                        command.CommandText += $" UPDATE [{nameof(Transportation)}] SET {update.Combine()} WHERE Id = {idInt};";
                        command.CommandText += " " + _transportationService.Transportation_ClosingUnitPrice(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnUnitPrice(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_Note4(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_EmptyCombinationId(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_BetFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_BetAmount(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_CombinationFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_Cont20_40(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_DemDate(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_Dem(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ExportListId(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_IsSplitBill(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_LandingFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_LiftFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_MonthText(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnClosingFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnDate(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnLiftFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnNotes(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnVs(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ShellDate(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ShipUnitPriceQuotation(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_VendorLocation(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_Expense(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_BookingId(patch, idInt);
                        if (disableTrigger)
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(Transportation)}];";
                        }
                        foreach (var item in updates)
                        {
                            command.Parameters.AddWithValue($"@{item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                        }
                        command.ExecuteNonQuery();
                        transaction.Commit();
                        await db.Entry(entity).ReloadAsync();
                        BackgroundJob.Enqueue<TaskService>(x => x.SendMessageAllUserOtherMe(new WebSocketResponse<Transportation>
                        {
                            EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                            TypeId = 1,
                            Data = entity
                        }, UserId));
                        return entity;
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(409, entity);
                }
            }
        }

        protected override IQueryable<Transportation> GetQuery()
        {
            var rs = base.GetQuery();
            //Sale
            if (AllRoleIds.Contains(10))
            {
                rs = rs.Where(x => x.UserId == UserId || x.InsertedBy == UserId).AsNoTracking();
            }
            return rs;
        }

        public override async Task<ActionResult<bool>> HardDeleteAsync([FromBody] List<int> ids)
        {
            if (ids.Nothing())
            {
                return true;
            }
            ids = ids.Where(x => x > 0).ToList();
            if (ids.Nothing())
            {
                return true;
            }
            var expenses = await db.Expense.Where(x => x.Active == true && ids.Contains((int)x.TransportationId) && x.IsPurchasedInsurance && x.RequestChangeId == null).ToListAsync();
            if (expenses.Count > 0)
            {
                foreach (var item in expenses)
                {
                    var (statusField, value) = item.GetComplexProp("StatusId");
                    if (statusField)
                    {
                        item.SetPropValue("StatusId", (int)ApprovalStatusEnum.Approving);
                    }
                    var entityType = _entitySvc.GetEntity(typeof(Expense).Name);
                    var approvalConfig = await db.ApprovalConfig.AsNoTracking().OrderBy(x => x.Level)
                        .Where(x => x.Active && x.EntityId == entityType.Id).ToListAsync();
                    if (approvalConfig.Nothing())
                    {
                        throw new ApiException("Quy trình duyệt chưa được cấu hình");
                    }
                    var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
                    if (matchApprovalConfig is null)
                    {
                        throw new ApiException("Quy trình duyệt chưa được cấu hình");
                    }
                    await db.SaveChangesAsync();
                    if (approvalConfig is null)
                    {
                        throw new ApiException("Quy trình duyệt chưa được cấu hình");
                    }
                    var listUser = await (
                        from user in db.User
                        join userRole in db.UserRole on user.Id equals userRole.UserId
                        join role in db.Role on userRole.RoleId equals role.Id
                        where userRole.RoleId == matchApprovalConfig.RoleId
                        select user
                    ).ToListAsync();
                    if (listUser.HasElement())
                    {
                        item.isDelete = true;
                        item.StatusId = (int)ApprovalStatusEnum.Approving;
                        item.DeleteBy = UserId;
                        var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                        var tasks = listUser.Select(user => new TaskNotification
                        {
                            Title = $"{currentUser.FullName}",
                            Description = $"Đã gửi yêu hủy",
                            EntityId = _entitySvc.GetEntity(typeof(Expense).Name).Id,
                            RecordId = item.Id,
                            Attachment = "fal fa-paper-plane",
                            AssignedId = user.Id,
                            StatusId = (int)TaskStateEnum.UnreadStatus,
                            RemindBefore = 540,
                            Deadline = DateTime.Now,
                        });
                        SetAuditInfo(tasks);
                        db.AddRange(tasks);
                        await db.SaveChangesAsync();
                        await _taskService.NotifyAsync(tasks);
                    }
                }
                throw new ApiException("Đã có phí bảo hiểm được mua. Đã gửi yêu cầu hủy !") { StatusCode = HttpStatusCode.BadRequest };
            }
            try
            {
                var sql = $@"select distinct * from [{typeof(Transportation).Name}] where Id in ({string.Join(",", ids)})";
                var data = await db.Set<Transportation>().FromSqlRaw(sql).ToListAsync();
                var delete = $" delete from [{typeof(Expense).Name}] where TransportationId in ({string.Join(",", ids)}) ";
                delete += $" delete from [{typeof(Revenue).Name}] where TransportationId in ({string.Join(",", ids)})";
                delete += $" delete from [{typeof(TransportationRequest).Name}] where TransportationId in ({string.Join(",", ids)}) delete from [{typeof(Transportation).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(delete);
                return true;
            }
            catch
            {
                throw new ApiException("Không thể xóa dữ liệu!") { StatusCode = HttpStatusCode.BadRequest };
            }
        }

        [HttpPost("api/Transportation/TransportationReturn")]
        public async Task<bool> SetTransportationReturn([FromBody] DateTime dateTime)
        {
            var updateCommand = $"update Transportation set IsReturn=1 where Active = 1 and ShipDate = DATEADD(dd, 0, DATEDIFF(dd, 0, '{dateTime}'))";
            await ctx.Database.ExecuteSqlRawAsync(updateCommand);
            return true;
        }

        [HttpGet("api/Transportation/RestoreDelete")]
        public async Task<bool> RestoreDelete()
        {
            var updateCommand = await db.DeleteHistory.Where(x => x.Id >= 99).ToListAsync();
            var value = updateCommand.Select(x => x.Value).ToList();
            var tran = new List<Transportation>();
            foreach (var item in value)
            {
                tran.AddRange(JsonConvert.DeserializeObject<List<Transportation>>(item));
            }
            db.Database.ExecuteSqlRaw($"DISABLE TRIGGER ALL ON [{nameof(Transportation)}]");
            var Ids = tran.Select(x => x.Id).ToList();
            var check = await db.Transportation.Where(x => Ids.Contains(x.Id)).ToListAsync();
            foreach (var item in tran)
            {
                if (check.FirstOrDefault(x => x.Id == item.Id) is null)
                {
                    db.Transportation.Add(item);
                }
            }
            await db.SaveChangesAsync();
            db.Database.ExecuteSqlRaw($"ENABLE TRIGGER ALL ON [{nameof(Transportation)}]");
            return true;
        }

        [HttpPost("api/Transportation/ExportTruckMaintenance")]
        public async Task<string> ExportTruckMaintenance([FromBody] CheckFeeHistory transportation)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(nameof(Transportation));
            var index = 1;
            worksheet.Style.Font.SetFontName("Times New Roman");
            worksheet.Cell($"A{index}").Value = $"BẢNG BÁO CÁO PHÍ PHÁT SINH PHẠT SỬA CHỮA HÀNG NHẬP HP THEO LIST TRẢ HÀNG TỪ {transportation.FromDate.Value.ToString("dd-MM-yyyy")} => {transportation.ToDate.Value.ToString("dd-MM-yyyy")} \r\n ( Tổng 1.921 cont trong đó Trả vỏ ~ 979 cont ~ 51%)";
            worksheet.Cell($"A{index}").Style.Font.Bold = true;
            worksheet.Cell($"A{index}").Style.Font.FontSize = 20;
            worksheet.Cell($"A{index}").Style.Font.FontColor = XLColor.Red;
            worksheet.Cell($"A{index}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell($"A{index}").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range($"A{index}:R{index}").Row(1).Merge();
            index++;
            worksheet.Cell($"A{index}").Value = "STT";
            worksheet.Cell($"B{index}").Value = "Hãng tàu";
            worksheet.Cell($"C{index}").Value = "Số cont\r\n trả vỏ ";
            worksheet.Cell($"D{index}").Value = "Số cont bị phạt";
            worksheet.Cell($"E{index}").Value = "Tổng số \r\ntiền phạt \r\n";
            worksheet.Cell($"F{index}").Value = "Trung bình\r\nsố tiền\r\nphạt/cont trả vỏ (VNĐ)";
            worksheet.Cell($"G{index}").Value = "Đặc điểm chung hãng tàu";
            worksheet.Range($"G{index}:M{index}").Row(1).Merge();
            worksheet.Cell($"N{index}").Value = "Ghi chú chung";
            worksheet.Cell($"O{index}").Value = "Ghi chú trong tháng 04/2023";
            worksheet.Range($"O{index}:P{index}").Row(1).Merge();
            worksheet.Cell($"Q{index}").Value = "Ghi chú chính sách dem det hãng tàu";
            worksheet.Range($"A{index}:Q{index}").Style.Font.Bold = true;
            worksheet.Range($"A{index}:Q{index}").Style.Alignment.WrapText = true;
            worksheet.Range($"A{index}:Q{index}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range($"A{index}:Q{index}").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range($"A{index}:Q{index}").Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Range($"A{index}:Q{index}").Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Range($"A{index}:Q{index}").Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Range($"A{index}:Q{index}").Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            var sql = @$"select br.Name as BrandShip,
            (select COUNT(Id) 
            from Transportation t 
            where t.Id in (
			select distinct TransportationId
            from Expense e 
            where e.InsertedDate > '{transportation.FromDate.Value.AddDays(-1):yyyy-MM-dd}'
            and e.ExpenseTypeId = 15955
			and BrandShipId = tb.BrandShipId
            and e.InsertedDate < '{transportation.ToDate.Value.AddDays(1):yyyy-MM-dd}'
			)) as ContainerTraVo,
            (select COUNT(Id)
            from Expense e 
            where e.InsertedDate > '{transportation.FromDate.Value.AddDays(-1):yyyy-MM-dd}'
            and e.BrandShipId = tb.BrandShipId
            and e.ExpenseTypeId = 15955
            and e.InsertedDate < '{transportation.ToDate.Value.AddDays(1):yyyy-MM-dd}') as ContainerBiPhat,
            (select sum(e.UnitPrice*e.Quantity)
            from Expense e 
            where e.InsertedDate > '{transportation.FromDate.Value.AddDays(-1):yyyy-MM-dd}'
            and e.BrandShipId = tb.BrandShipId
            and e.ExpenseTypeId = 15955
            and e.InsertedDate < '{transportation.ToDate.Value.AddDays(1):yyyy-MM-dd}') as SoTienBiPhat,
            ((select sum(e.UnitPrice*e.Quantity)
            from Expense e 
            where e.InsertedDate > '{transportation.FromDate.Value.AddDays(-1):yyyy-MM-dd}'
            and e.BrandShipId = tb.BrandShipId
            and e.ExpenseTypeId = 15955
            and e.InsertedDate < '{transportation.ToDate.Value.AddDays(1):yyyy-MM-dd}')/
            (select COUNT(Id) 
            from Transportation t 
            where t.Id in (
			select distinct TransportationId
            from Expense e 
            where e.InsertedDate > '{transportation.FromDate.Value.AddDays(-1):yyyy-MM-dd}'
            and e.ExpenseTypeId = 15955
			and BrandShipId = tb.BrandShipId
            and e.InsertedDate < '{transportation.ToDate.Value.AddDays(1):yyyy-MM-dd}'
			))) as TrungBinhSoTien
            from (select distinct t.BrandShipId
            from Transportation t
            where t.Id in (
			select distinct TransportationId
            from Expense e 
            where e.InsertedDate > '{transportation.FromDate.Value.AddDays(-1):yyyy-MM-dd}'
            and e.ExpenseTypeId = 15955
            and e.InsertedDate < '{transportation.ToDate.Value.AddDays(1):yyyy-MM-dd}'
			)) as tb
            join Vendor br on br.Id = tb.BrandShipId 
            ";
            index++;
            var data = await ConverSqlToDataSet(sql);
            var k = 1;
            foreach (var item in data[0])
            {
                worksheet.Cell($"A{index}").Value = k;
                worksheet.Cell($"B{index}").Value = item["BrandShip"].ToString();
                worksheet.Cell($"C{index}").Value = item["ContainerTraVo"] is null ? default(decimal?) : decimal.Parse(item["ContainerTraVo"].ToString());
                worksheet.Cell($"D{index}").Value = item["ContainerBiPhat"] is null ? default(decimal?) : decimal.Parse(item["ContainerBiPhat"].ToString());
                worksheet.Cell($"E{index}").Value = item["SoTienBiPhat"] is null ? default(decimal?) : decimal.Parse(item["SoTienBiPhat"].ToString());
                worksheet.Cell($"F{index}").Value = item["TrungBinhSoTien"] is null ? default(decimal?) : decimal.Parse(item["TrungBinhSoTien"].ToString());
                worksheet.Cell($"C{index}").Style.NumberFormat.Format = "#,##";
                worksheet.Cell($"D{index}").Style.NumberFormat.Format = "#,##";
                worksheet.Cell($"E{index}").Style.NumberFormat.Format = "#,##";
                worksheet.Cell($"F{index}").Style.NumberFormat.Format = "#,##";
                worksheet.Cell($"G{index}").Value = "";
                worksheet.Range($"G{index}:M{index}").Row(1).Merge();
                worksheet.Cell($"N{index}").Value = "";
                worksheet.Cell($"O{index}").Value = "";
                worksheet.Range($"O{index}:P{index}").Row(1).Merge();
                worksheet.Cell($"Q{index}").Value = "";
                worksheet.Range($"A{index}:Q{index}").Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{index}:Q{index}").Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{index}:Q{index}").Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{index}:Q{index}").Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                index++;
                k++;
            }
            index += 3;
            worksheet.Cell($"A{index}").Value = "Ngày nhập";
            worksheet.Cell($"B{index}").Value = "Hãng tàu";
            worksheet.Cell($"C{index}").Value = "Tên tàu";
            worksheet.Cell($"D{index}").Value = "Số chuyến";
            worksheet.Cell($"E{index}").Value = "Nhân viên bán hàng";
            worksheet.Cell($"F{index}").Value = "Ngày trả hàng";
            worksheet.Cell($"G{index}").Value = "Chủ hàng";
            worksheet.Cell($"H{index}").Value = "Vật tư hàng hóa";
            worksheet.Cell($"I{index}").Value = "Loại Container";
            worksheet.Cell($"J{index}").Value = "Số Cont";
            worksheet.Cell($"K{index}").Value = "Số tiền";
            worksheet.Cell($"L{index}").Value = "Hóa đơn";
            worksheet.Cell($"M{index}").Value = "Tháng đóng";
            worksheet.Cell($"N{index}").Value = "Năm";
            worksheet.Cell($"O{index}").Value = "[Phí khác (trả hàng)] Ghi chú";
            worksheet.Cell($"P{index}").Value = "User tạo";
            worksheet.Range($"A{index}:P{index}").Style.Font.Bold = true;
            worksheet.Range($"A{index}:P{index}").Style.Alignment.WrapText = true;
            worksheet.Range($"A{index}:P{index}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range($"A{index}:P{index}").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range($"A{index}:P{index}").Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Range($"A{index}:P{index}").Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Range($"A{index}:P{index}").Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Range($"A{index}:P{index}").Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            var sql1 = @$"select e.InsertedDate,
                        br.Name as BrandShip,
                        s.Name as Ship,
                        t.Trip,
                        u.FullName,
                        t.ReturnDate,
                        REPLACE(b.Name,'%26','&') as Boss,
                        com.Description as Com,
                        cont.Description as ContainerType,
                        t.ContainerNo,
                        e.UnitPrice*e.Quantity as UnitPrice,
                        e.IsVat,
                        REPLACE(t.MonthText,'%2F','/') as MonthText,
                        t.YearText,	
                        REPLACE(e.Notes,'%2F','/') as Notes,
                        ins.FullName as InsertedBy
                        from Expense e 
                        left join Transportation t on e.TransportationId = t.Id 
                        left join Vendor br on t.BrandShipId = br.Id 
                        left join Vendor b on t.BossId = b.Id 
                        left join Ship s on t.ShipId = s.Id 
                        left join [User] u on t.UserId = u.Id 
                        left join [User] ins on e.InsertedBy = ins.Id 
                        left join MasterData com on t.CommodityId = com.Id 
                        left join MasterData cont on t.ContainerTypeId = cont.Id 
                        where e.InsertedDate > '{transportation.FromDate.Value.AddDays(-1):yyyy-MM-dd}'
                        and e.ExpenseTypeId = 15955
                        and e.InsertedDate < '{transportation.ToDate.Value.AddDays(1):yyyy-MM-dd}'
            ";
            index++;
            var data1 = await ConverSqlToDataSet(sql1);
            foreach (var item in data1[0])
            {
                worksheet.Cell($"A{index}").Value = DateTime.Parse(item["InsertedDate"].ToString());
                worksheet.Cell($"B{index}").Value = item["BrandShip"].ToString();
                worksheet.Cell($"C{index}").Value = item["Ship"].ToString();
                worksheet.Cell($"D{index}").Value = item["Trip"].ToString();
                worksheet.Cell($"E{index}").Value = item["FullName"].ToString();
                worksheet.Cell($"F{index}").Value = item["ReturnDate"] is null ? default(DateTime?) : DateTime.Parse(item["ReturnDate"].ToString());
                worksheet.Cell($"G{index}").Value = item["Boss"].ToString();
                worksheet.Cell($"H{index}").Value = item["Com"].ToString();
                worksheet.Cell($"I{index}").Value = item["ContainerType"].ToString();
                worksheet.Cell($"J{index}").Value = item["ContainerNo"].ToString();
                worksheet.Cell($"K{index}").Value = decimal.Parse(item["UnitPrice"].ToString());
                worksheet.Cell($"K{index}").Style.NumberFormat.Format = "#,##";
                worksheet.Cell($"L{index}").Value = item["IsVat"].ToString() == "False" ? default(int?) : 1;
                worksheet.Cell($"M{index}").Value = item["MonthText"].ToString();
                worksheet.Cell($"N{index}").Value = int.Parse(item["YearText"].ToString());
                worksheet.Cell($"O{index}").Value = item["Notes"].ToString().DecodeSpecialChar();
                worksheet.Cell($"P{index}").Value = item["InsertedBy"].ToString();
                worksheet.Range($"A{index}:P{index}").Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{index}:P{index}").Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{index}:P{index}").Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{index}:P{index}").Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                index++;
            }
            var url = $"BaoCaoSuaChua{transportation.FromDate.Value.ToString("dd-MM-yyyy")}-{transportation.ToDate.Value.ToString("dd-MM-yyyy")}.xlsx";
            worksheet.Columns().AdjustToContents();
            workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
            return url;
        }

        [HttpPost("api/Transportation/ExportCheckFee")]
        public async Task<string> ExportCheckFee([FromBody] CheckFeeHistory transportation, [FromQuery] int Type)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(nameof(Transportation));
            var closingId = transportation.ClosingId;
            var closing = await db.Vendor.FirstOrDefaultAsync(x => x.Id == closingId);
            worksheet.Style.Font.SetFontName("Times New Roman");
            worksheet.Cell("A1").Value = closing.Name;
            worksheet.Cell("A2").Value = "Địa chỉ";
            worksheet.Cell("A3").Value = "MST";
            worksheet.Cell("A4").Value = $"BẢNG KÊ ĐỐI CHIẾU CƯỚC VC XE TỪ NGÀY {transportation.FromDate?.ToString("dd/MM/yyyy")} ĐẾN {transportation.ToDate?.ToString("dd/MM/yyyy")}";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("A4").Style.Font.FontSize = 20;
            worksheet.Cell("A4").Style.Font.FontColor = XLColor.Red;
            worksheet.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A4").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("A4:AA4").Row(1).Merge();
            worksheet.Cell("A5").Value = $"Kính gửi: Công Ty Cổ Phần Logistics Đông Á";
            worksheet.Cell("A5").Style.Font.Italic = true;
            worksheet.Cell("A5").Style.Font.Bold = true;

            worksheet.Range("A6:AA6").Style.Fill.BackgroundColor = XLColor.LightGreen;
            worksheet.Range("A7:AA7").Style.Fill.BackgroundColor = XLColor.LightGreen;
            worksheet.Row(6).Height = 30;
            worksheet.Range("A6:AA6").Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Range("A6:AA6").Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Range("A6:AA6").Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Range("A6:AA6").Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Row(7).Height = 70;
            worksheet.Range("A7:AA7").Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Range("A7:AA7").Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Range("A7:AA7").Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Range("A7:AA7").Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Cell("A6").Value = $"STT";
            worksheet.Cell("A6").Style.Alignment.WrapText = true;
            worksheet.Cell("A6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("A6:B7").Column(1).Merge();

            worksheet.Cell("B6").Value = transportation.TypeId == 1 ? $"Ngày đóng hàng" : "Ngày nhận hàng";
            worksheet.Cell("B6").Style.Alignment.WrapText = true;
            worksheet.Cell("B6").Style.Font.Bold = true;
            worksheet.Cell("B6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("B6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("B6:C7").Column(1).Merge();

            worksheet.Cell("C6").Value = $"Chủ hàng";
            worksheet.Cell("C6").Style.Alignment.WrapText = true;
            worksheet.Cell("C6").Style.Font.Bold = true;
            worksheet.Cell("C6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("P6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("C6:D7").Column(1).Merge();

            worksheet.Cell("D6").Value = $"Số cont";
            worksheet.Cell("D6").Style.Font.Bold = true;
            worksheet.Cell("D6").Style.Alignment.WrapText = true;
            worksheet.Cell("D6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("D6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("D6:E7").Column(1).Merge();

            worksheet.Cell("E6").Value = $"Số seal";
            worksheet.Cell("E6").Style.Font.Bold = true;
            worksheet.Cell("E6").Style.Alignment.WrapText = true;
            worksheet.Cell("E6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("E6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("E6:F7").Column(1).Merge();

            worksheet.Cell("F6").Value = $"Cont 20";
            worksheet.Cell("F6").Style.Font.Bold = true;
            worksheet.Cell("F6").Style.Alignment.WrapText = true;
            worksheet.Cell("F6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("F6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("F6:G7").Column(1).Merge();

            worksheet.Cell("G6").Value = $"Cont 40";
            worksheet.Cell("G6").Style.Font.Bold = true;
            worksheet.Cell("G6").Style.Alignment.WrapText = true;
            worksheet.Cell("G6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("G6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("G6:H7").Column(1).Merge();

            worksheet.Cell("H6").Value = transportation.TypeId == 1 ? $"Địa điểm nhận hàng" : "Địa điểm trả hàng";
            worksheet.Cell("H6").Style.Alignment.WrapText = true;
            worksheet.Cell("H6").Style.Font.Bold = true;
            worksheet.Cell("H6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("H6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("H6:I7").Column(1).Merge();

            worksheet.Cell("I6").Value = transportation.TypeId == 1 ? $"Nơi lấy rỗng" : "Cảng nâng hàng";
            worksheet.Cell("I6").Style.Font.Bold = true;
            worksheet.Cell("I6").Style.Alignment.WrapText = true;
            worksheet.Cell("I6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("I6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("I6:J7").Column(1).Merge();

            worksheet.Cell("J6").Value = transportation.TypeId == 1 ? $"Cảng hạ hàng" : "Nơi trả rỗng";
            worksheet.Cell("J6").Style.Font.Bold = true;
            worksheet.Cell("J6").Style.Alignment.WrapText = true;
            worksheet.Cell("J6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("J6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("J6:K7").Column(1).Merge();

            worksheet.Cell("K6").Value = $"Phí nâng";
            worksheet.Cell("K6").Style.Font.Bold = true;
            worksheet.Cell("K6").Style.Alignment.WrapText = true;
            worksheet.Cell("K6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("K6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("K6:L7").Column(1).Merge();

            worksheet.Cell("L6").Value = $"Phí hạ";
            worksheet.Cell("L6").Style.Alignment.WrapText = true;
            worksheet.Cell("L6").Style.Font.Bold = true;
            worksheet.Cell("L6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("L6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("L6:M7").Column(1).Merge();

            worksheet.Cell("M6").Value = $"Chi phí (có HĐ - VAT hiện hành)";
            worksheet.Cell("M6").Style.Font.Bold = true;
            worksheet.Cell("M6").Style.Alignment.WrapText = true;
            worksheet.Cell("M6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("M6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("M6:O6").Row(1).Merge();

            worksheet.Cell("P6").Value = $"Chi phí (không HĐ)";
            worksheet.Cell("P6").Style.Font.Bold = true;
            worksheet.Cell("P6").Style.Alignment.WrapText = true;
            worksheet.Cell("P6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("P6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("P6:U6").Row(1).Merge();

            worksheet.Cell("V6").Value = $"Thu lại tiền nhà xe (lưu vỏ, lưu cont, sửa chữa…) nhập số âm";
            worksheet.Cell("V6").Style.Font.Bold = true;
            worksheet.Cell("V6").Style.Alignment.WrapText = true;
            worksheet.Cell("V6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("V6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("V6:W7").Column(1).Merge();

            worksheet.Cell("W6").Value = $"% kết hợp";
            worksheet.Cell("W6").Style.Font.Bold = true;
            worksheet.Cell("W6").Style.Alignment.WrapText = true;
            worksheet.Cell("W6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("W6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("W6:X7").Column(1).Merge();

            worksheet.Cell("X6").Value = $"Cước vc 10%";
            worksheet.Cell("X6").Style.Alignment.WrapText = true;
            worksheet.Cell("X6").Style.Font.Bold = true;
            worksheet.Cell("X6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("X6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("X6:Y7").Column(1).Merge();

            worksheet.Cell("Y6").Value = $"Cước 8%";
            worksheet.Cell("Y6").Style.Alignment.WrapText = true;
            worksheet.Cell("Y6").Style.Font.Bold = true;
            worksheet.Cell("Y6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("Y6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("Y6:Z7").Column(1).Merge();

            worksheet.Cell("Z6").Value = $"Tổng Cước VC(theo thuế hiện hành";
            worksheet.Cell("Z6").Style.Font.Bold = true;
            worksheet.Cell("Z6").Style.Alignment.WrapText = true;
            worksheet.Cell("Z6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("Z6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("Z6:AA7").Column(1).Merge();

            worksheet.Cell("AA6").Value = $"Ghi chú";
            worksheet.Cell("AA6").Style.Alignment.WrapText = true;
            worksheet.Cell("AA6").Style.Font.Bold = true;
            worksheet.Cell("AA6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AA6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AA6:AB7").Column(1).Merge();

            worksheet.Cell("M7").Value = $"HĐ xuất cho ĐA (phí cân ở cảng, hạ ngoài)";
            worksheet.Cell("M7").Style.Alignment.WrapText = true;
            worksheet.Cell("M7").Style.Font.Bold = true;
            worksheet.Cell("M7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("M7").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell("N7").Value = $"HĐ xuất cho KH";
            worksheet.Cell("N7").Style.Alignment.WrapText = true;
            worksheet.Cell("N7").Style.Font.Bold = true;
            worksheet.Cell("N7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("N7").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell("O7").Value = $"Phí vé cầu đường, cao tốc, trạm thu phí... (giá có VAT nhưng ko có HĐ),";
            worksheet.Cell("O7").Style.Alignment.WrapText = true;
            worksheet.Cell("O7").Style.Font.Bold = true;
            worksheet.Cell("O7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("O7").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell("P7").Value = $"Bốc xếp";
            worksheet.Cell("P7").Style.Alignment.WrapText = true;
            worksheet.Cell("P7").Style.Font.Bold = true;
            worksheet.Cell("P7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("P7").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell("Q7").Value = $"Phí hạ xa (HP, SP, …)";
            worksheet.Cell("Q7").Style.Alignment.WrapText = true;
            worksheet.Cell("Q7").Style.Font.Bold = true;
            worksheet.Cell("Q7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("Q7").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell("R7").Value = $"2 kho, chuyển kho";
            worksheet.Cell("R7").Style.Alignment.WrapText = true;
            worksheet.Cell("R7").Style.Font.Bold = true;
            worksheet.Cell("R7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("R7").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell("S7").Value = $"Neo xe/Cân";
            worksheet.Cell("S7").Style.Alignment.WrapText = true;
            worksheet.Cell("S7").Style.Font.Bold = true;
            worksheet.Cell("S7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("S7").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell("T7").Value = $"CP phát sinh khác (máy phát, chống ẩm, bạt, vé cổng, vé bãi, phụ kho lót bạt, cò công an,...)";
            worksheet.Cell("T7").Style.Alignment.WrapText = true;
            worksheet.Cell("T7").Style.Font.Bold = true;
            worksheet.Cell("T7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("T7").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell("U7").Value = $"Chênh lệch hạ vỏ";
            worksheet.Cell("U7").Style.Alignment.WrapText = true;
            worksheet.Cell("U7").Style.Font.Bold = true;
            worksheet.Cell("U7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("U7").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            var sql = string.Empty;
            sql += @$"select 
                    b.Name as Boss
                    ,r.Description as Received
                    ,pi.Name as PickupEmpty
                    ,po.Name as PortLoading
                    ,t.*
                    from Transportation t
                    left join Vendor b on b.Id = t.BossId
                    left join Location r on r.Id = {(transportation.TypeId == 2 ? "t.ReturnId" : "t.ReceivedId")}
                    left join Location pi on pi.Id = {(transportation.TypeId == 2 ? "t.ReturnEmptyId" : "t.PickupEmptyId")}
                    left join Location po on po.Id = {(transportation.TypeId == 2 ? "t.PortLiftId" : "t.PortLoadingId")}";
            if (transportation.Id > 0)
            {
                if (transportation.TypeId == 1)
                {
                    sql += $" where t.CheckFeeHistoryId = {transportation.Id}" +
                    $"  order by t.OrderExcel asc";
                }
                else
                {
                    sql += $" where t.CheckFeeHistoryReturnId = {transportation.Id}" +
                    $"  order by t.OrderExcelReturn asc";
                }
            }
            else
            {
                if (transportation.TypeId == 1)
                {
                    sql += $" where t.ClosingDate >= '{transportation.FromDate.Value.ToString("yyyy-MM-dd")}' and t.ClosingDate <= '{transportation.ToDate.Value.ToString("yyyy-MM-dd")}' and t.ClosingId = {transportation.ClosingId} and t.RouteId in ({transportation.RouteIds.Combine()})"
                + $"  order by t.ClosingDate asc";
                }
                else
                {
                    sql += $" where t.ReturnDate >= '{transportation.FromDate.Value.ToString("yyyy-MM-dd")}' and t.ReturnDate <= '{transportation.ToDate.Value.ToString("yyyy-MM-dd")}' and t.ReturnVendorId = {transportation.ClosingId} and t.RouteId in ({transportation.RouteIds.Combine()})"
                + $"  order by t.ReturnDate asc";
                }
            }
            var data = await ConverSqlToDataSet(sql);
            var start = 8;
            foreach (var item in data[0])
            {
                worksheet.Cell("A" + start).SetValue(start - 7);
                worksheet.Cell("B" + start).SetValue(transportation.TypeId == 1 ? DateTime.Parse(item[nameof(Transportation.ClosingDate)].ToString()) : DateTime.Parse(item[nameof(Transportation.ReturnDate)].ToString()));
                worksheet.Cell("C" + start).SetValue(item["Boss"] is null ? null : item["Boss"].ToString().DecodeSpecialChar());
                worksheet.Cell("D" + start).SetValue(item["ContainerNo"] is null ? null : item["ContainerNo"].ToString());
                var seal = item["SealNo"] is null ? null : item["SealNo"].ToString();
                var sealUpload = item["SealCheckUpload"] is null ? null : item["SealCheckUpload"].ToString();
                worksheet.Cell("E" + start).SetValue(seal);
                if (seal != sealUpload)
                {
                    worksheet.Cell("E" + start).Style.Font.FontColor = XLColor.Red;
                }
                worksheet.Cell("F" + start).SetValue(item["Cont20"] is null ? default(decimal) : decimal.Parse(item["Cont20"].ToString()));
                worksheet.Cell("G" + start).SetValue(item["Cont40"] is null ? default(decimal) : decimal.Parse(item["Cont40"].ToString()));
                worksheet.Cell("H" + start).SetValue(item["Received"] is null ? null : item["Received"].ToString().DecodeSpecialChar());
                var pick = item["PickupEmpty"];
                worksheet.Cell("I" + start).SetValue(pick is null ? null : pick.ToString().DecodeSpecialChar());
                if (bool.Parse(item["IsEmptyLift"].ToString())
                    || bool.Parse(item["IsSeftPayment"].ToString()))
                {
                    item["LiftFee"] = null;
                }
                if (bool.Parse(item["IsSeftPaymentReturn"].ToString())
                    || bool.Parse(item["IsLiftFee"].ToString()))
                {
                    item["ReturnLiftFee"] = null;
                }
                worksheet.Cell("K" + start).SetValue(transportation.TypeId == 1 ? (item["LiftFee"] is null ? default(decimal) : decimal.Parse(item["LiftFee"].ToString()))
                    : (item["ReturnLiftFee"] is null ? default(decimal) : decimal.Parse(item["ReturnLiftFee"].ToString())));
                worksheet.Cell("K" + start).Style.NumberFormat.Format = "#,##";
                if (bool.Parse(item["IsLanding"].ToString())
                    || bool.Parse(item["IsSeftPaymentLand"].ToString()))
                {
                    item["LandingFee"] = null;
                }
                if (bool.Parse(item["IsSeftPaymentLandReturn"].ToString())
                    || bool.Parse(item["IsClosingEmptyFee"].ToString()))
                {
                    item["ReturnClosingFee"] = null;
                }
                worksheet.Cell("L" + start).SetValue(transportation.TypeId == 1 ? (item["LandingFee"] is null ? default(decimal) : decimal.Parse(item["LandingFee"].ToString()))
                    : (item["ReturnClosingFee"] is null ? default(decimal) : decimal.Parse(item["ReturnClosingFee"].ToString())));
                worksheet.Cell("L" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("M" + start).SetValue(transportation.TypeId == 1 ? (item["CollectOnBehaftInvoinceNoFee"] is null ? default(decimal) : decimal.Parse(item["CollectOnBehaftInvoinceNoFee"].ToString()))
                    : (item["FeeVatReturn"] is null ? default(decimal) : decimal.Parse(item["FeeVatReturn"].ToString())));
                worksheet.Cell("M" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("N" + start).SetValue(transportation.TypeId == 1 ? (item["FeeVat2"] is null ? default(decimal) : decimal.Parse(item["FeeVat2"].ToString()))
                    : (item["FeeVatReturn2"] is null ? default(decimal) : decimal.Parse(item["FeeVatReturn2"].ToString())));
                worksheet.Cell("N" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("O" + start).SetValue(transportation.TypeId == 1 ? (item["FeeVat3"] is null ? default(decimal) : decimal.Parse(item["FeeVat3"].ToString()))
                    : (item["FeeVatReturn3"] is null ? default(decimal) : decimal.Parse(item["FeeVatReturn3"].ToString())));
                worksheet.Cell("O" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("P" + start).SetValue(transportation.TypeId == 1 ? (item["CollectOnBehaftFee"] is null ? default(decimal) : decimal.Parse(item["CollectOnBehaftFee"].ToString()))
                    : (item["FeeReturn1"] is null ? default(decimal) : decimal.Parse(item["FeeReturn1"].ToString())));
                worksheet.Cell("P" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("Q" + start).SetValue(transportation.TypeId == 1 ? (item["Fee2"] is null ? default(decimal) : decimal.Parse(item["Fee2"].ToString()))
                    : (item["FeeReturn2"] is null ? default(decimal) : decimal.Parse(item["FeeReturn2"].ToString())));
                worksheet.Cell("Q" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("R" + start).SetValue(transportation.TypeId == 1 ? (item["Fee3"] is null ? default(decimal) : decimal.Parse(item["Fee3"].ToString()))
                    : (item["FeeReturn3"] is null ? default(decimal) : decimal.Parse(item["FeeReturn3"].ToString())));
                worksheet.Cell("R" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("S" + start).SetValue(transportation.TypeId == 1 ? (item["Fee4"] is null ? default(decimal) : decimal.Parse(item["Fee4"].ToString()))
                    : (item["FeeReturn4"] is null ? default(decimal) : decimal.Parse(item["FeeReturn4"].ToString())));
                worksheet.Cell("S" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("T" + start).SetValue(transportation.TypeId == 1 ? (item["Fee5"] is null ? default(decimal) : decimal.Parse(item["Fee5"].ToString()))
                    : (item["FeeReturn5"] is null ? default(decimal) : decimal.Parse(item["FeeReturn5"].ToString())));
                worksheet.Cell("T" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("U" + start).SetValue(transportation.TypeId == 1 ? (item["Fee6"] is null ? default(decimal) : decimal.Parse(item["Fee6"].ToString()))
                    : (item["FeeReturn6"] is null ? default(decimal) : decimal.Parse(item["FeeReturn6"].ToString())));
                worksheet.Cell("U" + start).Style.NumberFormat.Format = "#,##";

                var closingPercent = item["ClosingPercent"] is null ? "0" : item["ClosingPercent"].ToString();
                var closingPercentUpload = item["ClosingPercentUpload"] is null ? "0" : item["ClosingPercentUpload"].ToString();

                worksheet.Cell("V" + start).SetValue(transportation.TypeId == 1 ? (item["CollectOnSupPrice"] is null ? default(decimal) : decimal.Parse(item["CollectOnSupPrice"].ToString()))
                    : (item["CollectOnSupPriceReturn"] is null ? default(decimal) : decimal.Parse(item["CollectOnSupPriceReturn"].ToString())));
                worksheet.Cell("V" + start).Style.NumberFormat.Format = "#,##";
                if (transportation.TypeId == 1)
                {
                    if (closingPercent != closingPercentUpload)
                    {
                        worksheet.Cell("W" + start).SetValue(decimal.Parse(closingPercent));
                        worksheet.Cell("W" + start).Style.Font.FontColor = XLColor.Red;
                    }
                    else
                    {
                        worksheet.Cell("W" + start).SetValue(item["ClosingPercent"] is null ? "" : decimal.Parse(item["ClosingPercent"].ToString()));
                    }

                    worksheet.Cell("W" + start).Style.NumberFormat.Format = "#,##";

                    var closingCombinationUnitPrice = item["ClosingCombinationUnitPrice"] is null ? null : item["ClosingCombinationUnitPrice"].ToString();
                    var closingCombinationUnitPriceUpload = item["TotalPriceAfterTaxUpload"] is null ? null : item["TotalPriceAfterTaxUpload"].ToString();

                    worksheet.Cell("X" + start).SetValue(closingCombinationUnitPrice is null ? default(decimal) : decimal.Parse(closingCombinationUnitPrice.ToString()));
                    worksheet.Cell("X" + start).Style.NumberFormat.Format = "#,##";
                    if (closingCombinationUnitPrice != closingCombinationUnitPriceUpload)
                    {
                        worksheet.Cell("X" + start).Style.Font.FontColor = XLColor.Red;
                    }

                    var sum = (item["LiftFee"] is null ? default(decimal) : decimal.Parse(item["LiftFee"].ToString()))
                        + (closingCombinationUnitPrice is null ? default(decimal) : decimal.Parse(closingCombinationUnitPrice.ToString()))
                        + (item["LandingFee"] is null ? default(decimal) : decimal.Parse(item["LandingFee"].ToString()))
                        + (item["CollectOnBehaftInvoinceNoFee"] is null ? default(decimal) : decimal.Parse(item["CollectOnBehaftInvoinceNoFee"].ToString()))
                        + (item["CollectOnBehaftFee"] is null ? default(decimal) : decimal.Parse(item["CollectOnBehaftFee"].ToString()));
                    worksheet.Cell("Z" + start).SetValue(sum);
                }
                else
                {
                    worksheet.Cell("W" + start).SetValue(default(decimal));

                    var closingCombinationUnitPrice = item["ReturnUnitPrice"] is null ? null : item["ReturnUnitPrice"].ToString();
                    worksheet.Cell("X" + start).SetValue(closingCombinationUnitPrice is null ? default(decimal) : decimal.Parse(closingCombinationUnitPrice.ToString()));
                    worksheet.Cell("X" + start).Style.NumberFormat.Format = "#,##";

                    var sum = (item["ReturnLiftFee"] is null ? default(decimal) : decimal.Parse(item["ReturnLiftFee"].ToString()))
                        + (closingCombinationUnitPrice is null ? default(decimal) : decimal.Parse(closingCombinationUnitPrice.ToString()))
                        + (item["ReturnClosingFee"] is null ? default(decimal) : decimal.Parse(item["ReturnClosingFee"].ToString()))
                        + (item["ReturnCollectOnBehaftInvoinceFee"] is null ? default(decimal) : decimal.Parse(item["ReturnCollectOnBehaftInvoinceFee"].ToString()))
                        + (item["ReturnCollectOnBehaftFee"] is null ? default(decimal) : decimal.Parse(item["ReturnCollectOnBehaftFee"].ToString()));
                    worksheet.Cell("Z" + start).SetValue(sum);
                }
                worksheet.Cell("Z" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Range($"A{start}:AA{start}").Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{start}:AA{start}").Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{start}:AA{start}").Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{start}:AA{start}").Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                start++;
            }
            var tt = 8 + data[0].Count;
            worksheet.Row(tt).Style.Fill.BackgroundColor = XLColor.LightGreen;
            worksheet.Cell("A" + tt).Value = $"Tổng cộng";
            worksheet.Cell("F" + tt).Value = data[0].Sum(item => item["Cont20"] is null ? default(decimal) : decimal.Parse(item["Cont20"].ToString()));
            worksheet.Cell("F" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("F" + tt).Style.Font.Bold = true;
            worksheet.Cell("G" + tt).Value = data[0].Sum(item => item["Cont40"] is null ? default(decimal) : decimal.Parse(item["Cont40"].ToString()));
            worksheet.Cell("G" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("G" + tt).Style.Font.Bold = true;
            var t = data[0].Sum(item => transportation.TypeId == 1 ? (item["LiftFee"] is null ? default(decimal) : decimal.Parse(item["LiftFee"].ToString()))
            : (item["ReturnLiftFee"] is null ? default(decimal) : decimal.Parse(item["ReturnLiftFee"].ToString())));
            worksheet.Cell("K" + tt).Value = t;
            worksheet.Cell("K" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("K" + tt).Style.Font.Bold = true;


            var tt1 = data[0].Sum(item => transportation.TypeId == 1 ? (item["LandingFee"] is null ? default(decimal) : decimal.Parse(item["LandingFee"].ToString()))
            : (item["ReturnClosingFee"] is null ? default(decimal) : decimal.Parse(item["ReturnClosingFee"].ToString())));
            worksheet.Cell("L" + tt).Value = tt1;
            worksheet.Cell("L" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("L" + tt).Style.Font.Bold = true;

            var tt2 = data[0].Sum(item => transportation.TypeId == 1 ? (item["CollectOnBehaftInvoinceNoFee"] is null ? default(decimal) : decimal.Parse(item["CollectOnBehaftInvoinceNoFee"].ToString()))
            : (item["ReturnCollectOnBehaftInvoinceFee"] is null ? default(decimal) : decimal.Parse(item["ReturnCollectOnBehaftInvoinceFee"].ToString())));
            worksheet.Cell("O" + tt).Value = tt2;
            worksheet.Cell("O" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("O" + tt).Style.Font.Bold = true;
            var tt3 = data[0].Sum(item => transportation.TypeId == 1 ? (item["CollectOnBehaftFee"] is null ? default(decimal) : decimal.Parse(item["CollectOnBehaftFee"].ToString()))
            : (item["ReturnCollectOnBehaftFee"] is null ? default(decimal) : decimal.Parse(item["ReturnCollectOnBehaftFee"].ToString())));

            worksheet.Cell("P" + tt).Value = tt3;

            worksheet.Cell("P" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("P" + tt).Style.Font.Bold = true;
            var tt4 = data[0].Sum(item => transportation.TypeId == 1 ? (item["ClosingCombinationUnitPrice"] is null ? default(decimal) : decimal.Parse(item["ClosingCombinationUnitPrice"].ToString()))
            : (item["ReturnUnitPrice"] is null ? default(decimal) : decimal.Parse(item["ReturnUnitPrice"].ToString())));
            worksheet.Cell("X" + tt).Value = tt4;
            worksheet.Cell("Z" + tt).Value = t + tt1 + tt2 + tt3 + tt4;
            worksheet.Cell("Z" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("X" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("X" + tt).Style.Font.Bold = true;
            worksheet.Cell("Z" + tt).Style.Font.Bold = true;
            worksheet.Cell("A" + tt).Style.Font.Bold = true;
            worksheet.Cell("A" + tt).Style.Alignment.WrapText = true;
            worksheet.Cell("A" + tt).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A" + tt).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range($"A{tt}:E{tt}").Row(1).Merge();
            worksheet.Column(2).AdjustToContents();
            worksheet.Column(4).AdjustToContents();
            worksheet.Column(11).AdjustToContents();
            worksheet.Column(12).AdjustToContents();
            worksheet.Column(16).AdjustToContents();
            worksheet.Column(24).AdjustToContents();
            worksheet.Column(26).AdjustToContents();
            var url = $"BangKe{closing.Name}{transportation.FromDate.Value.ToString("dd-MM-yyyy")}Den{transportation.ToDate.Value.ToString("dd-MM-yyyy")}.xlsx";
            workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
            return url;
        }

        [HttpPost("api/Transportation/ExportProductionReport")]
        public async Task<string> ExportProductionReport([FromBody] ReportGroupVM entity)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(nameof(Transportation));
            worksheet.Style.Font.SetFontName("Times New Roman");
            var selects = new List<string>();
            if (entity.Route)
            {
                selects.Add("r.Name as 'Route'");
            }
            if (entity.BrandShip)
            {
                selects.Add("bs.Name as 'BrandShip'");
            }
            if (entity.StartShip)
            {
                selects.Add("t.StartShip");
            }
            if (entity.ContainerType)
            {
                selects.Add("cont.Description as 'ContainerType'");
            }
            if (entity.Ship)
            {
                selects.Add("s.Name as 'Ship'");
            }
            if (entity.User)
            {
                selects.Add("us.FullName as 'User'");
            }
            if (entity.Closing)
            {
                selects.Add("c.Name as 'Closing'");
            }
            if (entity.Boss)
            {
                selects.Add("b.Name as 'Boss'");
            }
            if (entity.ExportList)
            {
                selects.Add("e.Name as 'ExportList'");
            }
            if (entity.Commodity)
            {
                selects.Add("com.Description as 'Commodity'");
            }
            var sql = @$"select {selects.Combine()},SUM(t.Cont40) as Cont40,SUM(t.Cont20) as Cont20
            ,sum(case when t.EmptyCombinationId is not null and Cont20 = 1 then 1 else 0 end) as EmptyCombination20
            ,sum(case when t.EmptyCombinationId is not null and Cont40 = 1 then 1 else 0 end) as EmptyCombination40
            ,cast(Round((sum(case when t.EmptyCombinationId is not null then 1 else 0 end)/(SUM(t.Cont40)+SUM(t.Cont20)))*100,0) as int) as Per from Transportation t ";
            if (entity.Route)
            {
                sql += @$" left join Route as r on r.Id = t.RouteId";
            }
            if (entity.BrandShip)
            {
                sql += @$" left join Vendor as bs on bs.Id = t.BrandShipId";
            }
            if (entity.ContainerType)
            {
                sql += @$" left join MasterData as cont on cont.Id = t.ContainerTypeId";
            }
            if (entity.Ship)
            {
                sql += @$" left join Ship as s on s.Id = t.ShipId";
            }
            if (entity.User)
            {
                sql += @$" left join [User] as us on us.Id = t.UserId";
            }
            if (entity.Closing)
            {
                sql += @$" left join Vendor as c on c.Id = t.ClosingId";
            }
            if (entity.Boss)
            {
                sql += @$" left join Vendor as b on b.Id = t.BossId";
            }
            if (entity.ExportList)
            {
                sql += @$" left join Vendor as e on e.Id = t.ExportListId";
            }
            if (entity.Commodity)
            {
                sql += @$" left join MasterData as com on com.Id = t.CommodityId";
            }
            var selects1 = new List<string>();
            var orderby = new List<string>();
            if (entity.StartShip)
            {
                selects1.Add("t.StartShip,t.Id");
            }
            if (entity.ExportList)
            {
                selects1.Add("e.Name,e.Id");
            }
            if (entity.Route)
            {
                selects1.Add("r.Name,r.Id");

            }
            if (entity.BrandShip)
            {
                selects1.Add("bs.Name,bs.Id");
            }
            if (entity.Ship)
            {
                selects1.Add("s.Name,s.Id");
            }
            if (entity.ContainerType)
            {
                selects1.Add("cont.Description,cont.Id");
            }
            if (entity.Closing)
            {
                selects1.Add("c.Name,c.Id");
            }
            if (entity.Boss)
            {
                selects1.Add("b.Name,b.Id");
            }
            if (entity.Commodity)
            {
                selects1.Add("com.Description,com.Id");
            }
            if (entity.User)
            {
                selects1.Add("us.FullName,us.Id");
            }
            if (!entity.Return)
            {
                sql += $" where t.ClosingDate > '{entity.FromDate.Value.AddDays(-1).ToString("yyyy-MM-dd")}' and t.ClosingDate < '{entity.ToDate.Value.AddDays(1).ToString("yyyy-MM-dd")}' and ContainerTypeId not in (14805,14806)";
                if (entity.Combination)
                {
                    sql += $" and t.ReturnEmptyId = 114017";
                }
                if (entity.Maintenance)
                {
                    sql += $" and exists (select Id  from Expense where ExpenseTypeId = 15955 and IsReturn = 0 and TransportationId = t.Id)";
                }
            }
            else
            {
                sql += $" where t.ReturnDate >= '{entity.FromDate.Value.AddDays(-1).ToString("yyyy-MM-dd")}' and t.ReturnDate <= '{entity.ToDate.Value.AddDays(1).ToString("yyyy-MM-dd")}' and ContainerTypeId not in (14805,14806)  and t.Active = 1 and t.ShipDate is not null and t.IsSplitBill = 0";
                if (entity.Combination)
                {
                    sql += $" and t.ReturnEmptyId = 114017";
                }
                if (entity.Maintenance)
                {
                    sql += $" and exists (select Id  from Expense where ExpenseTypeId = 15955 and IsReturn = 1 and TransportationId = t.Id)";
                }
            }
            sql += @$" group by {selects1.Combine()}";
            sql += @$" order by {selects1.Combine()} asc";
            var data = await ConverSqlToDataSet(sql);
            var index = 1;
            if (entity.StartShip)
            {
                worksheet.Cell(2, index).Value = $"Ngày tàu chạy";
                worksheet.Cell(2, index).Style.Alignment.WrapText = true;
                worksheet.Cell(2, index).Style.Font.Bold = true;
                worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                index++;
            }
            if (entity.ExportList)
            {
                worksheet.Cell(2, index).Value = $"List xuất";
                worksheet.Cell(2, index).Style.Alignment.WrapText = true;
                worksheet.Cell(2, index).Style.Font.Bold = true;
                worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                index++;
            }
            if (entity.Route)
            {
                worksheet.Cell(2, index).Value = $"Tuyến đường";
                worksheet.Cell(2, index).Style.Alignment.WrapText = true;
                worksheet.Cell(2, index).Style.Font.Bold = true;
                worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                index++;
            }
            if (entity.BrandShip)
            {
                worksheet.Cell(2, index).Value = $"Hãng tàu";
                worksheet.Cell(2, index).Style.Alignment.WrapText = true;
                worksheet.Cell(2, index).Style.Font.Bold = true;
                worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                index++;
            }
            if (entity.Ship)
            {
                worksheet.Cell(2, index).Value = $"Tàu";
                worksheet.Cell(2, index).Style.Alignment.WrapText = true;
                worksheet.Cell(2, index).Style.Font.Bold = true;
                worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                index++;
            }
            if (entity.Boss)
            {
                worksheet.Cell(2, index).Value = $"Chủ hàng";
                worksheet.Cell(2, index).Style.Alignment.WrapText = true;
                worksheet.Cell(2, index).Style.Font.Bold = true;
                worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                index++;
            }
            if (entity.ContainerType)
            {
                worksheet.Cell(2, index).Value = $"Loại cont";
                worksheet.Cell(2, index).Style.Alignment.WrapText = true;
                worksheet.Cell(2, index).Style.Font.Bold = true;
                worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                index++;
            }
            if (entity.Closing)
            {
                worksheet.Cell(2, index).Value = $"Đơn vị đóng hàng";
                worksheet.Cell(2, index).Style.Alignment.WrapText = true;
                worksheet.Cell(2, index).Style.Font.Bold = true;
                worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                index++;
            }
            if (entity.Commodity)
            {
                worksheet.Cell(2, index).Value = $"Hàng hóa";
                worksheet.Cell(2, index).Style.Alignment.WrapText = true;
                worksheet.Cell(2, index).Style.Font.Bold = true;
                worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                index++;
            }
            if (entity.User)
            {
                worksheet.Cell(2, index).Value = $"Nhân viên bán hàng";
                worksheet.Cell(2, index).Style.Alignment.WrapText = true;
                worksheet.Cell(2, index).Style.Font.Bold = true;
                worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                index++;
            }
            worksheet.Cell(2, index).Value = $"Cont20";
            worksheet.Cell(2, index).Style.Alignment.WrapText = true;
            worksheet.Cell(2, index).Style.Font.Bold = true;
            worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            index++;
            worksheet.Cell(2, index).Value = $"Cont40";
            worksheet.Cell(2, index).Style.Alignment.WrapText = true;
            worksheet.Cell(2, index).Style.Font.Bold = true;
            worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            index++;
            worksheet.Cell(2, index).Value = $"KH 20";
            worksheet.Cell(2, index).Style.Alignment.WrapText = true;
            worksheet.Cell(2, index).Style.Font.Bold = true;
            worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            index++;
            worksheet.Cell(2, index).Value = $"KH 40";
            worksheet.Cell(2, index).Style.Alignment.WrapText = true;
            worksheet.Cell(2, index).Style.Font.Bold = true;
            worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            index++;
            worksheet.Cell(2, index).Value = $"% KH";
            worksheet.Cell(2, index).Style.Alignment.WrapText = true;
            worksheet.Cell(2, index).Style.Font.Bold = true;
            worksheet.Cell(2, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range(2, 1, 2, index).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Range(2, 1, 2, index).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Range(2, 1, 2, index).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Range(2, 1, 2, index).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            worksheet.Cell("A1").Value = $"BÁO CÁO SẢN LƯỢNG {entity.FromDate?.ToString("dd/MM/yyyy")} => {entity.ToDate?.ToString("dd/MM/yyyy")}";
            worksheet.Range(1, 1, index, index).Row(1).Merge();
            worksheet.Cell("A1").Style.Font.FontSize = 14;
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            var i = 3;
            foreach (var itemDetail in data[0])
            {
                var index1 = 1;
                if (entity.StartShip)
                {
                    worksheet.Cell(i, index1).Value = itemDetail["StartShip"] is null ? default(DateTime) : DateTime.Parse(itemDetail["StartShip"].ToString());
                    index1++;
                }
                if (entity.ExportList)
                {
                    worksheet.Cell(i, index1).Value = itemDetail["ExportList"] is null ? null : itemDetail["ExportList"].ToString().DecodeSpecialChar();
                    index1++;
                }
                if (entity.Route)
                {
                    worksheet.Cell(i, index1).Value = itemDetail["Route"] is null ? null : itemDetail["Route"].ToString().DecodeSpecialChar();
                    index1++;
                }
                if (entity.BrandShip)
                {
                    worksheet.Cell(i, index1).Value = itemDetail["BrandShip"] is null ? null : itemDetail["BrandShip"].ToString().DecodeSpecialChar();
                    index1++;
                }
                if (entity.Ship)
                {
                    worksheet.Cell(i, index1).Value = itemDetail["Ship"] is null ? null : itemDetail["Ship"].ToString().DecodeSpecialChar();
                    index1++;
                }
                if (entity.Boss)
                {
                    worksheet.Cell(i, index1).Value = itemDetail["Boss"] is null ? null : itemDetail["Boss"].ToString().DecodeSpecialChar();
                    index1++;
                }
                if (entity.ContainerType)
                {
                    worksheet.Cell(i, index1).Value = itemDetail["ContainerType"] is null ? null : itemDetail["ContainerType"].ToString().DecodeSpecialChar();
                    index1++;
                }
                if (entity.Closing)
                {
                    worksheet.Cell(i, index1).Value = itemDetail["Closing"] is null ? null : itemDetail["Closing"].ToString().DecodeSpecialChar();
                    index1++;
                }
                if (entity.Commodity)
                {
                    worksheet.Cell(i, index1).Value = itemDetail["Commodity"] is null ? null : itemDetail["Commodity"].ToString().DecodeSpecialChar();
                    index1++;
                }
                if (entity.User)
                {
                    worksheet.Cell(i, index1).Value = itemDetail["User"] is null ? null : itemDetail["User"].ToString().DecodeSpecialChar();
                    index1++;
                }
                worksheet.Cell(i, index1).Value = (itemDetail["Cont20"] is null || (itemDetail["Cont20"] != null && itemDetail["Cont20"].ToString() == "0.00000")) ? "" : decimal.Parse(itemDetail["Cont20"].ToString());
                index1++;
                worksheet.Cell(i, index1).Value = (itemDetail["Cont40"] is null || (itemDetail["Cont40"] != null && itemDetail["Cont40"].ToString() == "0.00000")) ? "" : decimal.Parse(itemDetail["Cont40"].ToString());
                index1++;
                worksheet.Cell(i, index1).Value = (itemDetail["EmptyCombination20"] is null || (itemDetail["EmptyCombination20"] != null && itemDetail["EmptyCombination20"].ToString() == "0")) ? "" : decimal.Parse(itemDetail["EmptyCombination20"].ToString());
                index1++;
                worksheet.Cell(i, index1).Value = (itemDetail["EmptyCombination40"] is null || (itemDetail["EmptyCombination40"] != null && itemDetail["EmptyCombination40"].ToString() == "0")) ? "" : decimal.Parse(itemDetail["EmptyCombination40"].ToString());
                index1++;
                worksheet.Cell(i, index1).Value = (itemDetail["Per"] is null || (itemDetail["Per"] != null && itemDetail["Per"].ToString() == "0")) ? "" : decimal.Parse(itemDetail["Per"].ToString());
                worksheet.Range(i, 1, i, index1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Range(i, 1, i, index1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Range(i, 1, i, index1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Range(i, 1, i, index1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                i++;
            }
            worksheet.Cell(i, 1).Value = "";
            worksheet.Cell(i, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(i, 1).Style.Alignment.WrapText = true;
            worksheet.Cell(i, 1).Style.Font.Bold = true;
            worksheet.Range(i, 1, i, index - 5).Row(1).Merge();
            var tt20 = data[0].ToList().Sum(x => decimal.Parse(x["Cont20"].ToString()));
            var tt40 = data[0].ToList().Sum(x => decimal.Parse(x["Cont40"].ToString()));
            var com20 = data[0].ToList().Sum(x => decimal.Parse(x["EmptyCombination20"].ToString()));
            var com40 = data[0].ToList().Sum(x => decimal.Parse(x["EmptyCombination40"].ToString()));
            worksheet.Cell(i, index - 3).Value = tt20 == 0 ? "" : tt20;
            worksheet.Cell(i, index - 4).Value = tt40 == 0 ? "" : tt40;
            worksheet.Cell(i, index - 1).Value = com20 == 0 ? "" : com20;
            worksheet.Cell(i, index - 2).Value = com40 == 0 ? "" : com40;
            worksheet.Cell(i, index - 1).Style.Alignment.WrapText = true;
            worksheet.Cell(i, index - 1).Style.Font.Bold = true;
            worksheet.Cell(i, index - 3).Style.Alignment.WrapText = true;
            worksheet.Cell(i, index - 3).Style.Font.Bold = true;
            worksheet.Cell(i, index - 2).Style.Alignment.WrapText = true;
            worksheet.Cell(i, index - 2).Style.Font.Bold = true;
            worksheet.Cell(i, index - 4).Style.Alignment.WrapText = true;
            worksheet.Cell(i, index - 4).Style.Font.Bold = true;
            worksheet.Cell(i, index).Style.Alignment.WrapText = true;
            worksheet.Cell(i, index).Style.Font.Bold = true;
            worksheet.Columns().AdjustToContents();
            var url = $"BaoCaoSanLuong{entity.FromDate?.ToString("ddMMyyyy")}{entity.ToDate?.ToString("ddMMyyyy")}.xlsx";
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

        [HttpPost("api/Transportation/CheckFee")]
        public async Task<List<Transportation>> CheckFee([FromForm] DateTime FromDate, [FromForm] DateTime ToDate, [FromForm] int ClosingId, [FromForm] string RouteIds, [FromServices] IWebHostEnvironment host, List<IFormFile> fileCheckFee, int type)
        {
            var formFile = fileCheckFee.FirstOrDefault();
            if (formFile == null || formFile.Length <= 0)
            {
                return null;
            }

            if (!Path.GetExtension(formFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var path = GetUploadExcelPath(formFile.FileName, host.WebRootPath);
            EnsureDirectoryExist(path);
            path = IncreaseFileName(path);
            using var stream = FileIO.Create(path);
            await formFile.CopyToAsync(stream);
            Workbook workbook = new Workbook(stream);
            Worksheet worksheet = workbook.Worksheets.FirstOrDefault(x => x.IsVisible);
            var start = 0;
            for (int row = 0; row <= 20; row++)
            {
                if (worksheet.Cells.Rows[row][0].Value != null && (worksheet.Cells.Rows[row][0].Value.ToString() == "1" || worksheet.Cells.Rows[row][0].Value.ToString() == "01"))
                {
                    start = row;
                    break;
                }
            }
            stream.Close();
            if (start == 0)
            {
                throw new ApiException("Số thứ tự phải bắt đầu từ 1!") { StatusCode = HttpStatusCode.BadRequest };
            }
            var list = new List<CheckCompineTransportationVM>();
            for (int row = start; row <= worksheet.Cells.MaxDataRow; row++)
            {
                try
                {
                    if (worksheet.Cells.Rows[row][0].Value is null && worksheet.Cells.Rows[row][0].Value is not int)
                    {
                        break;
                    }
                    var datetimes = worksheet.Cells.Rows[row][1].Value.ToString();
                    DateTime datetime = default(DateTime);
                    if (datetimes.Length == 10)
                    {
                        datetime = DateTime.ParseExact(datetimes, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        datetime = DateTime.Parse(datetimes);
                    }
                    var per = decimal.Parse(worksheet.Cells.Rows[row][22].Value is null || worksheet.Cells.Rows[row][22].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][22].Value.ToString().Replace("%", "").Replace(",", "").Trim());
                    var entity = new CheckCompineTransportationVM()
                    {
                        No = worksheet.Cells.Rows[row][0].Value.ToString().Trim(),
                        Vendor = worksheet.Cells.Rows[0][0].Value.ToString().Trim(),
                        ClosingDate = datetime,
                        Boss = worksheet.Cells.Rows[row][2].Value is null ? null : worksheet.Cells.Rows[row][2].Value.ToString().Trim(),
                        ContainerNo = worksheet.Cells.Rows[row][3].Value is null ? null : worksheet.Cells.Rows[row][3].Value.ToString().Trim(),
                        SealNo = worksheet.Cells.Rows[row][4].Value is null ? null : worksheet.Cells.Rows[row][4].Value.ToString().Trim(),
                        Cont20 = int.Parse(worksheet.Cells.Rows[row][5].Value is null || worksheet.Cells.Rows[row][5].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][5].Value.ToString().Trim()),
                        Cont40 = int.Parse(worksheet.Cells.Rows[row][6].Value is null || worksheet.Cells.Rows[row][6].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][6].Value.ToString().Trim()),
                        Received = worksheet.Cells.Rows[row][7].Value is null ? null : worksheet.Cells.Rows[row][7].Value.ToString().Trim(),
                        PickupEmpty = worksheet.Cells.Rows[row][8].Value is null ? null : worksheet.Cells.Rows[row][8].Value.ToString().Trim(),
                        PortLoading = worksheet.Cells.Rows[row][9].Value is null ? null : worksheet.Cells.Rows[row][9].Value.ToString().Trim(),
                        LiftFee = decimal.Parse(worksheet.Cells.Rows[row][10].Value is null || worksheet.Cells.Rows[row][10].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][10].Value.ToString().Replace(",", "").Trim()),
                        LandingFee = decimal.Parse(worksheet.Cells.Rows[row][11].Value is null || worksheet.Cells.Rows[row][11].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][11].Value.ToString().Replace(",", "").Trim()),
                        FeeVat1 = decimal.Parse(worksheet.Cells.Rows[row][12].Value is null || worksheet.Cells.Rows[row][12].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][12].Value.ToString().Replace(",", "").Trim()),
                        FeeVat2 = decimal.Parse(worksheet.Cells.Rows[row][13].Value is null || worksheet.Cells.Rows[row][13].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][13].Value.ToString().Replace(",", "").Trim()),
                        FeeVat3 = decimal.Parse(worksheet.Cells.Rows[row][14].Value is null || worksheet.Cells.Rows[row][14].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][14].Value.ToString().Replace(",", "").Trim()),
                        Fee1 = decimal.Parse(worksheet.Cells.Rows[row][15].Value is null || worksheet.Cells.Rows[row][15].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][15].Value.ToString().Replace(",", "").Trim()),
                        Fee2 = decimal.Parse(worksheet.Cells.Rows[row][16].Value is null || worksheet.Cells.Rows[row][16].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][16].Value.ToString().Replace(",", "").Trim()),
                        Fee3 = decimal.Parse(worksheet.Cells.Rows[row][17].Value is null || worksheet.Cells.Rows[row][17].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][17].Value.ToString().Replace(",", "").Trim()),
                        Fee4 = decimal.Parse(worksheet.Cells.Rows[row][18].Value is null || worksheet.Cells.Rows[row][18].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][18].Value.ToString().Replace(",", "").Trim()),
                        Fee5 = decimal.Parse(worksheet.Cells.Rows[row][19].Value is null || worksheet.Cells.Rows[row][19].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][19].Value.ToString().Replace(",", "").Trim()),
                        Fee6 = decimal.Parse(worksheet.Cells.Rows[row][20].Value is null || worksheet.Cells.Rows[row][20].Value.ToString().Trim() == "" ? "0" : worksheet.Cells.Rows[row][20].Value.ToString().Replace(",", "").Trim()),
                        CollectOnSupPrice = decimal.Parse(worksheet.Cells.Rows[row][21].Value is null || worksheet.Cells.Rows[row][21].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][21].Value.ToString().Replace(",", "").Trim()),
                        ClosingPercentCheck = per > 0 && per < 10 ? per * 100 : per,
                        TotalPriceAfterTax = decimal.Parse(worksheet.Cells.Rows[row][23].Value is null || worksheet.Cells.Rows[row][23].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][23].Value.ToString().Replace(",", "").Trim())
                    };
                    list.Add(entity);
                }
                catch (Exception e)
                {
                    throw new ApiException($"Dòng {row} bị lỗi: {e.Message}") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            var routes = RouteIds.Split(",").Where(x => x != null).Select(x => int.Parse(x)).ToList();
            if (type == 1)
            {
                var qr = db.Transportation.Where(x =>
                x.ClosingDate.Value.Date >= FromDate
                && x.ClosingDate.Value.Date <= ToDate
                && x.RouteId != null
                && x.ContainerNo != null
                && routes.Contains(x.RouteId.Value)
                && x.ClosingId == ClosingId).OrderBy(x => x.ClosingDate).AsQueryable();
                var transportations = await qr.ToListAsync();
                var lastHis = new CheckFeeHistory()
                {
                    ClosingId = ClosingId,
                    FromDate = FromDate,
                    ToDate = ToDate,
                    TypeId = type
                };
                SetAuditInfo(lastHis);
                db.Add(lastHis);
                await db.SaveChangesAsync();
                var checks = list.Select(x =>
                {
                    var tran = transportations.FirstOrDefault(y => y.ContainerNo != null && y.ContainerNo.ToLower().Trim() == x.ContainerNo.ToLower()
                    && y.ClosingDate.Value.Date == x.ClosingDate.Value.Date);
                    if (tran != null)
                    {
                        tran.OrderExcel = int.Parse(x.No);
                        tran.CheckFeeHistoryId = lastHis.Id;
                        tran.ReceivedCheck = x.Received;
                        tran.ClosingDateCheck = x.ClosingDate;
                        tran.SealCheck = x.SealNo;
                        tran.ContainerNoCheck = x.ContainerNo;
                        tran.BossCheck = x.Boss;
                        tran.Cont20Check = x.Cont20;
                        tran.Cont40Check = x.Cont40;
                        tran.ClosingPercentCheck = x.ClosingPercentCheck;
                        tran.PickupEmptyCheck = x.PickupEmpty;
                        tran.PortLoadingCheck = x.PortLoading;
                        tran.LiftFeeCheck = x.LiftFee;
                        tran.LandingFeeCheck = x.LandingFee;
                        tran.CollectOnBehaftInvoinceNoFeeCheck = x.FeeVat1 + x.FeeVat2 + x.FeeVat3;
                        tran.FeeVat1 = x.FeeVat1;
                        tran.FeeVat2 = x.FeeVat2;
                        tran.FeeVat3 = x.FeeVat3;
                        tran.FeeVat1Upload = x.FeeVat1;
                        tran.FeeVat2Upload = x.FeeVat2;
                        tran.FeeVat3Upload = x.FeeVat3;
                        tran.Fee1 = x.Fee1;
                        tran.Fee2 = x.Fee2;
                        tran.Fee3 = x.Fee3;
                        tran.Fee4 = x.Fee4;
                        tran.Fee5 = x.Fee5;
                        tran.Fee6 = x.Fee6;
                        tran.Fee1Upload = x.Fee1;
                        tran.Fee2Upload = x.Fee2;
                        tran.Fee3Upload = x.Fee3;
                        tran.Fee4Upload = x.Fee4;
                        tran.Fee5Upload = x.Fee5;
                        tran.Fee6Upload = x.Fee6;
                        tran.CollectOnBehaftFeeCheck = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5 + x.Fee6;
                        tran.CollectOnSupPriceCheck = x.CollectOnSupPrice;
                        tran.TotalPriceAfterTaxCheck = x.TotalPriceAfterTax;
                        tran.ReceivedCheckUpload = x.Received;
                        tran.ClosingDateUpload = x.ClosingDate;
                        tran.SealCheckUpload = x.SealNo;
                        tran.ContainerNoUpload = x.ContainerNo;
                        tran.Cont20CheckUpload = x.Cont20;
                        tran.Cont40CheckUpload = x.Cont40;
                        tran.ClosingPercentUpload = x.ClosingPercentCheck;
                        tran.PickupEmptyUpload = x.PickupEmpty;
                        tran.PortLoadingUpload = x.PortLoading;
                        tran.LiftFeeCheckUpload = x.LiftFee;
                        tran.LandingFeeUpload = x.LandingFee;
                        tran.CollectOnBehaftInvoinceNoFeeUpload = x.FeeVat1 + x.FeeVat2 + x.FeeVat3;
                        tran.CollectOnBehaftFeeUpload = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5 + x.Fee6;
                        tran.CollectOnSupPriceUpload = x.CollectOnSupPrice;
                        tran.TotalPriceAfterTaxUpload = x.TotalPriceAfterTax;
                    }
                    else
                    {
                        tran = new Transportation()
                        {
                            ClosingId = ClosingId,
                            OrderExcel = int.Parse(x.No),
                            ReceivedCheck = x.Received,
                            ReceivedCheckUpload = x.Received,
                            ClosingDateCheck = x.ClosingDate,
                            ClosingDateUpload = x.ClosingDate,
                            SealCheck = x.SealNo,
                            SealCheckUpload = x.SealNo,
                            BossCheck = x.Boss,
                            BossCheckUpload = x.Boss,
                            ContainerNoCheck = x.ContainerNo,
                            ContainerNoUpload = x.ContainerNo,
                            Cont20Check = x.Cont20,
                            Cont20CheckUpload = x.Cont20,
                            Cont40Check = x.Cont40,
                            Cont40CheckUpload = x.Cont40,
                            PickupEmptyCheck = x.PickupEmpty,
                            PickupEmptyUpload = x.PickupEmpty,
                            PortLoadingCheck = x.PortLoading,
                            PortLoadingUpload = x.PortLoading,
                            ClosingPercentCheck = x.ClosingPercentCheck,
                            ClosingPercentUpload = x.ClosingPercentCheck,
                            LiftFeeCheck = x.LiftFee,
                            LiftFeeCheckUpload = x.LiftFee,
                            LandingFeeCheck = x.LandingFee,
                            LandingFeeUpload = x.LandingFee,
                            FeeVat1 = x.FeeVat1,
                            FeeVat2 = x.FeeVat2,
                            FeeVat3 = x.FeeVat3,
                            FeeVat1Upload = x.FeeVat1,
                            FeeVat2Upload = x.FeeVat2,
                            FeeVat3Upload = x.FeeVat3,
                            Fee1 = x.Fee1,
                            Fee2 = x.Fee2,
                            Fee3 = x.Fee3,
                            Fee4 = x.Fee4,
                            Fee5 = x.Fee5,
                            Fee6 = x.Fee6,
                            Fee1Upload = x.Fee1,
                            Fee2Upload = x.Fee2,
                            Fee3Upload = x.Fee3,
                            Fee4Upload = x.Fee4,
                            Fee5Upload = x.Fee5,
                            Fee6Upload = x.Fee6,
                            CollectOnBehaftInvoinceNoFeeCheck = x.FeeVat1 + x.FeeVat2 + x.FeeVat3,
                            CollectOnBehaftInvoinceNoFeeUpload = x.FeeVat1 + x.FeeVat2 + x.FeeVat3,
                            CollectOnBehaftFeeCheck = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5 + x.Fee6,
                            CollectOnBehaftFeeUpload = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5 + x.Fee6,
                            CollectOnSupPriceCheck = x.CollectOnSupPrice,
                            CollectOnSupPriceUpload = x.CollectOnSupPrice,
                            TotalPriceAfterTaxCheck = x.TotalPriceAfterTax,
                            TotalPriceAfterTaxUpload = x.TotalPriceAfterTax,
                            CheckFeeHistoryId = lastHis.Id
                        };
                    }
                    return tran;
                }).ToList();
                await db.SaveChangesAsync();
                return checks;
            }
            else
            {
                var qr = db.Transportation.Where(x =>
                x.ReturnDate.Value.Date >= FromDate
                && x.ReturnDate.Value.Date <= ToDate
                && x.RouteId != null
                && x.ContainerNo != null
                && routes.Contains(x.RouteId.Value)
                && x.ReturnVendorId == ClosingId).OrderBy(x => x.ReturnDate).AsQueryable();
                var transportations = await qr.ToListAsync();
                var lastHis = new CheckFeeHistory()
                {
                    ClosingId = ClosingId,
                    FromDate = FromDate,
                    ToDate = ToDate,
                    TypeId = type
                };
                SetAuditInfo(lastHis);
                db.Add(lastHis);
                await db.SaveChangesAsync();
                var checks = list.Select(x =>
                {
                    var tran = transportations.FirstOrDefault(y => y.ContainerNo != null && y.ContainerNo.ToLower().Trim() == x.ContainerNo.ToLower()
                    && y.ReturnDate.Value.Date == x.ClosingDate.Value.Date);
                    if (tran != null)
                    {
                        tran.OrderExcelReturn = int.Parse(x.No);
                        tran.CheckFeeHistoryReturnId = lastHis.Id;
                        tran.ReceivedReturnCheck = x.Received;
                        tran.ClosingDateReturnCheck = x.ClosingDate;
                        tran.SealReturnCheck = x.SealNo;
                        tran.ContainerNoReturnCheck = x.ContainerNo;
                        tran.BossReturnCheck = x.Boss;
                        tran.Cont20ReturnCheck = x.Cont20;
                        tran.Cont40ReturnCheck = x.Cont40;
                        tran.ClosingPercentReturnCheck = x.ClosingPercentCheck;
                        tran.PickupEmptyReturnCheck = x.PickupEmpty;
                        tran.PortLoadingReturnCheck = x.PortLoading;
                        tran.LiftFeeReturnCheck = x.LiftFee;
                        tran.LandingFeeReturnCheck = x.LandingFee;
                        tran.CollectOnBehaftInvoinceNoFeeReturnCheck = x.FeeVat1 + x.FeeVat2 + x.FeeVat3;
                        tran.FeeVatReturn = x.FeeVat1;
                        tran.FeeVatReturn2 = x.FeeVat2;
                        tran.FeeVatReturn3 = x.FeeVat3;
                        tran.FeeVat1UploadReturn = x.FeeVat1;
                        tran.FeeVat2UploadReturn = x.FeeVat2;
                        tran.FeeVat3UploadReturn = x.FeeVat3;
                        tran.FeeReturn1 = x.Fee1;
                        tran.FeeReturn2 = x.Fee2;
                        tran.FeeReturn3 = x.Fee3;
                        tran.FeeReturn4 = x.Fee4;
                        tran.FeeReturn5 = x.Fee5;
                        tran.FeeReturn6 = x.Fee6;
                        tran.Fee1UploadReturn = x.Fee1;
                        tran.Fee2UploadReturn = x.Fee2;
                        tran.Fee3UploadReturn = x.Fee3;
                        tran.Fee4UploadReturn = x.Fee4;
                        tran.Fee5UploadReturn = x.Fee5;
                        tran.Fee6UploadReturn = x.Fee6;
                        tran.CollectOnBehaftFeeReturnCheck = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5 + x.Fee6;
                        tran.CollectOnSupPriceReturnCheck = x.CollectOnSupPrice;
                        tran.TotalPriceAfterTaxReturnCheck = x.TotalPriceAfterTax;
                        tran.ReceivedCheckReturnUpload = x.Received;
                        tran.ClosingDateReturnUpload = x.ClosingDate;
                        tran.SealCheckReturnUpload = x.SealNo;
                        tran.ContainerNoReturnUpload = x.ContainerNo;
                        tran.Cont20CheckReturnUpload = x.Cont20;
                        tran.Cont40CheckReturnUpload = x.Cont40;
                        tran.ClosingPercentReturnUpload = x.ClosingPercentCheck;
                        tran.PickupEmptyReturnUpload = x.PickupEmpty;
                        tran.PortLoadingReturnUpload = x.PortLoading;
                        tran.LiftFeeCheckReturnUpload = x.LiftFee;
                        tran.LandingFeeReturnUpload = x.LandingFee;
                        tran.CollectOnBehaftInvoinceNoFeeReturnUpload = x.FeeVat1 + x.FeeVat2 + x.FeeVat3;
                        tran.CollectOnBehaftFeeReturnUpload = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5 + x.Fee6;
                        tran.CollectOnSupPriceReturnUpload = x.CollectOnSupPrice;
                        tran.TotalPriceAfterTaxReturnUpload = x.TotalPriceAfterTax;
                    }
                    else
                    {
                        tran = new Transportation();
                        tran.ReturnVendorId = ClosingId;
                        tran.OrderExcelReturn = int.Parse(x.No);
                        tran.CheckFeeHistoryReturnId = lastHis.Id;
                        tran.ReceivedReturnCheck = x.Received;
                        tran.ClosingDateReturnCheck = x.ClosingDate;
                        tran.SealReturnCheck = x.SealNo;
                        tran.ContainerNoReturnCheck = x.ContainerNo;
                        tran.BossReturnCheck = x.Boss;
                        tran.Cont20ReturnCheck = x.Cont20;
                        tran.Cont40ReturnCheck = x.Cont40;
                        tran.ClosingPercentReturnCheck = x.ClosingPercentCheck;
                        tran.PickupEmptyReturnCheck = x.PickupEmpty;
                        tran.PortLoadingReturnCheck = x.PortLoading;
                        tran.LiftFeeReturnCheck = x.LiftFee;
                        tran.LandingFeeReturnCheck = x.LandingFee;
                        tran.CollectOnBehaftInvoinceNoFeeReturnCheck = x.FeeVat1 + x.FeeVat2 + x.FeeVat3;
                        tran.FeeVatReturn = x.FeeVat1;
                        tran.FeeVatReturn2 = x.FeeVat2;
                        tran.FeeVatReturn3 = x.FeeVat3;
                        tran.FeeVat1UploadReturn = x.FeeVat1;
                        tran.FeeVat2UploadReturn = x.FeeVat2;
                        tran.FeeVat3UploadReturn = x.FeeVat3;
                        tran.FeeReturn1 = x.Fee1;
                        tran.FeeReturn2 = x.Fee2;
                        tran.FeeReturn3 = x.Fee3;
                        tran.FeeReturn4 = x.Fee4;
                        tran.FeeReturn5 = x.Fee5;
                        tran.FeeReturn6 = x.Fee6;
                        tran.Fee1UploadReturn = x.Fee1;
                        tran.Fee2UploadReturn = x.Fee2;
                        tran.Fee3UploadReturn = x.Fee3;
                        tran.Fee4UploadReturn = x.Fee4;
                        tran.Fee5UploadReturn = x.Fee5;
                        tran.Fee6UploadReturn = x.Fee6;
                        tran.CollectOnBehaftFeeReturnCheck = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5 + x.Fee6;
                        tran.CollectOnSupPriceReturnCheck = x.CollectOnSupPrice;
                        tran.TotalPriceAfterTaxReturnCheck = x.TotalPriceAfterTax;
                        tran.ReceivedCheckReturnUpload = x.Received;
                        tran.ClosingDateReturnUpload = x.ClosingDate;
                        tran.SealCheckReturnUpload = x.SealNo;
                        tran.ContainerNoReturnUpload = x.ContainerNo;
                        tran.Cont20CheckReturnUpload = x.Cont20;
                        tran.Cont40CheckReturnUpload = x.Cont40;
                        tran.ClosingPercentReturnUpload = x.ClosingPercentCheck;
                        tran.PickupEmptyReturnUpload = x.PickupEmpty;
                        tran.PortLoadingReturnUpload = x.PortLoading;
                        tran.LiftFeeCheckReturnUpload = x.LiftFee;
                        tran.LandingFeeReturnUpload = x.LandingFee;
                        tran.CollectOnBehaftInvoinceNoFeeReturnUpload = x.FeeVat1 + x.FeeVat2 + x.FeeVat3;
                        tran.CollectOnBehaftFeeReturnUpload = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5 + x.Fee6;
                        tran.CollectOnSupPriceReturnUpload = x.CollectOnSupPrice;
                        tran.TotalPriceAfterTaxReturnUpload = x.TotalPriceAfterTax;
                    }
                    return tran;
                }).ToList();
                await db.SaveChangesAsync();
                return checks;
            }
        }

        [HttpPost("api/Transportation/SetStartShip")]
        public async Task<int> SetStartShip([FromBody] Transportation entity)
        {
            var check = await db.Transportation.Where(x => x.ShipId == entity.ShipId && x.Trip == entity.Trip && (x.BrandShipId == entity.BrandShipId || entity.BrandShipId == null) && entity.RouteIds.Contains(x.RouteId.Value)).CountAsync();
            if (check == 0)
            {
                return check;
            }
            var cmd = $"Update [{nameof(Transportation)}] set [ShipDate] = '{entity.ShipDate.Value.ToString("yyyy-MM-dd")}',[PortLiftId] = '{entity.PortLiftId}'" +
                $" ,[ExportListReturnId] = '{VendorId}',[IsLiftFee] = {(entity.IsLiftFee ? 1 : 0)},[UserReturnId] = '{UserId}'" +
                $" where ShipId = '{entity.ShipId}' and (BrandShipId = '{entity.BrandShipId}' or '{entity.BrandShipId}' = '') and Trip = '{entity.Trip}' and RouteId in ({entity.RouteIds.Combine()});" +
                @$" update Transportation set DemDate = DATEADD(day,(select top 1 [Day] from SettingTransportation where RouteId = t.RouteId and BranchShipId = isnull(t.LineId,t.BrandShipId) and StartDate <= t.ShipDate order by StartDate desc)-1,t.ShipDate)
						from Transportation t
						join MasterData on MasterData.Id = t.ContainerTypeId
						where MasterData.Description not like N'%tank%'
						and t.ShipId = '{entity.ShipId}' and (t.BrandShipId = '{entity.BrandShipId}' or '{entity.BrandShipId}' = '') and t.Trip = '{entity.Trip}' and t.RouteId in ({entity.RouteIds.Combine()});
                        update Transportation set ReturnDate = Transportation.ShipDate, ReturnId = r.BranchId
					    from Transportation
					    join [Route] r on r.Id = Transportation.RouteId
					    where Transportation.SplitBill is not null 
					    and Transportation.SplitBill <> '' 
					    and Transportation.SplitBill <> N'bill riêng'
					    and Transportation.ShipDate is not null
					    and Transportation.ReturnDate is null
                        and Transportation.ShipId = '{entity.ShipId}' and (Transportation.BrandShipId = '{entity.BrandShipId}' or '{entity.BrandShipId}' = '') and Transportation.Trip = '{entity.Trip}' and Transportation.RouteId in ({entity.RouteIds.Combine()});
                        update Transportation set IsSplitBill = 
					    (case when (l.Description is not null and l.Description like N'%Tách Bill Cho Khách%') 
					    or (Transportation.SplitBill is not null and trim(Transportation.SplitBill) <> N'') then 1 else 0 end)
					    from Transportation
					    left join Location l on Transportation.ReturnId = l.Id
					    where Transportation.ShipId = '{entity.ShipId}' and (Transportation.BrandShipId = '{entity.BrandShipId}' or '{entity.BrandShipId}' = '') and Transportation.Trip = '{entity.Trip}' and Transportation.RouteId in ({entity.RouteIds.Combine()});

                        update Transportation set ReturnLiftFee = (select top 1 CASE
					    WHEN Transportation.IsLiftFee = 1 THEN UnitPrice1
					    ELSE UnitPrice
					    END as UnitPrice from Quotation 
					    where TypeId = 7596
					    and ContainerTypeId = Transportation.ContainerTypeId 
					    and LocationId = Transportation.PortLiftId 
					    and (StartDate <= Transportation.ShipDate or Transportation.ShipDate is null) order by StartDate desc),
                        Dem = (case when DATEDIFF(DAY,Transportation.DemDate,Transportation.ReturnDate) <= 0 then null else DATEDIFF(DAY,Transportation.DemDate,Transportation.ReturnDate) end)
					    from Transportation
					    where Transportation.ShipDate is not null and Transportation.ShipId = '{entity.ShipId}' and (Transportation.BrandShipId = '{entity.BrandShipId}' or '{entity.BrandShipId}' = '') and Transportation.Trip = '{entity.Trip}' and Transportation.RouteId in ({entity.RouteIds.Combine()});
                        ";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return check;
        }

        [HttpGet("api/[Controller]/GetByRole")]
        public Task<OdataResult<Transportation>> UserClick(ODataQueryOptions<Transportation> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(Transportation).Name}]
                    where 1 = 1";
            if (RoleIds.Contains(10))
            {
                sql += @$" and (UserId = {UserId} or InsertedBy = {UserId} or User2Id = {UserId})";
            }
            else if (RoleIds.Contains(43))
            {
                sql += @$" and (UserId = 78 or InsertedBy = {UserId} or UserId = {UserId} or User2Id = {UserId})";
            }
            else if (RoleIds.Contains(17))
            {
                sql += @$" and (UserId = 78 or UserId = {UserId} or User2Id = {UserId})";
            }
            else if (RoleIds.Contains(25))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where TypeId = 25045 and UserId = {UserId}))";
            }
            else if (RoleIds.Contains(27))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId}))";
            }
            var qr = db.Transportation.FromSqlRaw(sql).AsNoTracking();
            return ApplyQuery(options, qr, sql: sql);
        }

        [HttpGet("api/[Controller]/GetByRoleReturn")]
        public Task<OdataResult<Transportation>> GetByRoleReturn(ODataQueryOptions<Transportation> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(Transportation).Name}]
                    where 1 = 1";
            if (RoleIds.Contains(10))
            {
                sql += @$" and (UserId = {UserId} or InsertedBy = {UserId})";
            }
            else if (RoleIds.Contains(17))
            {
                sql += @$" and (UserId = 78 or UserId = {UserId} or User2Id = {UserId})";
            }
            else if (RoleIds.Contains(32))
            {
                sql += @$" and Active = 1";
            }
            else if (RoleIds.Contains(25) || RoleIds.Contains(27) || RoleIds.Contains(22))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId}))";
            }
            var qr = db.Transportation.FromSqlRaw(sql).AsNoTracking();
            return ApplyQuery(options, qr, sql: sql);
        }

        [HttpGet("api/[Controller]/GetTransportationReturn")]
        public Task<OdataResult<Transportation>> GetTransportationReturn(ODataQueryOptions<Transportation> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(Transportation).Name}]
                    where 1 = 1";
            if (RoleIds.Contains(10))
            {
                sql += @$" and (UserId = {UserId} or InsertedBy = {UserId})";
            }
            else if (RoleIds.Contains(17))
            {
                sql += @$" and (UserId = 78 or UserId = {UserId} or User2Id = {UserId})";
            }
            else if (RoleIds.Contains(27))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId}))";
            }
            else if (RoleIds.Contains(25) || RoleIds.Contains(22))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25044))";
            }
            var qr = db.Transportation.FromSqlRaw(sql).AsNoTracking();
            return ApplyQuery(options, qr, sql: sql);
        }

        [HttpPost("api/[Controller]/ReportGroupBy")]
        public async Task<List<TranGroupVM>> ReportGroupBy([FromBody] Transportation options)
        {
            var query = db.Transportation.Where(x => x.ClosingDate != null).AsNoTracking();
            if (options.ContainerTypeId is not null)
            {
                query = query.Where(x => x.ContainerTypeId == options.ContainerTypeId);
            }
            if (options.FromDate is not null)
            {
                query = query.Where(x => x.ClosingDate.Value.Date >= options.FromDate.Value.Date);
            }
            if (options.ToDate is not null)
            {
                query = query.Where(x => x.ClosingDate.Value.Date <= options.ToDate.Value.Date);
            }
            if (options.BrandShipId is not null)
            {
                query = query.Where(x => x.BrandShipId == options.BrandShipId);
            }
            if (options.ShipId is not null)
            {
                query = query.Where(x => x.ShipId == options.ShipId);
            }
            var rs = await query.ToListAsync();
            var groupByPercentileQuery =
                                        from tran in rs
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
                                            tran.StartShip,
                                            tran.ContainerTypeId,
                                            tran.PolicyId
                                        } into tranGroup
                                        select new TranGroupVM()
                                        {
                                            Month = tranGroup.Key.Month,
                                            ExportListId = tranGroup.Key.ExportListId,
                                            Year = tranGroup.Key.Year,
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
                                            ShipUnitPrice = tranGroup.Where(x => x.ShipUnitPrice != null).Sum(x => x.ShipUnitPrice.Value),
                                            ShipPrice = tranGroup.Where(x => x.ShipPrice != null).Sum(x => x.ShipPrice.Value),
                                            ShipPolicyPrice = tranGroup.Where(x => x.ShipPolicyPrice != null).Sum(x => x.ShipPolicyPrice.Value),
                                        };
            return groupByPercentileQuery.OrderByDescending(x => x.Month).ThenByDescending(x => x.Year).ToList();
        }
        #region Core
        public override async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ViewSumary([FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config, [FromBody] string sum, [FromQuery] string group, [FromQuery] string tablename, [FromQuery] string refname, [FromQuery] string formatsumary, [FromQuery] string orderby, [FromQuery] string sql, [FromQuery] string where)
        {
            var connectionStr = _config.GetConnectionString("Default");
            using var con = new SqlConnection(connectionStr);
            var reportQuery = string.Empty;
            if (!sql.IsNullOrWhiteSpace())
            {
                reportQuery = $@"select {group},{formatsumary} as TotalRecord,{sum}
                                  from ({sql})  as [{tablename}] where 1=1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
            }
            else
            {
                reportQuery = $@"select {group},{formatsumary} as TotalRecord,{sum}
                                 from [{tablename}]
                                 where 1 = 1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")} ";
                if (RoleIds.Contains(10))
                {
                    reportQuery += @$" and (UserId = {UserId})";
                }
                else if (RoleIds.Contains(43))
                {
                    reportQuery += @$" and (UserId = {UserId} or UserId = 78)";
                }
                else if (RoleIds.Contains(17))
                {
                    reportQuery += @$" and (UserId = {UserId} or UserId = 78)";
                }
                else if (RoleIds.Contains(25))
                {
                    reportQuery += @$" and (RouteId in (select RouteId from UserRoute where TypeId = 25045 and UserId = {UserId}))";
                }
                else if (RoleIds.Contains(27))
                {
                    reportQuery += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId}))";
                }
            }
            reportQuery += $@" group by {group}
                                 order by {formatsumary} {orderby} ";
            if (!refname.IsNullOrEmpty())
            {
                if (!sql.IsNullOrWhiteSpace())
                {
                    reportQuery += $@" select *
                                 from [{refname}] 
                                 where Id in (select distinct {group}
                                 from ({sql}) as [{tablename}] 
                                  where 1=1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")})";
                }
                else
                {
                    reportQuery += $@" select *
                                 from [{refname}] 
                                 where Id in (select distinct {group}
                                              from [{tablename}]
                                 where 1 = 1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";

                    if (RoleIds.Contains(10))
                    {
                        reportQuery += @$" and (UserId = {UserId})";
                    }
                    else if (RoleIds.Contains(43))
                    {
                        reportQuery += @$" and (UserId = {UserId}  or UserId = 78)";
                    }
                    else if (RoleIds.Contains(17))
                    {
                        reportQuery += @$" and (UserId = {UserId} or UserId = 78)";
                    }
                    else if (RoleIds.Contains(25))
                    {
                        reportQuery += @$" and (RouteId in (select RouteId from UserRoute where TypeId = 25045 and UserId = {UserId}))";
                    }
                    else if (RoleIds.Contains(27))
                    {
                        reportQuery += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId}))";
                    }
                    reportQuery += ")";
                }
            }
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

        public override async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> SubTotal(
            [FromServices] IServiceProvider serviceProvider
            , [FromServices] IConfiguration config
            , [FromBody] string sum
            , [FromQuery] string group
            , [FromQuery] string tablename
            , [FromQuery] string refname
            , [FromQuery] string formatsumary
            , [FromQuery] string orderby
            , [FromQuery] string sql
            , [FromQuery] bool showNull
            , [FromQuery] string datetimeField
            , [FromQuery] string where)
        {
            var connectionStr = _config.GetConnectionString("Default");
            using var con = new SqlConnection(connectionStr);
            var reportQuery = string.Empty;
            if (!sql.IsNullOrWhiteSpace())
            {
                reportQuery = $@"select {sum}
                                  from ({sql})  as [{tablename}] where 1=1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
            }
            else
            {
                reportQuery = $@"select {sum}
                                  from [{tablename}] where 1=1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
                if (RoleIds.Contains(10))
                {
                    reportQuery += @$" and ([{tablename}].UserId = {UserId})";
                }
                else if (RoleIds.Contains(43))
                {
                    reportQuery += @$" and ([{tablename}].UserId = {UserId} or UserId = 78)";
                }
                else if (RoleIds.Contains(17))
                {
                    reportQuery += @$" and ([{tablename}].UserId = {UserId} or UserId = 78)";
                }
                else if (RoleIds.Contains(25))
                {
                    reportQuery += @$" and ([{tablename}].RouteId in (select RouteId from UserRoute where TypeId = 25045 and UserId = {UserId}))";
                }
                else if (RoleIds.Contains(27))
                {
                    reportQuery += @$" and ([{tablename}].RouteId in (select RouteId from UserRoute where UserId = {UserId}))";
                }
            }
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
        public static string ConvertTextEn(string text)
        {
            return text is null || text == "" ? "" : Regex.Replace(text.ToLower().Trim(), @"\s+", " ");
        }

        public static string ConvertTextVn(string text)
        {
            return text is null || text == "" ? "" : Regex.Replace(text.Trim(), @"\s+", " ");
        }
        #endregion
        #region  Request
        [HttpPost("api/Transportation/RequestUnLock")]
        public async Task RequestUnLock([FromBody] TransportationRequestDetails transportationRequestDetails)
        {
            var check = await db.TransportationRequest.Where(x => x.TransportationId == transportationRequestDetails.TransportationId && x.Active).ToListAsync();
            if (check.Count > 0)
            {
                throw new ApiException("Đã có yêu cầu mở khóa");
            }
            var entityType = _entitySvc.GetEntity(typeof(Transportation).Name);
            var approvalConfig = await db.ApprovalConfig.AsNoTracking().OrderBy(x => x.Level)
                .Where(x => x.Active && x.EntityId == entityType.Id).ToListAsync();
            if (approvalConfig.Nothing())
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
            if (matchApprovalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            if (approvalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var listUser = await (
                from user in db.User
                join userRole in db.UserRole on user.Id equals userRole.UserId
                join role in db.Role on userRole.RoleId equals role.Id
                where userRole.RoleId == matchApprovalConfig.RoleId
                select user
            ).ToListAsync();
            if (listUser.HasElement())
            {
                var tranRequest = new TransportationRequest();
                tranRequest.Id = 0;
                tranRequest.IsRequestUnLockExploit = true;
                tranRequest.ReasonUnLockExploit = transportationRequestDetails.Reason;
                tranRequest.TransportationId = transportationRequestDetails.TransportationId;
                tranRequest.StatusId = (int)ApprovalStatusEnum.Approving;
                SetAuditInfo(tranRequest);
                db.Add(tranRequest);
                await db.SaveChangesAsync();
                var transportationRequestDetailsDB = await db.TransportationRequestDetails.Where(x => x.Id == transportationRequestDetails.Id).FirstOrDefaultAsync();
                transportationRequestDetailsDB.TransportationRequestId = tranRequest.Id;
                transportationRequestDetailsDB.StatusId = (int)ApprovalStatusEnum.Approving;
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = new List<TaskNotification>();
                foreach (var user in listUser)
                {
                    var task = new TaskNotification()
                    {
                        Title = $"{currentUser.FullName}",
                        Description = $"Đã gửi yêu cầu thay đổi (Khai thác)",
                        EntityId = _entitySvc.GetEntity(typeof(TransportationRequest).Name).Id,
                        RecordId = tranRequest.Id,
                        Attachment = "fal fa-paper-plane",
                        AssignedId = user.Id,
                        StatusId = (int)TaskStateEnum.UnreadStatus,
                        RemindBefore = 540,
                        Deadline = DateTime.Now,
                    };
                    SetAuditInfo(task);
                    db.AddRange(task);
                    tasks.Add(task);
                }
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(tasks);
            }
        }

        [HttpPost("api/Transportation/RequestUnLockAccountant")]
        public async Task RequestUnLockAccountant([FromBody] TransportationRequestDetails transportationRequestDetails)
        {
            var check = await db.TransportationRequest.Where(x => x.TransportationId == transportationRequestDetails.TransportationId && x.Active).ToListAsync();
            if (check.Count > 0)
            {
                throw new ApiException("Đã có yêu cầu mở khóa");
            }
            var entityType = _entitySvc.GetEntity(typeof(Transportation).Name);
            var approvalConfig = await db.ApprovalConfig.AsNoTracking().OrderBy(x => x.Level)
                .Where(x => x.Active && x.EntityId == entityType.Id).ToListAsync();
            if (approvalConfig.Nothing())
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
            if (matchApprovalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            if (approvalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var listUser = await (
                from user in db.User
                join userRole in db.UserRole on user.Id equals userRole.UserId
                join role in db.Role on userRole.RoleId equals role.Id
                where userRole.RoleId == matchApprovalConfig.RoleId
                select user
            ).ToListAsync();
            if (listUser.HasElement())
            {
                var tranRequest = new TransportationRequest();
                tranRequest.Id = 0;
                tranRequest.IsRequestUnLockAccountant = true;
                tranRequest.ReasonUnLockAccountant = transportationRequestDetails.Reason;
                tranRequest.TransportationId = transportationRequestDetails.TransportationId;
                tranRequest.StatusId = (int)ApprovalStatusEnum.Approving;
                SetAuditInfo(tranRequest);
                db.Add(tranRequest);
                await db.SaveChangesAsync();
                var transportationRequestDetailsDB = await db.TransportationRequestDetails.Where(x => x.Id == transportationRequestDetails.Id).FirstOrDefaultAsync();
                transportationRequestDetailsDB.TransportationRequestId = tranRequest.Id;
                transportationRequestDetailsDB.StatusId = (int)ApprovalStatusEnum.Approving;
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = new List<TaskNotification>();
                foreach (var user in listUser)
                {
                    var task = new TaskNotification()
                    {
                        Title = $"{currentUser.FullName}",
                        Description = $"Đã gửi yêu cầu thay đổi (Kế toán)",
                        EntityId = _entitySvc.GetEntity(typeof(TransportationRequest).Name).Id,
                        RecordId = tranRequest.Id,
                        Attachment = "fal fa-paper-plane",
                        AssignedId = user.Id,
                        StatusId = (int)TaskStateEnum.UnreadStatus,
                        RemindBefore = 540,
                        Deadline = DateTime.Now,
                    };
                    SetAuditInfo(task);
                    db.AddRange(task);
                    tasks.Add(task);
                }
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(tasks);
            }
        }

        [HttpPost("api/Transportation/RequestUnLockAll")]
        public async Task RequestUnLockAll([FromBody] TransportationRequestDetails transportationRequestDetails)
        {
            var check = await db.TransportationRequest.Where(x => x.TransportationId == transportationRequestDetails.TransportationId && x.Active).ToListAsync();
            if (check.Count > 0)
            {
                throw new ApiException("Đã có yêu cầu mở khóa");
            }
            var entityType = _entitySvc.GetEntity(typeof(Transportation).Name);
            var approvalConfig = await db.ApprovalConfig.AsNoTracking().OrderBy(x => x.Level)
                .Where(x => x.Active && x.EntityId == entityType.Id).ToListAsync();
            if (approvalConfig.Nothing())
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
            if (matchApprovalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            if (approvalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var listUser = await (
                from user in db.User
                join userRole in db.UserRole on user.Id equals userRole.UserId
                join role in db.Role on userRole.RoleId equals role.Id
                where userRole.RoleId == matchApprovalConfig.RoleId
                select user
            ).ToListAsync();
            if (listUser.HasElement())
            {
                var tranRequest = new TransportationRequest();
                tranRequest.Id = 0;
                tranRequest.IsRequestUnLockAll = true;
                tranRequest.ReasonUnLockAll = transportationRequestDetails.Reason;
                tranRequest.TransportationId = transportationRequestDetails.TransportationId;
                tranRequest.StatusId = (int)ApprovalStatusEnum.Approving;
                SetAuditInfo(tranRequest);
                db.Add(tranRequest);
                await db.SaveChangesAsync();
                var transportationRequestDetailsDB = await db.TransportationRequestDetails.Where(x => x.Id == transportationRequestDetails.Id).FirstOrDefaultAsync();
                transportationRequestDetailsDB.TransportationRequestId = tranRequest.Id;
                transportationRequestDetailsDB.StatusId = (int)ApprovalStatusEnum.Approving;
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = new List<TaskNotification>();
                foreach (var user in listUser)
                {
                    var task = new TaskNotification()
                    {
                        Title = $"{currentUser.FullName}",
                        Description = $"Đã gửi yêu cầu thay đổi (Hệ thống)",
                        EntityId = _entitySvc.GetEntity(typeof(TransportationRequest).Name).Id,
                        RecordId = tranRequest.Id,
                        Attachment = "fal fa-paper-plane",
                        AssignedId = user.Id,
                        StatusId = (int)TaskStateEnum.UnreadStatus,
                        RemindBefore = 540,
                        Deadline = DateTime.Now,
                    };
                    SetAuditInfo(task);
                    db.AddRange(task);
                    tasks.Add(task);
                }
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(tasks);
            }
        }

        [HttpPost("api/Transportation/RequestUnLockShip")]
        public async Task RequestUnLockShip([FromBody] TransportationRequestDetails transportationRequestDetails)
        {
            var check = await db.TransportationRequest.Where(x => x.TransportationId == transportationRequestDetails.TransportationId && x.Active).ToListAsync();
            if (check.Count > 0)
            {
                throw new ApiException("Đã có yêu cầu mở khóa");
            }
            var entityType = _entitySvc.GetEntity(typeof(Transportation).Name);
            var approvalConfig = await db.ApprovalConfig.AsNoTracking().OrderBy(x => x.Level)
                .Where(x => x.Active && x.EntityId == entityType.Id).ToListAsync();
            if (approvalConfig.Nothing())
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
            if (matchApprovalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            if (approvalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var listUser = await (
                from user in db.User
                join userRole in db.UserRole on user.Id equals userRole.UserId
                join role in db.Role on userRole.RoleId equals role.Id
                where userRole.RoleId == matchApprovalConfig.RoleId
                select user
            ).ToListAsync();
            if (listUser.HasElement())
            {
                var tranRequest = new TransportationRequest();
                tranRequest.Id = 0;
                tranRequest.IsRequestUnLockShip = true;
                tranRequest.ReasonUnLockShip = transportationRequestDetails.Reason;
                tranRequest.TransportationId = transportationRequestDetails.TransportationId;
                tranRequest.StatusId = (int)ApprovalStatusEnum.Approving;
                SetAuditInfo(tranRequest);
                db.Add(tranRequest);
                await db.SaveChangesAsync();
                var transportationRequestDetailsDB = await db.TransportationRequestDetails.Where(x => x.Id == transportationRequestDetails.Id).FirstOrDefaultAsync();
                transportationRequestDetailsDB.TransportationRequestId = tranRequest.Id;
                transportationRequestDetailsDB.StatusId = (int)ApprovalStatusEnum.Approving;
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = new List<TaskNotification>();
                foreach (var user in listUser)
                {
                    var task = new TaskNotification()
                    {
                        Title = $"{currentUser.FullName}",
                        Description = $"Đã gửi yêu cầu thay đổi (Cước tàu)",
                        EntityId = _entitySvc.GetEntity(typeof(TransportationRequest).Name).Id,
                        RecordId = tranRequest.Id,
                        Attachment = "fal fa-paper-plane",
                        AssignedId = user.Id,
                        StatusId = (int)TaskStateEnum.UnreadStatus,
                        RemindBefore = 540,
                        Deadline = DateTime.Now,
                    };
                    SetAuditInfo(task);
                    db.AddRange(task);
                    tasks.Add(task);
                }
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(tasks);
            }
        }

        [HttpPost("api/Transportation/ApproveUnLockAll")]
        public async Task<bool> ApproveUnLockAll([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var user = await db.User.Where(x => x.Active && x.Id == UserId).FirstOrDefaultAsync();
            foreach (var item in transportations)
            {
                var tranRequest = await db.TransportationRequest.Where(x => x.TransportationId == item.Id && x.Active).FirstOrDefaultAsync();
                var taskNotification = new TaskNotification
                {
                    Title = $"{user.FullName}",
                    Description = $"Đã duyệt yêu cầu thay đổi",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = item.Id,
                    Attachment = "fal fa-check",
                    AssignedId = tranRequest.InsertedBy,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                };
                SetAuditInfo(taskNotification);
                db.AddRange(taskNotification);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId) && x.Active).Select(x => x.Id).ToListAsync();
            var tranRequestDetails = await db.TransportationRequestDetails.Where(x => ids.Contains((int)x.TransportationId) && x.Active).ToListAsync();
            var tranRequestDetailsIds = tranRequestDetails.Select(x => x.Id).ToList();
            var cmd = "";
            foreach (var item in transportations)
            {
                var getTranRequestDetails = tranRequestDetails.Where(x => x.TransportationId == item.Id && x.StatusId == (int)ApprovalStatusEnum.Approving).FirstOrDefault();
                var patchUpdate = CompareChanges(getTranRequestDetails, item);
                await AddTriggerTransportation(patchUpdate, item);
            }
            cmd += $" Update [{nameof(TransportationRequest)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Approved}" +
                $" where Id in ({tranRequestIds.Combine()}) and Active = 1";
            cmd += $" Update [{nameof(TransportationRequestDetails)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Approved}" +
                $" where Id in ({tranRequestDetailsIds.Combine()}) and Active = 1;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/ApproveUnLockTransportation")]
        public async Task<bool> ApproveUnLockTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var user = await db.User.Where(x => x.Active && x.Id == UserId).FirstOrDefaultAsync();
            foreach (var item in transportations)
            {
                var tranRequest = await db.TransportationRequest.Where(x => x.TransportationId == item.Id && x.Active).FirstOrDefaultAsync();
                var taskNotification = new TaskNotification
                {
                    Title = $"{user.FullName}",
                    Description = $"Đã duyệt yêu cầu thay đổi",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = item.Id,
                    Attachment = "fal fa-check",
                    AssignedId = tranRequest.InsertedBy,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                };
                SetAuditInfo(taskNotification);
                db.AddRange(taskNotification);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId) && x.Active).Select(x => x.Id).ToListAsync();
            var tranRequestDetails = await db.TransportationRequestDetails.Where(x => ids.Contains((int)x.TransportationId) && x.Active).ToListAsync();
            var tranRequestDetailsIds = tranRequestDetails.Select(x => x.Id).ToList();
            var cmd = "";
            foreach (var item in transportations)
            {
                var getTranRequestDetails = tranRequestDetails.Where(x => x.TransportationId == item.Id && x.StatusId == (int)ApprovalStatusEnum.Approving).FirstOrDefault();
                var patchUpdate = CompareChanges(getTranRequestDetails, item);
                await AddTriggerTransportation(patchUpdate, item);
            }
            cmd += $" Update [{nameof(TransportationRequest)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Approved}" +
                $" where Id in ({tranRequestIds.Combine()}) and Active = 1";
            cmd += $" Update [{nameof(TransportationRequestDetails)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Approved}" +
                $" where Id in ({tranRequestDetailsIds.Combine()}) and Active = 1;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/ApproveUnLockAccountantTransportation")]
        public async Task<bool> ApproveUnLockAccountantTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var user = await db.User.Where(x => x.Active && x.Id == UserId).FirstOrDefaultAsync();
            foreach (var item in transportations)
            {
                var tranRequest = await db.TransportationRequest.Where(x => x.TransportationId == item.Id && x.Active).FirstOrDefaultAsync();
                var taskNotification = new TaskNotification
                {
                    Title = $"{user.FullName}",
                    Description = $"Đã duyệt yêu cầu thay đổi",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = item.Id,
                    Attachment = "fal fa-check",
                    AssignedId = tranRequest.InsertedBy,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                };
                SetAuditInfo(taskNotification);
                db.AddRange(taskNotification);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId) && x.Active).Select(x => x.Id).ToListAsync();
            var tranRequestDetails = await db.TransportationRequestDetails.Where(x => ids.Contains((int)x.TransportationId) && x.Active).ToListAsync();
            var tranRequestDetailsIds = tranRequestDetails.Select(x => x.Id).ToList();
            var cmd = "";
            foreach (var item in transportations)
            {
                var getTranRequestDetails = tranRequestDetails.Where(x => x.TransportationId == item.Id && x.StatusId == (int)ApprovalStatusEnum.Approving).FirstOrDefault();
                var patchUpdate = CompareChanges(getTranRequestDetails, item);
                await AddTriggerTransportation(patchUpdate, item);
            }
            cmd += $"Update [{nameof(Transportation)}] set IsSubmit = 0" +
                $" where Id in ({ids.Combine()})";
            cmd += $" Update [{nameof(TransportationRequest)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Approved}" +
                $" where Id in ({tranRequestIds.Combine()}) and Active = 1";
            cmd += $" Update [{nameof(TransportationRequestDetails)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Approved}" +
                $" where Id in ({tranRequestDetailsIds.Combine()}) and Active = 1;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/ApproveUnLockShip")]
        public async Task<bool> ApproveUnLockShip([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var user = await db.User.Where(x => x.Active && x.Id == UserId).FirstOrDefaultAsync();
            foreach (var item in transportations)
            {
                var tranRequest = await db.TransportationRequest.Where(x => x.TransportationId == item.Id && x.Active).FirstOrDefaultAsync();
                var taskNotification = new TaskNotification
                {
                    Title = $"{user.FullName}",
                    Description = $"Đã duyệt yêu cầu thay đổi",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = item.Id,
                    Attachment = "fal fa-check",
                    AssignedId = tranRequest.InsertedBy,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                };
                SetAuditInfo(taskNotification);
                db.AddRange(taskNotification);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId) && x.Active).Select(x => x.Id).ToListAsync();
            var tranRequestDetails = await db.TransportationRequestDetails.Where(x => ids.Contains((int)x.TransportationId) && x.Active).ToListAsync();
            var tranRequestDetailsIds = tranRequestDetails.Select(x => x.Id).ToList();
            var cmd = "";
            foreach (var item in transportations)
            {
                var getTranRequestDetails = tranRequestDetails.Where(x => x.TransportationId == item.Id && x.StatusId == (int)ApprovalStatusEnum.Approving).FirstOrDefault();
                var patchUpdate = CompareChanges(getTranRequestDetails, item);
                await AddTriggerTransportation(patchUpdate, item);
            }
            cmd += $" Update [{nameof(TransportationRequest)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Approved}" +
                $" where Id in ({tranRequestIds.Combine()}) and Active = 1";
            cmd += $" Update [{nameof(TransportationRequestDetails)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Approved}" +
                $" where Id in ({tranRequestDetailsIds.Combine()}) and Active = 1;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/RejectUnLockAll")]
        public async Task<bool> RejectUnLockAll([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var user = await db.User.Where(x => x.Active && x.Id == UserId).FirstOrDefaultAsync();
            foreach (var item in transportations)
            {
                var tranRequest = await db.TransportationRequest.Where(x => x.TransportationId == item.Id && x.Active).FirstOrDefaultAsync();
                var taskNotification = new TaskNotification
                {
                    Title = $"{user.FullName}",
                    Description = $"Đã hủy yêu cầu thay đổi. Lý do: {tranRequest.ReasonReject}",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = item.Id,
                    Attachment = "fal fa-check",
                    AssignedId = tranRequest.InsertedBy,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                };
                SetAuditInfo(taskNotification);
                db.AddRange(taskNotification);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var tranRequestDetailsIds = await db.TransportationRequestDetails.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var cmd = $"Update [{nameof(TransportationRequest)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Rejected}" +
                $" where Id in ({tranRequestIds.Combine()}) and Active = 1";
            cmd += $" Update [{nameof(TransportationRequestDetails)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Rejected}" +
                $" where Id in ({tranRequestDetailsIds.Combine()}) and Active = 1;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/RejectUnLockTransportation")]
        public async Task<bool> RejectUnLockTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var user = await db.User.Where(x => x.Active && x.Id == UserId).FirstOrDefaultAsync();
            foreach (var item in transportations)
            {
                var tranRequest = await db.TransportationRequest.Where(x => x.TransportationId == item.Id && x.Active).FirstOrDefaultAsync();
                var taskNotification = new TaskNotification
                {
                    Title = $"{user.FullName}",
                    Description = $"Đã hủy yêu cầu thay đổi. Lý do: {tranRequest.ReasonReject}",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = item.Id,
                    Attachment = "fal fa-check",
                    AssignedId = tranRequest.InsertedBy,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                };
                SetAuditInfo(taskNotification);
                db.AddRange(taskNotification);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var tranRequestDetailsIds = await db.TransportationRequestDetails.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var cmd = $"Update [{nameof(TransportationRequest)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Rejected}" +
                $" where Id in ({tranRequestIds.Combine()}) and Active = 1";
            cmd += $" Update [{nameof(TransportationRequestDetails)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Rejected}" +
                $" where Id in ({tranRequestDetailsIds.Combine()}) and Active = 1;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/RejectUnLockAccountantTransportation")]
        public async Task<bool> RejectUnLockAccountantTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var user = await db.User.Where(x => x.Active && x.Id == UserId).FirstOrDefaultAsync();
            foreach (var item in transportations)
            {
                var tranRequest = await db.TransportationRequest.Where(x => x.TransportationId == item.Id && x.Active).FirstOrDefaultAsync();
                var taskNotification = new TaskNotification
                {
                    Title = $"{user.FullName}",
                    Description = $"Đã hủy yêu cầu thay đổi. Lý do: {tranRequest.ReasonReject}",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = item.Id,
                    Attachment = "fal fa-check",
                    AssignedId = tranRequest.InsertedBy,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                };
                SetAuditInfo(taskNotification);
                db.AddRange(taskNotification);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var tranRequestDetailsIds = await db.TransportationRequestDetails.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var cmd = $"Update [{nameof(TransportationRequest)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Rejected}" +
                $" where Id in ({tranRequestIds.Combine()}) and Active = 1";
            cmd += $" Update [{nameof(TransportationRequestDetails)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Rejected}" +
                $" where Id in ({tranRequestDetailsIds.Combine()}) and Active = 1;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/RejectUnLockShip")]
        public async Task<bool> RejectUnLockShip([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var user = await db.User.Where(x => x.Active && x.Id == UserId).FirstOrDefaultAsync();
            foreach (var item in transportations)
            {
                var tranRequest = await db.TransportationRequest.Where(x => x.TransportationId == item.Id && x.Active).FirstOrDefaultAsync();
                var taskNotification = new TaskNotification
                {
                    Title = $"{user.FullName}",
                    Description = $"Đã hủy yêu cầu thay đổi. Lý do: {tranRequest.ReasonReject}",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = item.Id,
                    Attachment = "fal fa-check",
                    AssignedId = tranRequest.InsertedBy,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                };
                SetAuditInfo(taskNotification);
                db.AddRange(taskNotification);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var tranRequestDetailsIds = await db.TransportationRequestDetails.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var cmd = $"Update [{nameof(TransportationRequest)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Rejected}" +
                $" where Id in ({tranRequestIds.Combine()}) and Active = 1";
            cmd += $" Update [{nameof(TransportationRequestDetails)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Rejected}" +
                $" where Id in ({tranRequestDetailsIds.Combine()}) and Active = 1;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/CancelRequestChaneTransportation")]
        public async Task<bool> CancelRequestChaneTransportation([FromBody] List<TransportationRequest> transportationRequests)
        {
            if (transportationRequests == null)
            {
                return false;
            }
            var user = await db.User.Where(x => x.Active && x.Id == UserId).FirstOrDefaultAsync();
            var entityType = _entitySvc.GetEntity(typeof(Transportation).Name);
            var approvalConfig = await db.ApprovalConfig.AsNoTracking().OrderBy(x => x.Level)
                .Where(x => x.Active && x.EntityId == entityType.Id).ToListAsync();
            if (approvalConfig.Nothing())
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
            if (matchApprovalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            if (approvalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var listUser = await (
                    from u in db.User
                    join userRole in db.UserRole on u.Id equals userRole.UserId
                    join role in db.Role on userRole.RoleId equals role.Id
                    where userRole.RoleId == matchApprovalConfig.RoleId
                    select u).ToListAsync();
            foreach (var item in transportationRequests)
            {
                if (listUser.HasElement())
                {
                    var tasks = new List<TaskNotification>();
                    foreach (var u in listUser)
                    {
                        var task = new TaskNotification()
                        {
                            Title = $"{user.FullName}",
                            Description = $"Đã hủy yêu cầu thay đổi",
                            EntityId = _entitySvc.GetEntity(typeof(TransportationRequest).Name).Id,
                            RecordId = item.Id,
                            Attachment = "fal fa-paper-plane",
                            AssignedId = u.Id,
                            StatusId = (int)TaskStateEnum.UnreadStatus,
                            RemindBefore = 540,
                            Deadline = DateTime.Now,
                        };
                        SetAuditInfo(task);
                        db.AddRange(task);
                        tasks.Add(task);
                    }
                    await db.SaveChangesAsync();
                    await _taskService.NotifyAsync(tasks);
                }
            }
            var ids = transportationRequests.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.Id)).Select(x => x.Id).ToListAsync();
            var tranRequestDetailsIds = await db.TransportationRequestDetails.Where(x => ids.Contains((int)x.TransportationRequestId)).Select(x => x.Id).ToListAsync();
            var cmd = $"Update [{nameof(TransportationRequest)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Rejected}" +
                $" where Id in ({tranRequestIds.Combine()}) and Active = 1";
            cmd += $" Update [{nameof(TransportationRequestDetails)}] set Active = 0, StatusId = {(int)ApprovalStatusEnum.Rejected}" +
                $" where Id in ({tranRequestDetailsIds.Combine()}) and Active = 1;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        private PatchUpdate CompareChanges(object change, object cutting)
        {
            var patch = new PatchUpdate();
            patch.Changes = new List<PatchUpdateDetail>();
            if (change != null)
            {
                var propsChange = change.GetType().GetProperties().ToList();
                var propsCutting = cutting.GetType().GetProperties().ToList();
                foreach (var item in propsChange)
                {
                    var a1 = item.GetValue(change);
                    var a2 = propsCutting.Where(x => x.Name == item.Name).FirstOrDefault()?.GetValue(cutting);
                    if (a1 == null && a2 == null)
                    {
                        continue;
                    }
                    if (((a1 != null && a2 == null) || (a1 == null && a2 != null) || (a1 != null && a2 != null) && (a1.ToString() != a2.ToString()))
                        && item.Name != "Id"
                        && item.Name != "InsertedDate"
                        && item.Name != "InsertedBy"
                        && item.Name != "UpdatedDate"
                        && item.Name != "UpdatedBy"
                        && item.Name != "TransportationId"
                        && item.Name != "TransportationRequestId"
                        && item.Name != "StatusId"
                        && item.Name != "Reason"
                        && item.Name != "ReasonReject"
                        && item.Name != "TransportationRequest"
                        && item.Name != "TotalBet"
                        && item.Name != "TotalBetReport")
                    {
                        var propType = item.PropertyType.FullName;
                        if (propType.Contains("System.DateTime"))
                        {
                            patch.Changes.Add(new PatchUpdateDetail()
                            {
                                Field = item.Name,
                                Value = a1 != null ? $"{DateTime.Parse(a1.ToString()).ToString("yyyy/MM/dd")}" : "NULL",
                            });
                        }
                        else
                        {
                            patch.Changes.Add(new PatchUpdateDetail()
                            {
                                Field = item.Name,
                                Value = a1 != null ? $"{a1.ToString()}" : "NULL",
                            });
                        }
                    }
                }
            }
            return patch;
        }

        public async Task AddTriggerTransportation(PatchUpdate patch, Transportation item)
        {
            var idInt = item.Id;
            patch.Changes.Add(new PatchUpdateDetail
            {
                Field = nameof(Transportation.Id),
                Value = item.Id.ToString()
            });
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
                        command.CommandText += $" UPDATE [{nameof(Transportation)}] SET {update.Combine()} WHERE Id = {idInt};";
                        command.CommandText += " " + _transportationService.Transportation_ClosingUnitPrice(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_Note4(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_EmptyCombinationId(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_BetAmount(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_CombinationFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_Cont20_40(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_Dem(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_DemDate(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ExportListId(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_IsSplitBill(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_LandingFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_LiftFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_MonthText(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnClosingFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnDate(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnLiftFee(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnNotes(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ReturnVs(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ShellDate(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_ShipUnitPriceQuotation(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_VendorLocation(patch, idInt);
                        command.CommandText += " " + _transportationService.Transportation_BetFee(patch, idInt);
                        command.CommandText += " " + @"update t set ClosingNotes = isnull(tr.Notes,'') + case when ven1.ContactPhoneNumber is null and ven1.ContactName is null and ven1.ContactUser is null then '' else (' TTLH: '+isnull(ven1.ContactName,'') + '/'+ isnull(ven1.ContactUser,'') + '/' + isnull(ven1.ContactPhoneNumber,'') + '/' + isnull(ven1.Note,'')) end
	                        from Transportation t
	                        left join TransportationPlan tr on tr.Id = t.TransportationPlanId
	                        left join VendorContact ven1 on ven1.Id = tr.Contact2Id
                            where t.Id = " + idInt + ";";
                        foreach (var itemDetail in updates)
                        {
                            command.Parameters.AddWithValue($"@{itemDetail.Field.ToLower()}", itemDetail.Value is null ? DBNull.Value : itemDetail.Value);
                        }
                        await command.ExecuteNonQueryAsync();
                        transaction.Commit();
                        await db.Entry(item).ReloadAsync();
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }
        }
        #endregion
        #region Lock

        [HttpPost("api/Transportation/LockAllTransportation")]
        public async Task<bool> LockAllTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set IsLocked = 1" +
                $" where Id in ({ids.Combine()});";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/LockTransportation")]
        public async Task<bool> LockTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set IsKt = 1" +
                $" where Id in ({ids.Combine()});";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/LockAccountantTransportation")]
        public async Task<bool> LockAccountantTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set IsSubmit = 1" +
                $" where Id in ({ids.Combine()});";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/LockRevenueTransportation")]
        public async Task<bool> LockRevenueTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set IsLockedRevenue = 1" +
                $" where Id in ({ids.Combine()});";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/LockShipTransportation")]
        public async Task<bool> LockShipTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set LockShip = 1" +
                $" where Id in ({ids.Combine()});";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/UnLockAllTransportation")]
        public async Task<bool> UnLockAllTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set IsLocked = 0" +
                $" where Id in ({ids.Combine()});";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/UnLockTransportation")]
        public async Task<bool> UnLockTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set IsKt = 0" +
                $" where Id in ({ids.Combine()});";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/UnLockAccountantTransportation")]
        public async Task<bool> UnLockAccountantTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set IsSubmit = 0" +
                $" where Id in ({ids.Combine()});";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/UnLockRevenueTransportation")]
        public async Task<bool> UnLockRevenueTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set IsLockedRevenue = 0" +
                $" where Id in ({ids.Combine()});";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }

        [HttpPost("api/Transportation/UnLockShipTransportation")]
        public async Task<bool> UnLockShipTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set LockShip = 0" +
                $" where Id in ({ids.Combine()});";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;");
            return true;
        }
        #endregion
        #region Export
        [HttpPost("api/Transportation/ExportTransportationAndRevenue")]
        public async Task<string> ExportTransportationAndRevenue([FromBody] List<int> tranIds)
        {
            var trans = await db.Transportation.Where(x => tranIds.Contains(x.Id)).ToListAsync();
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(nameof(Transportation));
            worksheet.Style.Font.SetFontName("Times New Roman");
            worksheet.Row(1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Row(1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Row(1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Row(1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Row(2).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Row(2).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Row(2).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Row(2).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Cell("A1").Value = $"STT";
            worksheet.Cell("A1").Style.Alignment.WrapText = true;
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("A1:B2").Column(1).Merge();
            worksheet.Cell("B1").Value = $"Khóa hệ thống";
            worksheet.Cell("B1").Style.Alignment.WrapText = true;
            worksheet.Cell("B1").Style.Font.Bold = true;
            worksheet.Cell("B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("B1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("B1:C2").Column(1).Merge();
            worksheet.Cell("C1").Value = $"Tháng";
            worksheet.Cell("C1").Style.Alignment.WrapText = true;
            worksheet.Cell("C1").Style.Font.Bold = true;
            worksheet.Cell("C1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("C1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("C1:D2").Column(1).Merge();
            worksheet.Cell("D1").Value = $"Năm";
            worksheet.Cell("D1").Style.Alignment.WrapText = true;
            worksheet.Cell("D1").Style.Font.Bold = true;
            worksheet.Cell("D1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("D1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("D1:E2").Column(1).Merge();
            worksheet.Cell("E1").Value = $"List xuất";
            worksheet.Cell("E1").Style.Alignment.WrapText = true;
            worksheet.Cell("E1").Style.Font.Bold = true;
            worksheet.Cell("E1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("E1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("E1:F2").Column(1).Merge();
            worksheet.Cell("F1").Value = $"Tuyến vận chuyển";
            worksheet.Cell("F1").Style.Alignment.WrapText = true;
            worksheet.Cell("F1").Style.Font.Bold = true;
            worksheet.Cell("F1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("F1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("F1:G2").Column(1).Merge();
            worksheet.Cell("G1").Value = $"SOC";
            worksheet.Cell("G1").Style.Alignment.WrapText = true;
            worksheet.Cell("G1").Style.Font.Bold = true;
            worksheet.Cell("G1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("G1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("G1:H2").Column(1).Merge();
            worksheet.Cell("H1").Value = $"Tên tàu";
            worksheet.Cell("H1").Style.Alignment.WrapText = true;
            worksheet.Cell("H1").Style.Font.Bold = true;
            worksheet.Cell("H1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("H1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("H1:I2").Column(1).Merge();
            worksheet.Cell("I1").Value = $"Số chuyến";
            worksheet.Cell("I1").Style.Alignment.WrapText = true;
            worksheet.Cell("I1").Style.Font.Bold = true;
            worksheet.Cell("I1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("I1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("I1:J2").Column(1).Merge();
            worksheet.Cell("J1").Value = $"Ngày đóng hàng";
            worksheet.Cell("J1").Style.Alignment.WrapText = true;
            worksheet.Cell("J1").Style.Font.Bold = true;
            worksheet.Cell("J1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("J1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("J1:K2").Column(1).Merge();
            worksheet.Cell("K1").Value = $"Ngày tàu chạy";
            worksheet.Cell("K1").Style.Alignment.WrapText = true;
            worksheet.Cell("K1").Style.Font.Bold = true;
            worksheet.Cell("K1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("K1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("K1:L2").Column(1).Merge();
            worksheet.Cell("L1").Value = $"Loại container";
            worksheet.Cell("L1").Style.Alignment.WrapText = true;
            worksheet.Cell("L1").Style.Font.Bold = true;
            worksheet.Cell("L1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("L1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("L1:M2").Column(1).Merge();
            worksheet.Cell("M1").Value = $"Số cont";
            worksheet.Cell("M1").Style.Alignment.WrapText = true;
            worksheet.Cell("M1").Style.Font.Bold = true;
            worksheet.Cell("M1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("M1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("M1:N2").Column(1).Merge();
            worksheet.Cell("N1").Value = $"Số seal";
            worksheet.Cell("N1").Style.Alignment.WrapText = true;
            worksheet.Cell("N1").Style.Font.Bold = true;
            worksheet.Cell("N1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("N1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("N1:O2").Column(1).Merge();
            worksheet.Cell("O1").Value = $"Chủ hàng";
            worksheet.Cell("O1").Style.Alignment.WrapText = true;
            worksheet.Cell("O1").Style.Font.Bold = true;
            worksheet.Cell("O1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("O1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("O1:P2").Column(1).Merge();
            worksheet.Cell("P1").Value = $"Nhân viên bán hàng";
            worksheet.Cell("P1").Style.Alignment.WrapText = true;
            worksheet.Cell("P1").Style.Font.Bold = true;
            worksheet.Cell("P1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("P1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("P1:Q2").Column(1).Merge();
            worksheet.Cell("Q1").Value = $"Vật tư hàng hóa";
            worksheet.Cell("Q1").Style.Alignment.WrapText = true;
            worksheet.Cell("Q1").Style.Font.Bold = true;
            worksheet.Cell("Q1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("Q1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("Q1:R2").Column(1).Merge();
            worksheet.Cell("R1").Value = $"Cont 20";
            worksheet.Cell("R1").Style.Alignment.WrapText = true;
            worksheet.Cell("R1").Style.Font.Bold = true;
            worksheet.Cell("R1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("R1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("R1:S2").Column(1).Merge();
            worksheet.Cell("S1").Value = $"Cont 40";
            worksheet.Cell("S1").Style.Alignment.WrapText = true;
            worksheet.Cell("S1").Style.Font.Bold = true;
            worksheet.Cell("S1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("S1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("S1:T2").Column(1).Merge();
            worksheet.Cell("T1").Value = $"Trọng lượng";
            worksheet.Cell("T1").Style.Alignment.WrapText = true;
            worksheet.Cell("T1").Style.Font.Bold = true;
            worksheet.Cell("T1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("T1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("T1:U2").Column(1).Merge();
            worksheet.Cell("U1").Value = $"Địa điểm nhận hàng";
            worksheet.Cell("U1").Style.Alignment.WrapText = true;
            worksheet.Cell("U1").Style.Font.Bold = true;
            worksheet.Cell("U1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("U1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("U1:V2").Column(1).Merge();
            worksheet.Cell("V1").Value = $"Phát sinh đóng hàng";
            worksheet.Cell("V1").Style.Alignment.WrapText = true;
            worksheet.Cell("V1").Style.Font.Bold = true;
            worksheet.Cell("V1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("V1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("V1:W2").Column(1).Merge();
            worksheet.Cell("W1").Value = $"Phí bảo hiểm";
            worksheet.Cell("W1").Style.Alignment.WrapText = true;
            worksheet.Cell("W1").Style.Font.Bold = true;
            worksheet.Cell("W1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("W1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("W1:X2").Column(1).Merge();
            worksheet.Cell("X1").Value = $"Ngày tàu cập";
            worksheet.Cell("X1").Style.Alignment.WrapText = true;
            worksheet.Cell("X1").Style.Font.Bold = true;
            worksheet.Cell("X1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("X1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("X1:Y2").Column(1).Merge();
            worksheet.Cell("Y1").Value = $"Ngày trả hàng";
            worksheet.Cell("Y1").Style.Alignment.WrapText = true;
            worksheet.Cell("Y1").Style.Font.Bold = true;
            worksheet.Cell("Y1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("Y1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("Y1:Z2").Column(1).Merge();
            worksheet.Cell("Z1").Value = $"Địa điểm trả hàng";
            worksheet.Cell("Z1").Style.Alignment.WrapText = true;
            worksheet.Cell("Z1").Style.Font.Bold = true;
            worksheet.Cell("Z1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("Z1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("Z1:AA2").Column(1).Merge();
            worksheet.Cell("AA1").Value = $"Phát sinh trả hàng";
            worksheet.Cell("AA1").Style.Alignment.WrapText = true;
            worksheet.Cell("AA1").Style.Font.Bold = true;
            worksheet.Cell("AA1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AA1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AA1:AB2").Column(1).Merge();
            worksheet.Cell("AB1").Value = $"Khóa khai thác";
            worksheet.Cell("AB1").Style.Alignment.WrapText = true;
            worksheet.Cell("AB1").Style.Font.Bold = true;
            worksheet.Cell("AB1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AB1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AB1:AC2").Column(1).Merge();
            worksheet.Cell("AC1").Value = $"Khóa kế toán";
            worksheet.Cell("AC1").Style.Alignment.WrapText = true;
            worksheet.Cell("AC1").Style.Font.Bold = true;
            worksheet.Cell("AC1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AC1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AC1:AD2").Column(1).Merge();
            worksheet.Cell("AD1").Value = $"Tên doanh thu";
            worksheet.Cell("AD1").Style.Alignment.WrapText = true;
            worksheet.Cell("AD1").Style.Font.Bold = true;
            worksheet.Cell("AD1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AD1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AD1:AE2").Column(1).Merge();
            worksheet.Cell("AE1").Value = $"Số bảng kê";
            worksheet.Cell("AE1").Style.Alignment.WrapText = true;
            worksheet.Cell("AE1").Style.Font.Bold = true;
            worksheet.Cell("AE1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AE1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AE1:AF2").Column(1).Merge();
            worksheet.Cell("AF1").Value = $"Ngày bảng kê";
            worksheet.Cell("AF1").Style.Alignment.WrapText = true;
            worksheet.Cell("AF1").Style.Font.Bold = true;
            worksheet.Cell("AF1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AF1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AF1:AG2").Column(1).Merge();
            worksheet.Cell("AG1").Value = $"Số hóa đơn";
            worksheet.Cell("AG1").Style.Alignment.WrapText = true;
            worksheet.Cell("AG1").Style.Font.Bold = true;
            worksheet.Cell("AG1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AG1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AG1:AH2").Column(1).Merge();
            worksheet.Cell("AH1").Value = $"Ngày hóa đơn";
            worksheet.Cell("AH1").Style.Alignment.WrapText = true;
            worksheet.Cell("AH1").Style.Font.Bold = true;
            worksheet.Cell("AH1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AH1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AH1:AI2").Column(1).Merge();
            worksheet.Cell("AI1").Value = $"% GTGT";
            worksheet.Cell("AI1").Style.Alignment.WrapText = true;
            worksheet.Cell("AI1").Style.Font.Bold = true;
            worksheet.Cell("AI1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AI1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AI1:AJ2").Column(1).Merge();
            worksheet.Cell("AJ1").Value = $"Đơn giá (chưa VAT)";
            worksheet.Cell("AJ1").Style.Alignment.WrapText = true;
            worksheet.Cell("AJ1").Style.Font.Bold = true;
            worksheet.Cell("AJ1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AJ1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AJ1:AK2").Column(1).Merge();
            worksheet.Cell("AK1").Value = $"Đơn giá (có VAT)";
            worksheet.Cell("AK1").Style.Alignment.WrapText = true;
            worksheet.Cell("AK1").Style.Font.Bold = true;
            worksheet.Cell("AK1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AK1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AK1:AL2").Column(1).Merge();
            worksheet.Cell("AL1").Value = $"Thu khác";
            worksheet.Cell("AL1").Style.Alignment.WrapText = true;
            worksheet.Cell("AL1").Style.Font.Bold = true;
            worksheet.Cell("AL1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AL1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AL1:AM2").Column(1).Merge();
            worksheet.Cell("AM1").Value = $"Thu chi hộ";
            worksheet.Cell("AM1").Style.Alignment.WrapText = true;
            worksheet.Cell("AM1").Style.Font.Bold = true;
            worksheet.Cell("AM1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AM1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AM1:AN2").Column(1).Merge();
            worksheet.Cell("AN1").Value = $"Ghi chú doanh thu";
            worksheet.Cell("AN1").Style.Alignment.WrapText = true;
            worksheet.Cell("AN1").Style.Font.Bold = true;
            worksheet.Cell("AN1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AN1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AN1:AO2").Column(1).Merge();
            worksheet.Cell("AO1").Value = $"Giá trị (chưa VAT)";
            worksheet.Cell("AO1").Style.Alignment.WrapText = true;
            worksheet.Cell("AO1").Style.Font.Bold = true;
            worksheet.Cell("AO1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AO1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AO1:AP2").Column(1).Merge();
            worksheet.Cell("AP1").Value = $"Thuế GTGT";
            worksheet.Cell("AP1").Style.Alignment.WrapText = true;
            worksheet.Cell("AP1").Style.Font.Bold = true;
            worksheet.Cell("AP1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AP1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AP1:AQ2").Column(1).Merge();
            worksheet.Cell("AQ1").Value = $"Tổng giá trị";
            worksheet.Cell("AQ1").Style.Alignment.WrapText = true;
            worksheet.Cell("AQ1").Style.Font.Bold = true;
            worksheet.Cell("AQ1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AQ1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AQ1:AR2").Column(1).Merge();
            worksheet.Cell("AR1").Value = $"Đơn vị xuất hóa đơn";
            worksheet.Cell("AR1").Style.Alignment.WrapText = true;
            worksheet.Cell("AR1").Style.Font.Bold = true;
            worksheet.Cell("AR1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AR1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AR1:AS2").Column(1).Merge();
            worksheet.Cell("AS1").Value = $"Ghi chú";
            worksheet.Cell("AS1").Style.Alignment.WrapText = true;
            worksheet.Cell("AS1").Style.Font.Bold = true;
            worksheet.Cell("AS1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AS1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AS1:AT2").Column(1).Merge();
            worksheet.Cell("AT1").Value = $"Thanh toán";
            worksheet.Cell("AT1").Style.Alignment.WrapText = true;
            worksheet.Cell("AT1").Style.Font.Bold = true;
            worksheet.Cell("AT1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AT1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AT1:AU2").Column(1).Merge();
            var ids = trans.Select(x => x.Id).ToList();
            var sql = @$"select t.Id,
            t.IsLocked,
            replace(t.MonthText, '%2F', '/') as MonthText,
            t.YearText,
            v.Name as ExportList,
            r.Name as Route,
            v2.Name as Soc,
            s.Name as Ship,
            t.Trip,
            t.ClosingDate,
            t.StartShip,
            m.Description as ContainerType,
            t.ContainerNo,
            t.SealNo,
            v3.Name as Boss,
            u.FullName as [User],
            m2.Description as Commodity,
            t.Cont20,
            t.Cont40,
            t.Weight,
            l.Description as Received,
            t.FreeText2,
            t.InsuranceFee,
            t.ShipDate,
            t.ReturnDate,
            l2.Description as [Return],
            t.FreeText3,
            t.IsKt,
            t.IsSubmit,
            t.Name,
            t.LotNo,
            t.NoteLotDate,
            t.InvoinceNo,
            t.NoteInvoinceDate,
            t.Vat,
            t.UnitPriceBeforeTax,
            t.UnitPriceAfterTax,
            t.ReceivedPrice,
            t.CollectOnBehaftPrice,
            t.NotePayment,
            t.TotalPriceBeforTax,
            t.VatPrice,
            t.TotalPrice,
            t.NoteVendorVatId,
            t.Note,
            t.IsPayment
            from Transportation t 
            left join Vendor v on t.ExportListId = v.Id
            left join Route r on t.RouteId = r.Id
            left join Vendor v2 on t.SocId = v2.Id
            left join Ship s on t.ShipId = s.Id
            left join MasterData m on t.ContainerTypeId = m.Id
            left join Vendor v3 on t.BossId = v3.Id
            left join [User] u on t.UserId = u.Id
            left join MasterData m2 on t.CommodityId = m2.Id
            left join Location l on t.ReceivedId = l.Id
            left join Location l2 on t.ReturnId = l2.Id
            where t.Id in ({ids.Combine()})";
            var data = await ConverSqlToDataSet(sql);
            var start = 3;
            foreach (var item in data[0])
            {
                worksheet.Cell("A" + start).SetValue(start - 2);
                worksheet.Cell("B" + start).SetValue(item["IsLocked"].ToString().Contains("False") ? 0 : 1);
                worksheet.Cell("C" + start).SetValue(item["MonthText"] is null ? "" : item["MonthText"].ToString());
                worksheet.Cell("D" + start).SetValue(item["YearText"] is null ? "" : item["YearText"].ToString());
                worksheet.Cell("E" + start).SetValue(item["ExportList"] is null ? "" : item["ExportList"].ToString());
                worksheet.Cell("F" + start).SetValue(item["Route"] is null ? "" : item["Route"].ToString());
                worksheet.Cell("G" + start).SetValue(item["Soc"] is null ? "" : item["Soc"].ToString());
                worksheet.Cell("H" + start).SetValue(item["Ship"] is null ? "" : item["Ship"].ToString());
                worksheet.Cell("I" + start).SetValue(item["Trip"] is null ? "" : item["Trip"].ToString());
                worksheet.Cell("J" + start).SetValue(item["ClosingDate"] is null ? "" : DateTime.Parse(item["ClosingDate"].ToString()));
                worksheet.Cell("K" + start).SetValue(item["StartShip"] is null ? "" : DateTime.Parse(item["StartShip"].ToString()));
                worksheet.Cell("L" + start).SetValue(item["ContainerType"] is null ? "" : item["ContainerType"].ToString());
                worksheet.Cell("M" + start).SetValue(item["ContainerNo"] is null ? "" : item["ContainerNo"].ToString());
                worksheet.Cell("N" + start).SetValue(item["SealNo"] is null ? "" : item["SealNo"].ToString());
                worksheet.Cell("O" + start).SetValue(item["Boss"] is null ? "" : item["Boss"].ToString());
                worksheet.Cell("P" + start).SetValue(item["User"] is null ? "" : item["User"].ToString());
                worksheet.Cell("Q" + start).SetValue(item["Commodity"] is null ? "" : item["Commodity"].ToString());
                worksheet.Cell("R" + start).SetValue(item["Cont20"] is null ? 0 : decimal.Parse(item["Cont20"].ToString()));
                worksheet.Cell("S" + start).SetValue(item["Cont40"] is null ? 0 : decimal.Parse(item["Cont40"].ToString()));
                worksheet.Cell("T" + start).SetValue(item["Weight"] is null ? default(decimal) : decimal.Parse(item["Weight"].ToString()));
                worksheet.Cell("T" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("U" + start).SetValue(item["Received"] is null ? "" : item["Received"].ToString());
                worksheet.Cell("V" + start).SetValue(item["FreeText2"] is null ? "" : item["FreeText2"].ToString());
                worksheet.Cell("W" + start).SetValue(item["InsuranceFee"] is null ? default(decimal) : decimal.Parse(item["InsuranceFee"].ToString()));
                worksheet.Cell("W" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("X" + start).SetValue(item["ShipDate"] is null ? "" : DateTime.Parse(item["ShipDate"].ToString()));
                worksheet.Cell("Y" + start).SetValue(item["ReturnDate"] is null ? "" : DateTime.Parse(item["ReturnDate"].ToString()));
                worksheet.Cell("Z" + start).SetValue(item["Return"] is null ? "" : item["Return"].ToString());
                worksheet.Cell("AA" + start).SetValue(item["FreeText3"] is null ? "" : item["FreeText3"].ToString());
                worksheet.Cell("AB" + start).SetValue(item["IsKt"].ToString().Contains("False") ? 0 : 1);
                worksheet.Cell("AC" + start).SetValue(item["IsSubmit"].ToString().Contains("False") ? 0 : 1);
                worksheet.Cell("AD" + start).SetValue(item["Name"] is null ? "" : item["Name"].ToString());
                worksheet.Cell("AE" + start).SetValue(item["LotNo"] is null ? "" : item["LotNo"].ToString());
                worksheet.Cell("AF" + start).SetValue(item["NoteLotDate"] is null ? "" : item["NoteLotDate"].ToString());
                worksheet.Cell("AG" + start).SetValue(item["InvoinceNo"] is null ? "" : item["InvoinceNo"].ToString());
                worksheet.Cell("AH" + start).SetValue(item["NoteInvoinceDate"] is null ? "" : item["NoteInvoinceDate"].ToString());
                worksheet.Cell("AI" + start).SetValue(item["Vat"] is null ? default(decimal) : decimal.Parse(item["Vat"].ToString()));
                worksheet.Cell("AJ" + start).SetValue(item["UnitPriceBeforeTax"] is null ? default(decimal) : decimal.Parse(item["UnitPriceBeforeTax"].ToString()));
                worksheet.Cell("AJ" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("AK" + start).SetValue(item["UnitPriceAfterTax"] is null ? default(decimal) : decimal.Parse(item["UnitPriceAfterTax"].ToString()));
                worksheet.Cell("AK" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("AL" + start).SetValue(item["ReceivedPrice"] is null ? default(decimal) : decimal.Parse(item["ReceivedPrice"].ToString()));
                worksheet.Cell("AL" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("AM" + start).SetValue(item["CollectOnBehaftPrice"] is null ? default(decimal) : decimal.Parse(item["CollectOnBehaftPrice"].ToString()));
                worksheet.Cell("AM" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("AN" + start).SetValue(item["NotePayment"] is null ? "" : item["NotePayment"].ToString());
                worksheet.Cell("AO" + start).SetValue(item["TotalPriceBeforTax"] is null ? default(decimal) : decimal.Parse(item["TotalPriceBeforTax"].ToString()));
                worksheet.Cell("AO" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("AP" + start).SetValue(item["VatPrice"] is null ? default(decimal) : decimal.Parse(item["VatPrice"].ToString()));
                worksheet.Cell("AP" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("AQ" + start).SetValue(item["TotalPrice"] is null ? default(decimal) : decimal.Parse(item["TotalPrice"].ToString()));
                worksheet.Cell("AQ" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("AR" + start).SetValue(item["NoteVendorVatId"] is null ? "" : item["NoteVendorVatId"].ToString());
                worksheet.Cell("AS" + start).SetValue(item["Note"] is null ? "" : item["Note"].ToString());
                worksheet.Cell("AT" + start).SetValue(item["IsPayment"].ToString().Contains("False") ? 0 : 1);
                worksheet.Row(start).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Row(start).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Row(start).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Row(start).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                start++;
            }
            var url = $"Transportation.xlsx";
            workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
            return url;
        }
        #endregion
    }
}
