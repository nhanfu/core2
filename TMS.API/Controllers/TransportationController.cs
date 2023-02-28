using Aspose.Cells;
using ClosedXML.Excel;
using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class TransportationController : TMSController<Transportation>
    {
        public TransportationController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<Transportation>> PatchAsync([FromQuery] ODataQueryOptions<Transportation> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
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
                        var entity = await db.Transportation.FindAsync(idInt);
                        if (!disableTrigger)
                        {
                            await db.Entry(entity).ReloadAsync();
                        }
                        return entity;
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var entity = await db.Transportation.FindAsync(idInt);
                    return entity;
                }
            }
        }

        protected override IQueryable<Transportation> GetQuery()
        {
            var rs = base.GetQuery();
            //Sale
            if (AllRoleIds.Contains(10))
            {
                rs = rs.Where(x => x.UserId == UserId || x.InsertedBy == UserId);
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
                    await _taskService.SendMessageAllUser(new WebSocketResponse<Expense>
                    {
                        EntityId = _entitySvc.GetEntity(typeof(Expense).Name).Id,
                        Data = item
                    });
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
                var deleteExpense = $" delete from [{typeof(Expense).Name}] where TransportationId in ({string.Join(",", ids)}) ";
                deleteExpense += $" delete from [{typeof(Revenue).Name}] where TransportationId in ({string.Join(",", ids)}) delete from [{typeof(Transportation).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteExpense);
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

        public static DateTime TryParseDateTimeFile(string value)
        {
            DateTime dateTime = DateTime.Now;
            bool parsed = false;
            string format = null;
            for (long i = 0; i < formats.Length; i++)
            {
                parsed = DateTime.TryParseExact(value, formats[i], CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTime);
                if (parsed)
                {
                    format = formats[i];
                    break;
                }
            }
            Console.WriteLine(value);
            Console.WriteLine(dateTime);
            return dateTime;
        }

        public static string[] formats = { "dd/MM/yy", "dd/MM/yyyy" };

        [HttpPost("api/Transportation/ExportCheckFee")]
        public async Task<string> ExportCheckFee([FromBody] List<Transportation> transportations)
        {
            transportations = transportations.OrderBy(x => x.ClosingDate).ToList();
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(nameof(Transportation));
            var closingId = transportations.FirstOrDefault().ClosingId;
            var closing = await db.Vendor.FirstOrDefaultAsync(x => x.Id == closingId);
            worksheet.Style.Font.SetFontName("Times New Roman");
            worksheet.Cell("A1").Value = closing.Name;
            worksheet.Cell("A2").Value = "Địa chỉ";
            worksheet.Cell("A3").Value = "MST";
            worksheet.Cell("A4").Value = $"BẢNG KÊ ĐỐI CHIẾU CƯỚC VC XE TỪ NGÀY {transportations.FirstOrDefault().ClosingDate?.ToString("dd/MM/yyyy")} ĐẾN {transportations.LastOrDefault().ClosingDate?.ToString("dd/MM/yyyy")}";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("A4").Style.Font.FontSize = 20;
            worksheet.Cell("A4").Style.Font.FontColor = XLColor.Red;
            worksheet.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A4").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("A4:AA4").Row(1).Merge();
            worksheet.Cell("A5").Value = $"Kính gửi: Công Ty Cổ Phần Logistics Đông Á";
            worksheet.Cell("A5").Style.Font.Italic = true;
            worksheet.Cell("A5").Style.Font.Bold = true;

            worksheet.Row(6).Style.Fill.BackgroundColor = XLColor.LightGreen;
            worksheet.Row(7).Style.Fill.BackgroundColor = XLColor.LightGreen;
            worksheet.Row(6).Height = 30;
            worksheet.Row(6).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Row(6).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Row(6).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Row(6).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Row(7).Height = 70;
            worksheet.Row(7).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Row(7).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Row(7).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Row(7).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Cell("A6").Value = $"STT";
            worksheet.Cell("A6").Style.Alignment.WrapText = true;
            worksheet.Cell("A6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("A6:B7").Column(1).Merge();

            worksheet.Cell("B6").Value = $"Ngày đóng hàng(ĐỀ NGHỊ ĐỂ ĐÚNG ĐỊNH DẠNG DD/MM/YYYY)";
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

            worksheet.Cell("H6").Value = $"Địa điểm nhận hàng";
            worksheet.Cell("H6").Style.Alignment.WrapText = true;
            worksheet.Cell("H6").Style.Font.Bold = true;
            worksheet.Cell("H6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("H6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("H6:I7").Column(1).Merge();

            worksheet.Cell("I6").Value = $"Nơi lấy rỗng";
            worksheet.Cell("I6").Style.Font.Bold = true;
            worksheet.Cell("I6").Style.Alignment.WrapText = true;
            worksheet.Cell("I6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("I6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("I6:J7").Column(1).Merge();

            worksheet.Cell("J6").Value = $"Cảng hạ hàng";
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

            var sql = @$"select ClosingDate, ClosingDateCheck, ClosingDateUpload
                    ,ClosingDate, ClosingDateCheck, ClosingDateUpload
                    ,b.Name as Boss, BossCheck, BossCheckUpload
                    ,ContainerNo, ContainerNoCheck, ContainerNoUpload
                    ,SealNo, SealCheck, SealCheckUpload
                    ,Cont20 as Cont20, Cont20Check, Cont20CheckUpload
                    ,Cont40 as Cont40, Cont40Check, Cont40CheckUpload
                    ,r.Description as Received, ReceivedCheck, ReceivedCheckUpload
                    ,CollectOnBehaftInvoinceNoFee as CollectOnBehaftInvoinceNoFee, CollectOnBehaftInvoinceNoFeeCheck, CollectOnBehaftInvoinceNoFeeUpload
                    ,CollectOnBehaftFee as CollectOnBehaftFee, CollectOnBehaftFeeCheck, CollectOnBehaftFeeUpload,TotalPriceAfterTaxUpload
                    ,pi.Description as PickupEmpty, PickupEmptyCheck, PickupEmptyUpload
                    ,po.Description as PortLoading, PortLoadingCheck, PortLoadingUpload
                    ,LiftFee as LiftFee, LiftFeeCheck, LiftFeeCheckUpload
                    ,LandingFee as LandingFee, LandingFeeCheck, LandingFeeUpload
                    ,CollectOnSupPrice as CollectOnSupPrice, CollectOnSupPriceCheck, CollectOnSupPriceUpload
                    ,ClosingPercent as ClosingPercent, ClosingPercentCheck, ClosingPercentUpload
                    ,Fee1, Fee2, Fee3,Fee4, Fee5, Fee6
                    ,Fee1Upload, Fee2Upload, Fee3Upload,Fee4Upload, Fee5Upload, Fee6Upload
                    ,FeeVat1,FeeVat2,FeeVat3
                    ,FeeVat1Upload,FeeVat1Upload,FeeVat1Upload
                    ,ClosingCombinationUnitPrice as ClosingCombinationUnitPrice, ClosingCombinationUnitPriceCheck, ClosingCombinationUnitPriceUpload
                    ,IsEmptyLift, IsSeftPayment, IsLanding, IsSeftPaymentLand
                    from Transportation t
                    left join Vendor b on b.Id = t.BossId
                    left join Location r on r.Id = t.ReceivedId
                    left join Location pi on pi.Id = t.PickupEmptyId
                    left join Location po on po.Id = t.PortLoadingId";
            if (transportations.FirstOrDefault().CheckFeeHistoryId != null)
            {
                sql += $" where t.CheckFeeHistoryId = {transportations.FirstOrDefault().CheckFeeHistoryId}";
            }
            else
            {
                sql += $" where t.Id in ({string.Join(",", transportations.Select(x => x.Id).ToList())})";
            }
            var data = await ConverSqlToDataSet(sql);
            var start = 8;
            foreach (var item in data[0])
            {
                worksheet.Cell("A" + start).SetValue(start - 7);
                worksheet.Cell("B" + start).SetValue(DateTime.Parse(item[nameof(Transportation.ClosingDate)].ToString()));
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
                if (bool.Parse(item["IsEmptyLift"].ToString()) || bool.Parse(item["IsSeftPayment"].ToString()))
                {
                    item["LiftFee"] = null;
                }
                worksheet.Cell("K" + start).SetValue(item["LiftFee"] is null ? default(decimal) : decimal.Parse(item["LiftFee"].ToString()));
                worksheet.Cell("K" + start).Style.NumberFormat.Format = "#,##";
                if (bool.Parse(item["IsLanding"].ToString()) || bool.Parse(item["IsSeftPaymentLand"].ToString()))
                {
                    item["LandingFee"] = null;
                }
                worksheet.Cell("L" + start).SetValue(item["LandingFee"] is null ? default(decimal) : decimal.Parse(item["LandingFee"].ToString()));
                worksheet.Cell("L" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("M" + start).SetValue(item["FeeVat1"] is null ? default(decimal) : decimal.Parse(item["FeeVat1"].ToString()));
                worksheet.Cell("M" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("N" + start).SetValue(item["FeeVat2"] is null ? default(decimal) : decimal.Parse(item["FeeVat2"].ToString()));
                worksheet.Cell("N" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("O" + start).SetValue(item["FeeVat3"] is null ? default(decimal) : decimal.Parse(item["FeeVat3"].ToString()));
                worksheet.Cell("O" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("P" + start).SetValue(item["Fee1"] is null ? default(decimal) : decimal.Parse(item["Fee1"].ToString()));
                worksheet.Cell("P" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("Q" + start).SetValue(item["Fee2"] is null ? default(decimal) : decimal.Parse(item["Fee2"].ToString()));
                worksheet.Cell("Q" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("R" + start).SetValue(item["Fee3"] is null ? default(decimal) : decimal.Parse(item["Fee3"].ToString()));
                worksheet.Cell("R" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("S" + start).SetValue(item["Fee4"] is null ? default(decimal) : decimal.Parse(item["Fee4"].ToString()));
                worksheet.Cell("S" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("T" + start).SetValue(item["Fee5"] is null ? default(decimal) : decimal.Parse(item["Fee5"].ToString()));
                worksheet.Cell("T" + start).Style.NumberFormat.Format = "#,##";

                worksheet.Cell("U" + start).SetValue(item["Fee6"] is null ? default(decimal) : decimal.Parse(item["Fee6"].ToString()));
                worksheet.Cell("U" + start).Style.NumberFormat.Format = "#,##";

                var closingPercent = item["ClosingPercent"] is null ? "0" : item["ClosingPercent"].ToString();
                var closingPercentUpload = item["ClosingPercentUpload"] is null ? "0" : item["ClosingPercentUpload"].ToString();
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
                worksheet.Cell("Z" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Row(start).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Row(start).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Row(start).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Row(start).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
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
            worksheet.Cell("K" + tt).Value = data[0].Sum(item => item["LiftFee"] is null ? default(decimal) : decimal.Parse(item["LiftFee"].ToString()));
            worksheet.Cell("K" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("K" + tt).Style.Font.Bold = true;
            var tt1 = data[0].Sum(item => item["LandingFee"] is null ? default(decimal) : decimal.Parse(item["LandingFee"].ToString()));
            worksheet.Cell("L" + tt).Value = tt1;
            worksheet.Cell("L" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("L" + tt).Style.Font.Bold = true;
            var tt2 = data[0].Sum(item => item["CollectOnBehaftInvoinceNoFee"] is null ? default(decimal) : decimal.Parse(item["CollectOnBehaftInvoinceNoFee"].ToString()));
            worksheet.Cell("O" + tt).Value = tt2;
            worksheet.Cell("O" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("O" + tt).Style.Font.Bold = true;
            var tt3 = data[0].Sum(item => item["CollectOnBehaftFee"] is null ? default(decimal) : decimal.Parse(item["CollectOnBehaftFee"].ToString()));
            worksheet.Cell("P" + tt).Value = data[0].Sum(item => item["CollectOnBehaftFee"] is null ? default(decimal) : decimal.Parse(item["CollectOnBehaftFee"].ToString()));
            worksheet.Cell("P" + tt).Style.NumberFormat.Format = "#,##";
            worksheet.Cell("P" + tt).Style.Font.Bold = true;
            var tt4 = data[0].Sum(item => item["ClosingCombinationUnitPrice"] is null ? default(decimal) : decimal.Parse(item["ClosingCombinationUnitPrice"].ToString()));
            worksheet.Cell("X" + tt).Value = tt4;
            worksheet.Cell("Z" + tt).Value = tt1 + tt2 + tt3 + tt4;
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
            var url = $"BangKe{closing.Name}{transportations.FirstOrDefault().ClosingDate.Value.ToString("dd-MM-yyyy")}Den{transportations.LastOrDefault().ClosingDate.Value.ToString("dd-MM-yyyy")}.xlsx";
            workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
            return url;
        }

        [HttpPost("api/Transportation/ExportProductionReport")]
        public async Task<string> ExportProductionReport()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(nameof(Transportation));
            worksheet.Style.Font.SetFontName("Times New Roman");
            worksheet.Cell("A1").Value = "BÁO CÁO SẢN LƯỢNG";
            worksheet.Range(1, 1, 8, 8).Row(1).Merge();
            worksheet.Cell("A1").Style.Font.FontSize = 14;
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            var sql = @"select t.StartShip,el.Name as 'ExportList',bs.Name as 'BrandShip',s.Name as 'Ship' ,r.Name as 'Route',cont.Description as 'ContainerType',SUM(t.Cont40) as Cont40,SUM(t.Cont20) as Cont20
                    from Transportation t
                    left join Vendor as el on el.Id = t.ExportListId
                    left join Ship as s on s.Id = t.ShipId
                    left join Vendor as bs on bs.Id = t.BrandShipId
                    left join Route as r on r.Id = t.RouteId
                    left join MasterData as cont on cont.Id = t.ContainerTypeId
                    where BookingId is not null
                    group by el.Name,bs.Name,s.Name,r.Name,cont.Description, t.StartShip
                    order by StartShip desc";
            var data = await ConverSqlToDataSet(sql);
            worksheet.Cell("A2").Value = $"Ngày tàu chạy";
            worksheet.Cell("A2").Style.Alignment.WrapText = true;
            worksheet.Cell("A2").Style.Font.Bold = true;
            worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell("B2").Value = $"List xuất";
            worksheet.Cell("B2").Style.Alignment.WrapText = true;
            worksheet.Cell("B2").Style.Font.Bold = true;
            worksheet.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("B2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell("C2").Value = $"Hãng tàu";
            worksheet.Cell("C2").Style.Alignment.WrapText = true;
            worksheet.Cell("C2").Style.Font.Bold = true;
            worksheet.Cell("C2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("C2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell("D2").Value = $"Tàu";
            worksheet.Cell("D2").Style.Alignment.WrapText = true;
            worksheet.Cell("D2").Style.Font.Bold = true;
            worksheet.Cell("D2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("D2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell("E2").Value = $"Tuyến đường";
            worksheet.Cell("E2").Style.Alignment.WrapText = true;
            worksheet.Cell("E2").Style.Font.Bold = true;
            worksheet.Cell("E2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("E2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell("F2").Value = $"Loại cont";
            worksheet.Cell("F2").Style.Alignment.WrapText = true;
            worksheet.Cell("F2").Style.Font.Bold = true;
            worksheet.Cell("F2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("F2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell("G2").Value = $"Cont20";
            worksheet.Cell("G2").Style.Alignment.WrapText = true;
            worksheet.Cell("G2").Style.Font.Bold = true;
            worksheet.Cell("G2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("G2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell("H2").Value = $"Cont40";
            worksheet.Cell("H2").Style.Alignment.WrapText = true;
            worksheet.Cell("H2").Style.Font.Bold = true;
            worksheet.Cell("H2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("H2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range(2, 1, 2, 8).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Range(2, 1, 2, 8).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Range(2, 1, 2, 8).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Range(2, 1, 2, 8).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            var group = data[0].GroupBy(x => new { BrandShip = x["BrandShip"], Route = x["Route"], ExportList = x["ExportList"], Ship = x["Ship"], ContainerType = x["ContainerType"] });
            var i = 3;
            foreach (var item in group)
            {
                foreach (var itemDetail in item.ToList())
                {
                    worksheet.Cell("A" + i).Value = itemDetail["StartShip"] is null ? default(DateTime) : DateTime.Parse(itemDetail["StartShip"].ToString());
                    worksheet.Cell("B" + i).Value = itemDetail["ExportList"] is null ? default(string) : itemDetail["ExportList"].ToString();
                    worksheet.Cell("C" + i).Value = itemDetail["BrandShip"] is null ? default(string) : itemDetail["BrandShip"].ToString();
                    worksheet.Cell("D" + i).Value = itemDetail["Ship"] is null ? default(string) : itemDetail["Ship"].ToString();
                    worksheet.Cell("E" + i).Value = itemDetail["Route"] is null ? default(string) : itemDetail["Route"].ToString();
                    worksheet.Cell("F" + i).Value = itemDetail["ContainerType"] is null ? default(string) : itemDetail["ContainerType"].ToString();
                    worksheet.Cell("G" + i).Value = itemDetail["Cont20"] is null ? default(decimal) : decimal.Parse(itemDetail["Cont20"].ToString());
                    worksheet.Cell("H" + i).Value = itemDetail["Cont40"] is null ? default(decimal) : decimal.Parse(itemDetail["Cont40"].ToString());
                    worksheet.Range(i, 1, i, 8).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(i, 1, i, 8).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(i, 1, i, 8).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(i, 1, i, 8).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    i++;
                }
                worksheet.Cell("A" + i).Value = "Tổng cộng";
                worksheet.Cell("A" + i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell("A" + i).Style.Alignment.WrapText = true;
                worksheet.Cell("A" + i).Style.Font.Bold = true;
                worksheet.Range(i, 1, i, 6).Row(1).Merge();
                worksheet.Cell("G" + i).Style.Alignment.WrapText = true;
                worksheet.Cell("G" + i).Style.Font.Bold = true;
                worksheet.Cell("G" + i).Value = item.ToList().Sum(x => decimal.Parse(x["Cont20"].ToString()));
                worksheet.Cell("H" + i).Value = item.ToList().Sum(x => decimal.Parse(x["Cont40"].ToString()));
                worksheet.Cell("H" + i).Style.Alignment.WrapText = true;
                worksheet.Cell("H" + i).Style.Font.Bold = true;
                i++;
            }
            worksheet.Columns().AdjustToContents();
            var url = $"BaoCaoSanLuong.xlsx";
            workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
            return url;
        }

        public async Task<List<List<Dictionary<string, object>>>> ConverSqlToDataSet(string reportQuery)
        {
            var connectionStr = Startup.GetConnectionString(_serviceProvider, _config, "Default");
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

        private void SetBackgroundColor(Workbook workbook, string cell)
        {
            var style = workbook.Worksheets[0].Cells[cell].GetStyle();
            style.Pattern = BackgroundType.Solid;
            style.ForegroundColor = Color.LightGreen;
            workbook.Worksheets[0].Cells[cell].SetStyle(style);
            SetBorder(workbook, cell);
        }

        private void SetColor(Workbook workbook, string cell)
        {
            var style = workbook.Worksheets[0].Cells[cell].GetStyle();
            style.Font.Color = Color.Red;
            workbook.Worksheets[0].Cells[cell].SetStyle(style);
        }

        private void SetBorder(Workbook workbook, string cell)
        {
            var style = workbook.Worksheets[0].Cells[cell].GetStyle();
            style.SetBorder(BorderType.BottomBorder, CellBorderType.Thin, Color.Black);
            style.SetBorder(BorderType.LeftBorder, CellBorderType.Thin, Color.Black);
            style.SetBorder(BorderType.RightBorder, CellBorderType.Thin, Color.Black);
            style.SetBorder(BorderType.TopBorder, CellBorderType.Thin, Color.Black);
            workbook.Worksheets[0].Cells[cell].SetStyle(style);
        }

        private void SetAlign(Workbook workbook, string cell)
        {
            var style = workbook.Worksheets[0].Cells[cell].GetStyle();
            style.HorizontalAlignment = TextAlignmentType.Center;
            workbook.Worksheets[0].Cells[cell].SetStyle(style);
        }

        [HttpPost("api/Transportation/CheckFee")]
        public async Task<List<Transportation>> CheckFee([FromForm] DateTime FromDate, [FromForm] DateTime ToDate, [FromForm] int ClosingId, [FromServices] IWebHostEnvironment host, List<IFormFile> fileCheckFee, int type)
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
            Worksheet worksheet = workbook.Worksheets[0];
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
                    if (worksheet.Cells.Rows[row][0].Value is null)
                    {
                        break;
                    }
                    var datetimes = worksheet.Cells.Rows[row][1].Value.ToString();
                    var datetime = DateTime.Parse(datetimes);
                    var per = decimal.Parse(worksheet.Cells.Rows[row][22].Value is null || worksheet.Cells.Rows[row][22].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][22].Value.ToString().Replace("%", "").Replace(",", "").Trim());
                    var entity = new CheckCompineTransportationVM()
                    {
                        No = worksheet.Cells.Rows[row][0].Value.ToString().Trim(),
                        Vendor = worksheet.Cells.Rows[0][0].Value.ToString().Trim(),
                        ClosingDate = datetime,
                        Boss = worksheet.Cells.Rows[row][2].Value is null ? null : worksheet.Cells.Rows[row][2].Value.ToString().Trim(),
                        ContainerNo = worksheet.Cells.Rows[row][3].Value is null ? null : worksheet.Cells.Rows[row][3].Value.ToString().Trim(),
                        SealNo = worksheet.Cells.Rows[row][4].Value is null ? null : worksheet.Cells.Rows[row][4].Value.ToString().Trim(),
                        Cont20 = int.Parse(worksheet.Cells.Rows[row][5].Value is null || worksheet.Cells.Rows[row][5].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][5].Value.ToString().Trim()),
                        Cont40 = int.Parse(worksheet.Cells.Rows[row][6].Value is null || worksheet.Cells.Rows[row][6].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][6].Value.ToString().Trim()),
                        Received = worksheet.Cells.Rows[row][7].Value is null ? null : worksheet.Cells.Rows[row][7].Value.ToString().Trim(),
                        PickupEmpty = worksheet.Cells.Rows[row][8].Value is null ? null : worksheet.Cells.Rows[row][8].Value.ToString().Trim(),
                        PortLoading = worksheet.Cells.Rows[row][9].Value is null ? null : worksheet.Cells.Rows[row][9].Value.ToString().Trim(),
                        LiftFee = decimal.Parse(worksheet.Cells.Rows[row][10].Value is null || worksheet.Cells.Rows[row][10].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][10].Value.ToString().Replace(",", "").Trim()),
                        LandingFee = decimal.Parse(worksheet.Cells.Rows[row][11].Value is null || worksheet.Cells.Rows[row][11].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][11].Value.ToString().Replace(",", "").Trim()),
                        FeeVat1 = decimal.Parse(worksheet.Cells.Rows[row][12].Value is null || worksheet.Cells.Rows[row][12].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][12].Value.ToString().Replace(",", "").Trim()),
                        FeeVat2 = decimal.Parse(worksheet.Cells.Rows[row][13].Value is null || worksheet.Cells.Rows[row][13].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][13].Value.ToString().Replace(",", "").Trim()),
                        FeeVat3 = decimal.Parse(worksheet.Cells.Rows[row][14].Value is null || worksheet.Cells.Rows[row][14].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][14].Value.ToString().Replace(",", "").Trim()),
                        Fee1 = decimal.Parse(worksheet.Cells.Rows[row][15].Value is null || worksheet.Cells.Rows[row][15].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][15].Value.ToString().Replace(",", "").Trim()),
                        Fee2 = decimal.Parse(worksheet.Cells.Rows[row][16].Value is null || worksheet.Cells.Rows[row][16].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][16].Value.ToString().Replace(",", "").Trim()),
                        Fee3 = decimal.Parse(worksheet.Cells.Rows[row][17].Value is null || worksheet.Cells.Rows[row][17].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][17].Value.ToString().Replace(",", "").Trim()),
                        Fee4 = decimal.Parse(worksheet.Cells.Rows[row][18].Value is null || worksheet.Cells.Rows[row][18].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][18].Value.ToString().Replace(",", "").Trim()),
                        Fee5 = decimal.Parse(worksheet.Cells.Rows[row][19].Value is null || worksheet.Cells.Rows[row][19].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][19].Value.ToString().Replace(",", "").Trim()),
                        Fee6 = decimal.Parse(worksheet.Cells.Rows[row][20].Value is null || worksheet.Cells.Rows[row][20].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][20].Value.ToString().Replace(",", "").Trim()),
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
            if (type == 1)
            {
                var qr = db.Transportation.Where(x =>
                x.ClosingDate.Value.Date >= FromDate
                && x.ClosingDate.Value.Date <= ToDate
                && x.ExportListId == VendorId
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
                return null;
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
                $" ,[ExportListReturnId] = '{VendorId}',[UserReturnId] = '{UserId}'" +
                $" where ShipId = '{entity.ShipId}' and (BrandShipId = '{entity.BrandShipId}' or '{entity.BrandShipId}' = '') and Trip = '{entity.Trip}' and RouteId in ({entity.RouteIds.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
            return check;
        }

        [HttpPost("api/Transportation/ImportExcel")]
        public async Task<List<TransportationPlan>> ImportExcel([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
        {
            var formFile = fileImport.FirstOrDefault();
            if (formFile == null || formFile.Length <= 0)
            {
                return null;
            }

            if (!Path.GetExtension(formFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var path = GetUploadPath(formFile.FileName, host.WebRootPath);
            EnsureDirectoryExist(path);
            path = IncreaseFileName(path);
            using var stream = FileIO.Create(path);
            await formFile.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            var currentSheet = package.Workbook.Worksheets;
            var worksheet = currentSheet.First();
            var noOfCol = worksheet.Dimension.End.Column;
            var noOfRow = worksheet.Dimension.End.Row;
            var list = new List<ImportTransportation>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var listExport = worksheet.Cells[row, 3].Value?.ToString().Trim();
                var route = worksheet.Cells[row, 4].Value?.ToString().Trim();
                var booking = worksheet.Cells[row, 5].Value?.ToString().Trim();
                var name = worksheet.Cells[row, 6].Value?.ToString().Trim();
                var brandShip = worksheet.Cells[row, 7].Value?.ToString().Trim();
                var line = worksheet.Cells[row, 8].Value?.ToString().Trim();
                var ship = worksheet.Cells[row, 9].Value?.ToString().Trim();
                var closing = worksheet.Cells[row, 14].Value?.ToString().Trim();
                var soc = worksheet.Cells[row, 15].Value?.ToString().Trim();
                var emptyCombinationId = worksheet.Cells[row, 18].Value?.ToString().Trim();
                var containerTypeId = worksheet.Cells[row, 20].Value?.ToString().Trim();
                var boss = worksheet.Cells[row, 24].Value?.ToString().Trim();
                var sale = worksheet.Cells[row, 25].Value?.ToString().Trim();
                var commodityId = worksheet.Cells[row, 26].Value?.ToString().Trim();
                var received = worksheet.Cells[row, 30].Value?.ToString().Trim();
                var pickupEmptyId = worksheet.Cells[row, 35].Value?.ToString().Trim();
                var portLoadingId = worksheet.Cells[row, 36].Value?.ToString().Trim();
                var returnId = worksheet.Cells[row, 60].Value?.ToString().Trim();
                var returnVendorId = worksheet.Cells[row, 63].Value?.ToString().Trim();
                var portLiftId = worksheet.Cells[row, 68].Value?.ToString().Trim();
                var returnEmptyId = worksheet.Cells[row, 69].Value?.ToString().Trim();
                var transportationPlan = new ImportTransportation()
                {
                    ListExport = ConvertTextVn(listExport),
                    ListExportEn = ConvertTextEn(listExport),
                    Route = ConvertTextVn(route),
                    RouteEn = ConvertTextEn(route),
                    Booking = ConvertTextVn(booking),
                    BookingEn = ConvertTextEn(booking),
                    Name = ConvertTextVn(name),
                    NameEn = ConvertTextEn(name),
                    BrandShip = ConvertTextVn(brandShip),
                    BrandShipEn = ConvertTextEn(brandShip),
                    Line = ConvertTextVn(line),
                    LineEn = ConvertTextEn(line),
                    Ship = ConvertTextVn(ship),
                    ShipEn = ConvertTextEn(ship),
                    Trip = worksheet.Cells[row, 10].Value?.ToString().Trim(),
                    Bill = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                    ClosingDate = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                    StartDate = worksheet.Cells[row, 13].Value?.ToString().Trim(),
                    Closing = ConvertTextVn(closing),
                    ClosingEn = ConvertTextEn(closing),
                    Soc = ConvertTextVn(soc),
                    SocEn = ConvertTextEn(soc),
                    SplitBill = worksheet.Cells[row, 16].Value?.ToString().Trim(),
                    IsEmptyCombination = worksheet.Cells[row, 17].Value?.ToString().Trim(),
                    EmptyCombinationId = ConvertTextVn(emptyCombinationId),
                    EmptyCombinationIdEn = ConvertTextEn(emptyCombinationId),
                    IsClosingCustomer = worksheet.Cells[row, 19].Value?.ToString().Trim(),
                    ContainerTypeId = ConvertTextVn(containerTypeId),
                    ContainerTypeIdEn = ConvertTextEn(containerTypeId),
                    ContainerNo = worksheet.Cells[row, 22].Value?.ToString().Trim(),
                    SealNo = worksheet.Cells[row, 23].Value?.ToString().Trim(),
                    Boss = ConvertTextVn(boss),
                    BossEn = ConvertTextEn(boss),
                    Sale = ConvertTextVn(sale),
                    SaleEn = ConvertTextEn(sale),
                    CommodityId = ConvertTextVn(commodityId),
                    CommodityIdEn = ConvertTextEn(commodityId),
                    Cont20 = worksheet.Cells[row, 27].Value?.ToString().Trim(),
                    Cont40 = worksheet.Cells[row, 28].Value?.ToString().Trim(),
                    Weight = worksheet.Cells[row, 29].Value?.ToString().Trim(),
                    Received = ConvertTextVn(received),
                    ReceivedEn = ConvertTextEn(received),
                    ClosingNotes = worksheet.Cells[row, 31].Value?.ToString().Trim(),
                    ClosingUser = worksheet.Cells[row, 32].Value?.ToString().Trim(),
                    ClosingDriver = worksheet.Cells[row, 33].Value?.ToString().Trim(),
                    Closingtruck = worksheet.Cells[row, 34].Value?.ToString().Trim(),
                    PickupEmptyId = ConvertTextVn(pickupEmptyId),
                    PickupEmptyIdEn = ConvertTextEn(pickupEmptyId),
                    PortLoadingId = ConvertTextVn(portLoadingId),
                    PortLoadingIdEn = ConvertTextEn(portLoadingId),
                    IsClampingFee = worksheet.Cells[row, 37].Value?.ToString().Trim(),
                    ClosingUnitPrice = worksheet.Cells[row, 38].Value?.ToString().Trim(),
                    IsEmptyLift = worksheet.Cells[row, 39].Value?.ToString().Trim(),
                    IsLanding = worksheet.Cells[row, 40].Value?.ToString().Trim(),
                    LiftFee = worksheet.Cells[row, 41].Value?.ToString().Trim(),
                    LandingFee = worksheet.Cells[row, 42].Value?.ToString().Trim(),
                    CheckFee = worksheet.Cells[row, 43].Value?.ToString().Trim(),
                    OrtherFee = worksheet.Cells[row, 44].Value?.ToString().Trim(),
                    OrtherFeeInvoinceNo = worksheet.Cells[row, 45].Value?.ToString().Trim(),
                    CollectOnBehaftFee = worksheet.Cells[row, 46].Value?.ToString().Trim(),
                    CollectOnBehaftInvoinceNoFee = worksheet.Cells[row, 47].Value?.ToString().Trim(),
                    InsuranceFee = worksheet.Cells[row, 48].Value?.ToString().Trim(),
                    TotalFee = worksheet.Cells[row, 49].Value?.ToString().Trim(),
                    ShipPrice = worksheet.Cells[row, 50].Value?.ToString().Trim(),
                    ShipRoses = worksheet.Cells[row, 51].Value?.ToString().Trim(),
                    ShipNote = worksheet.Cells[row, 52].Value?.ToString().Trim(),
                    ShipDate = worksheet.Cells[row, 53].Value?.ToString().Trim(),
                    Dem = worksheet.Cells[row, 54].Value?.ToString().Trim(),
                    DemDate = worksheet.Cells[row, 55].Value?.ToString().Trim(),
                    LeftDate = worksheet.Cells[row, 56].Value?.ToString().Trim(),
                    ReturnDate = worksheet.Cells[row, 57].Value?.ToString().Trim(),
                    ClosingCont = worksheet.Cells[row, 58].Value?.ToString().Trim(),
                    ShellDate = worksheet.Cells[row, 59].Value?.ToString().Trim(),
                    ReturnId = ConvertTextVn(returnId),
                    ReturnIdEn = ConvertTextEn(returnId),
                    ReturnNotes = worksheet.Cells[row, 61].Value?.ToString().Trim(),
                    ReturnUserId = worksheet.Cells[row, 62].Value?.ToString().Trim(),
                    ReturnVendorId = ConvertTextVn(returnVendorId),
                    ReturnVendorIdEn = ConvertTextEn(returnVendorId),
                    ReturnDriverId = worksheet.Cells[row, 64].Value?.ToString().Trim(),
                    ReturnTruckId = worksheet.Cells[row, 65].Value?.ToString().Trim(),
                    NotificationCount = worksheet.Cells[row, 66].Value?.ToString().Trim(),
                    Bet = worksheet.Cells[row, 67].Value?.ToString().Trim(),
                    PortLiftId = ConvertTextVn(portLiftId),
                    PortLiftIdEn = ConvertTextEn(portLiftId),
                    ReturnEmptyId = ConvertTextVn(returnEmptyId),
                    ReturnEmptyIdEn = ConvertTextEn(returnEmptyId),
                    ReturnUnitPrice = worksheet.Cells[row, 70].Value?.ToString().Trim(),
                    IsLiftFee = worksheet.Cells[row, 71].Value?.ToString().Trim(),
                    IsClosingEmptyFee = worksheet.Cells[row, 72].Value?.ToString().Trim(),
                    ReturnLiftFee = worksheet.Cells[row, 73].Value?.ToString().Trim(),
                    ReturnClosingFee = worksheet.Cells[row, 74].Value?.ToString().Trim(),
                    ReturnDo = worksheet.Cells[row, 75].Value?.ToString().Trim(),
                    ReturnCheckFee = worksheet.Cells[row, 76].Value?.ToString().Trim(),
                    ReturnOrtherFee = worksheet.Cells[row, 77].Value?.ToString().Trim(),
                    ReturnOrtherInvoinceFee = worksheet.Cells[row, 78].Value?.ToString().Trim(),
                    ReturnCollectOnBehaftFee = worksheet.Cells[row, 79].Value?.ToString().Trim(),
                    ReturnCollectOnBehaftInvoinceFee = worksheet.Cells[row, 80].Value?.ToString().Trim(),
                    ReturnPlusFee = worksheet.Cells[row, 81].Value?.ToString().Trim(),
                    ReturnTotalFee = worksheet.Cells[row, 82].Value?.ToString().Trim(),
                    Notes = worksheet.Cells[row, 83].Value?.ToString().Trim(),
                    IsKt = worksheet.Cells[row, 84].Value?.ToString().Trim(),
                    InsertedBy = worksheet.Cells[row, 85].Value?.ToString().Trim(),
                };
                list.Add(transportationPlan);
            }

            var listListExport = list.Select(x => x.ListExportEn).Where(x => x != null && x != "").Distinct().ToList();
            var listRoute = list.Select(x => x.RouteEn).Where(x => x != null && x != "").Distinct().ToList();
            var listBooking = list.Select(x => x.BookingEn).Where(x => x != null && x != "").Distinct().ToList();
            var listName = list.Select(x => x.NameEn).Where(x => x != null && x != "").Distinct().ToList();
            var listBrandShip = list.Select(x => x.BrandShipEn).Where(x => x != null && x != "").Distinct().ToList();
            var listLine = list.Select(x => x.LineEn).Where(x => x != null && x != "").Distinct().ToList();
            var listShip = list.Select(x => x.ShipEn).Where(x => x != null && x != "").Distinct().ToList();
            var listClosing = list.Select(x => x.ClosingEn).Where(x => x != null && x != "").Distinct().ToList();
            var listSoc = list.Select(x => x.SocEn).Where(x => x != null && x != "").Distinct().ToList();
            var listEmptyCombination = list.Select(x => x.EmptyCombinationIdEn).Where(x => x != null && x != "").Distinct().ToList();
            var listContainerType = list.Select(x => x.ContainerTypeId).Where(x => x != null && x != "").Distinct().ToList();
            var listBoss = list.Select(x => x.BossEn).Where(x => x != null && x != "").Distinct().ToList();
            var listSale = list.Select(x => x.SaleEn).Where(x => x != null && x != "").Distinct().ToList();
            var listCommodity = list.Select(x => x.CommodityIdEn).Where(x => x != null && x != "").Distinct().ToList();
            var listReceived = list.Select(x => x.ReceivedEn).Where(x => x != null && x != "").Distinct().ToList();
            var listPickupEmpty = list.Select(x => x.PickupEmptyIdEn).Where(x => x != null && x != "").Distinct().ToList();
            var listPortLoading = list.Select(x => x.PortLoadingIdEn).Where(x => x != null && x != "").Distinct().ToList();
            var listReturn = list.Select(x => x.ReturnIdEn).Where(x => x != null && x != "").Distinct().ToList();
            var listReturnVendor = list.Select(x => x.ReturnVendorIdEn).Where(x => x != null && x != "").Distinct().ToList();
            var listPortLift = list.Select(x => x.PortLiftIdEn).Where(x => x != null && x != "").Distinct().ToList();
            var listReturnEmpty = list.Select(x => x.ReturnEmptyIdEn).Where(x => x != null && x != "").Distinct().ToList();
            var listUser = list.Select(x => x.InsertedBy).Where(x => x != null && x != "").Distinct().ToList();

            var rsVendor = await db.Vendor.ToListAsync();
            var rsRoute = await db.Route.ToListAsync();
            var rsBooking = await db.Booking.ToListAsync();
            var rsTransportationPlan = await db.TransportationPlan.ToListAsync();
            var rsShip = await db.Ship.ToListAsync();
            var rsMasterData = await db.MasterData.ToListAsync();
            var rsLocation = await db.Location.ToListAsync();

            var listExportDB = rsVendor.Where(x => listListExport.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7552).ToDictionary(x => ConvertTextEn(x.Name));
            var routeDB = rsRoute.Where(x => listRoute.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var bookingDB1 = rsBooking.Where(x => listBooking.Contains(ConvertTextEn(x.BookingNo))).OrderByDescending(x => x.Id).ToList();
            var bookingDB = bookingDB1.ToDictionary(x => ConvertTextEn(x.BookingNo));
            var tranpDB1 = rsTransportationPlan.Where(x => listName.Contains(ConvertTextEn(x.Name))).ToList();
            var tranpDB = tranpDB1.ToDictionaryDistinct(x => $"{x.Name}{(x.ClosingDate is null ? null : x.ClosingDate)}");
            var vendorDB1 = rsVendor.Where(x => (listBrandShip.Contains(ConvertTextEn(x.Name)) || listLine.Contains(ConvertTextEn(x.Name)) || listSoc.Contains(ConvertTextEn(x.Name)) || listEmptyCombination.Contains(ConvertTextEn(x.Name)) || listReturnVendor.Contains(ConvertTextEn(x.Name)) || listClosing.Contains(ConvertTextEn(x.Name))) && x.TypeId == 7552).ToList();
            var vendorDB = vendorDB1.ToDictionary(x => x.Name);
            var shipDB1 = rsShip.Where(x => listShip.Contains(ConvertTextEn(x.Name))).ToList();
            var shipDB = shipDB1.ToDictionary(x => x.Name + x.BrandShipId);
            var containerDB = rsMasterData.Where(x => listContainerType.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7565).ToDictionary(x => ConvertTextEn(x.Description));
            var bossDB = rsVendor.Where(x => listBoss.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7551).ToDictionary(x => ConvertTextEn(x.Name));
            var userDB = await db.User.Where(x => listSale.Contains(x.UserName) || listUser.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            var commodityDB1 = rsMasterData.Where(x => listCommodity.Contains(ConvertTextEn(x.Description)) && x.Path.Contains(@"\7651\") && x.ParentId != 7651).ToList();
            var commodityDB = commodityDB1.ToDictionary(x => ConvertTextEn(x.Description));
            var receivedDB = rsLocation.Where(x => listReceived.Contains(ConvertTextEn(x.Description)) || listPickupEmpty.Contains(ConvertTextEn(x.Description)) || listPortLoading.Contains(ConvertTextEn(x.Description)) || listPortLoading.Contains(ConvertTextEn(x.Description)) || listReturn.Contains(ConvertTextEn(x.Description)) || listPortLift.Contains(ConvertTextEn(x.Description)) || listPortLift.Contains(ConvertTextEn(x.Description)) || listReturnEmpty.Contains(ConvertTextEn(x.Description))).OrderByDescending(x => x.Id).ToDictionary(x => ConvertTextEn(x.Description));
            foreach (var item in list)
            {
                var exportListId = item.ListExport is null ? null : listExportDB.GetValueOrDefault(item.ListExportEn);
                var bookingId = item.Booking is null ? null : bookingDB.GetValueOrDefault(item.BookingEn);
                var KEY = $"{item.NameEn}{(item.ClosingDate is null ? null : DateTime.Parse(item.ClosingDate))}";
                var tranpId = item.Name is null ? null : tranpDB.GetValueOrDefault(KEY);
                var brandShipId = item.BrandShip is null ? null : vendorDB.GetValueOrDefault(item.BrandShipEn);
                var lineId = item.Line is null ? null : vendorDB.GetValueOrDefault(item.LineEn);
                var shipId = item.Ship is null ? null : shipDB.GetValueOrDefault(item.ShipEn);
                var closingId = item.Closing is null ? null : vendorDB.GetValueOrDefault(item.ClosingEn);
                var socId = item.Soc is null ? null : vendorDB.GetValueOrDefault(item.Soc);
                var emptyCombinationId = item.EmptyCombinationId is null ? null : vendorDB.GetValueOrDefault(item.EmptyCombinationIdEn);
                var containerTypeId = item.ContainerTypeId is null ? null : containerDB.GetValueOrDefault(item.ContainerTypeIdEn);
                var bossId = item.Boss is null ? null : bossDB.GetValueOrDefault(item.BossEn);
                var saleId = item.Sale is null ? null : userDB.GetValueOrDefault(item.SaleEn);
                var commodityId = item.CommodityId is null ? null : commodityDB.GetValueOrDefault(item.CommodityIdEn);
                var receivedId = item.Received is null ? null : receivedDB.GetValueOrDefault(item.ReceivedEn);
                var pickupEmptyId = item.PickupEmptyId is null ? null : receivedDB.GetValueOrDefault(item.PickupEmptyIdEn);
                var portLoadingId = item.PortLoadingId is null ? null : receivedDB.GetValueOrDefault(item.PortLoadingIdEn);
                var returnId = item.ReturnId is null ? null : receivedDB.GetValueOrDefault(item.ReturnIdEn);
                var returnVendorId = item.ReturnVendorId is null ? null : vendorDB.GetValueOrDefault(item.ReturnVendorIdEn);
                var insertedById = item.InsertedBy is null ? null : userDB.GetValueOrDefault(item.InsertedBy);
                var portLiftIdId = item.PortLiftId is null ? null : userDB.GetValueOrDefault(item.PortLiftIdEn);
                var returnEmptyId = item.ReturnEmptyId is null ? null : userDB.GetValueOrDefault(item.ReturnEmptyIdEn);
                var routeId = item.Route is null ? null : routeDB.GetValueOrDefault(item.RouteEn + brandShipId.Id);
                var tran = new Transportation()
                {
                    CheckFeeHistoryId = exportListId is null ? null : exportListId.Id,
                    ExportListId = exportListId is null ? null : exportListId.Id,
                    RouteId = routeId is null ? null : routeId.Id,
                    BookingId = bookingId is null ? null : bookingId.Id,
                    TransportationPlanId = tranpId is null ? null : tranpId.Id,
                    BrandShipId = brandShipId is null ? null : brandShipId.Id,
                    LineId = lineId is null ? null : lineId.Id,
                    ShipId = shipId is null ? null : shipId.Id,
                    Trip = item.Trip,
                    BillNo = item.Bill,
                    ClosingDate = item.ClosingDate is null ? null : DateTime.Parse(item.ClosingDate),
                    StartShip = item.StartDate is null ? null : DateTime.Parse(item.StartDate),
                    ClosingId = closingId is null ? null : closingId.Id,
                    SocId = socId is null ? null : socId.Id,
                    SplitBill = item.SplitBill,
                    IsEmptyCombination = item.IsEmptyCombination == "1" ? true : false,
                    EmptyCombinationId = emptyCombinationId is null ? null : emptyCombinationId.Id,
                    IsClosingCustomer = item.IsClosingCustomer == "1" ? true : false,
                    ContainerTypeId = containerTypeId is null ? null : containerTypeId.Id,
                    ContainerNo = item.ContainerNo,
                    SealNo = item.SealNo,
                    BossId = bossId is null ? null : bossId.Id,
                    UserId = saleId is null ? null : saleId.Id,
                    CommodityId = commodityId is null ? null : commodityId.Id,
                    Cont20 = decimal.Parse((item.Cont20 == "" || item.Cont20 is null) ? "0" : item.Cont20.Replace(",", "")),
                    Cont40 = decimal.Parse((item.Cont40 == "" || item.Cont40 is null) ? "0" : item.Cont40.Replace(",", "")),
                    Weight = decimal.Parse((item.Weight == "" || item.Weight is null) ? "0" : item.Weight.Replace(",", "")),
                    ReceivedId = receivedId is null ? null : receivedId.Id,
                    ClosingNotes = item.ClosingNotes,
                    ClosingUserId = item.ClosingUser,
                    ClosingDriverId = item.ClosingDriver,
                    ClosingTruckId = item.Closingtruck,
                    PickupEmptyId = pickupEmptyId is null ? null : pickupEmptyId.Id,
                    PortLoadingId = portLoadingId is null ? null : portLoadingId.Id,
                    IsClampingFee = item.IsClampingFee == "1" ? true : false,
                    ClosingUnitPrice = decimal.Parse((item.ClosingUnitPrice == "" || item.ClosingUnitPrice is null) ? "0" : item.ClosingUnitPrice.Replace(",", "")),
                    IsEmptyLift = item.IsEmptyLift == "1" ? true : false,
                    IsLanding = item.IsEmptyLift == "1" ? true : false,
                    LiftFee = decimal.Parse((item.LiftFee == "" || item.LiftFee is null) ? "0" : item.LiftFee.Replace(",", "")),
                    LandingFee = decimal.Parse((item.LandingFee == "" || item.LandingFee is null) ? "0" : item.LandingFee.Replace(",", "")),
                    CheckFee = decimal.Parse((item.CheckFee == "" || item.CheckFee is null) ? "0" : item.CheckFee.Replace(",", "")),
                    OrtherFee = decimal.Parse((item.OrtherFee == "" || item.OrtherFee is null) ? "0" : item.OrtherFee.Replace(",", "")),
                    OrtherFeeInvoinceNo = decimal.Parse((item.OrtherFeeInvoinceNo == "" || item.OrtherFeeInvoinceNo is null) ? "0" : item.OrtherFeeInvoinceNo.Replace(",", "")),
                    CollectOnBehaftFee = decimal.Parse((item.CollectOnBehaftFee == "" || item.CollectOnBehaftFee is null) ? "0" : item.CollectOnBehaftFee.Replace(",", "")),
                    CollectOnBehaftInvoinceNoFee = decimal.Parse((item.CollectOnBehaftInvoinceNoFee == "" || item.CollectOnBehaftInvoinceNoFee is null) ? "0" : item.CollectOnBehaftInvoinceNoFee.Replace(",", "")),
                    InsuranceFee = decimal.Parse((item.InsuranceFee == "" || item.InsuranceFee is null) ? "0" : item.InsuranceFee.Replace(",", "")),
                    TotalFee = decimal.Parse((item.TotalFee == "" || item.TotalFee is null) ? "0" : item.TotalFee.Replace(",", "")),
                    ShipPrice = decimal.Parse((item.ShipPrice == "" || item.ShipPrice is null) ? "0" : item.ShipPrice.Replace(",", "")),
                    ShipRoses = decimal.Parse((item.ShipRoses == "" || item.ShipRoses is null) ? "0" : item.ShipRoses.Replace(",", "")),
                    ShipNotes = item.ShipNote,
                    ShipDate = item.ShipDate is null ? null : DateTime.Parse(item.ShipDate),
                    Dem = decimal.Parse((item.Dem == "" || item.Dem is null) ? "0" : item.Dem.Replace(",", "")),
                    DemDate = item.DemDate is null ? null : DateTime.Parse(item.DemDate),
                    LeftDate = item.LeftDate is null ? null : DateTime.Parse(item.LeftDate),
                    ReturnDate = item.ReturnDate is null ? null : DateTime.Parse(item.ReturnDate),
                    ClosingCont = item.ClosingCont is null ? null : DateTime.Parse(item.ClosingCont),
                    ShellDate = decimal.Parse((item.ShellDate == "" || item.ShellDate is null) ? "0" : item.ShellDate.Replace(",", "")),
                    ReturnId = returnId is null ? null : returnId.Id,
                    ReturnNotes = item.ReturnNotes,
                    ReturnUserId = item.ReturnUserId,
                    ReturnVendorId = returnVendorId is null ? null : returnVendorId.Id,
                    ReturnDriverId = item.ReturnDriverId,
                    ReturnTruckId = item.ReturnTruckId,
                    NotificationCount = item.NotificationCount,
                    Bet = item.Bet,
                    PortLiftId = portLiftIdId is null ? null : portLiftIdId.Id,
                    ReturnEmptyId = returnEmptyId is null ? null : returnEmptyId.Id,
                    ReturnUnitPrice = decimal.Parse((item.ShellDate == "" || item.ShellDate is null) ? "0" : item.ShellDate.Replace(",", "")),
                    IsLiftFee = item.IsLiftFee == "1" ? true : false,
                    IsClosingEmptyFee = item.IsClosingEmptyFee == "1" ? true : false,
                    ReturnLiftFee = decimal.Parse((item.ReturnLiftFee == "" || item.ReturnLiftFee is null) ? "0" : item.ReturnLiftFee.Replace(",", "")),
                    ReturnClosingFee = decimal.Parse((item.ReturnClosingFee == "" || item.ReturnClosingFee is null) ? "0" : item.ReturnClosingFee.Replace(",", "")),
                    ReturnDo = decimal.Parse((item.ReturnDo == "" || item.ReturnDo is null) ? "0" : item.ReturnDo.Replace(",", "")),
                    ReturnCheckFee = decimal.Parse((item.ReturnCheckFee == "" || item.ReturnCheckFee is null) ? "0" : item.ReturnCheckFee.Replace(",", "")),
                    ReturnOrtherFee = decimal.Parse((item.ReturnOrtherFee == "" || item.ReturnOrtherFee is null) ? "0" : item.ReturnOrtherFee.Replace(",", "")),
                    ReturnOrtherInvoinceFee = decimal.Parse((item.ReturnOrtherInvoinceFee == "" || item.ReturnOrtherInvoinceFee is null) ? "0" : item.ReturnOrtherInvoinceFee.Replace(",", "")),
                    ReturnCollectOnBehaftFee = decimal.Parse((item.ReturnCollectOnBehaftFee == "" || item.ReturnCollectOnBehaftFee is null) ? "0" : item.ReturnCollectOnBehaftFee.Replace(",", "")),
                    ReturnCollectOnBehaftInvoinceFee = decimal.Parse((item.ReturnCollectOnBehaftInvoinceFee == "" || item.ReturnCollectOnBehaftInvoinceFee is null) ? "0" : item.ReturnCollectOnBehaftInvoinceFee.Replace(",", "")),
                    ReturnPlusFee = decimal.Parse((item.ReturnPlusFee == "" || item.ReturnPlusFee is null) ? "0" : item.ReturnPlusFee.Replace(",", "")),
                    ReturnTotalFee = decimal.Parse((item.ReturnTotalFee == "" || item.ReturnTotalFee is null) ? "0" : item.ReturnTotalFee.Replace(",", "")),
                    Notes = item.Notes,
                    IsKt = item.IsKt == "1" ? true : false,
                    InsertedBy = insertedById is null ? 1 : insertedById.Id,
                    Active = true,
                    InsertedDate = DateTime.Now,
                    Name = item.Name
                };
                db.Add(tran);
            }
            await db.SaveChangesAsync();
            return null;
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
                sql += @$" and (UserId = {UserId} or InsertedBy = {UserId})";
            }
            else if (RoleIds.Contains(25))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where TypeId = 25045 and UserId = {UserId}))";
            }
            else if (RoleIds.Contains(27))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId}))";
            }
            var qr = db.Transportation.AsNoTracking();
            if (RoleIds.Contains(10))
            {
                qr = qr.Where(x => x.UserId == UserId || x.InsertedBy == UserId);
            }
            else if (RoleIds.Contains(43))
            {
                qr = qr.Where(x => x.UserId == 78 || x.InsertedBy == UserId || x.UserId == UserId);
            }
            else if (RoleIds.Contains(17))
            {
                qr = qr.Where(x => x.UserId == 78 || x.UserId == UserId);
            }
            else if (RoleIds.Contains(25))
            {
                qr = from tr in qr
                     join route in db.UserRoute.AsNoTracking()
                     on tr.RouteId equals route.RouteId
                     where route.UserId == UserId && route.TypeId == 25045
                     select tr;
            }
            else if (RoleIds.Contains(27))
            {
                qr = from tr in qr
                     join route in db.UserRoute.AsNoTracking()
                     on tr.RouteId equals route.RouteId
                     where route.UserId == UserId
                     select tr;
            }
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
            else if (RoleIds.Contains(25) || RoleIds.Contains(27) || RoleIds.Contains(22))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId}))";
            }
            var qr = db.Transportation.AsNoTracking();
            if (RoleIds.Contains(10))
            {
                qr = qr.Where(x => x.UserId == UserId || x.InsertedBy == UserId);
            }
            else if (RoleIds.Contains(25) || RoleIds.Contains(27) || RoleIds.Contains(22))
            {
                qr = from tr in qr
                     join route in db.UserRoute.AsNoTracking()
                     on tr.RouteId equals route.RouteId
                     where route.UserId == UserId
                     select tr;
            }
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
            else if (RoleIds.Contains(27))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId}))";
            }
            else if (RoleIds.Contains(25) || RoleIds.Contains(22))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25044))";
            }
            var qr = db.Transportation.AsNoTracking();
            if (RoleIds.Contains(10))
            {
                qr = qr.Where(x => x.UserId == UserId || x.InsertedBy == UserId);
            }
            else if (RoleIds.Contains(27))
            {
                qr = from tr in qr
                     join route in db.UserRoute.AsNoTracking()
                     on tr.RouteId equals route.RouteId
                     where route.UserId == UserId
                     select tr;
            }
            if (RoleIds.Contains(22) || RoleIds.Contains(25))
            {
                qr = from tr in qr
                     join route in db.UserRoute.AsNoTracking()
                     on tr.RouteId equals route.RouteId
                     where route.UserId == UserId && route.TypeId == 25044
                     select tr;
            }
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

        public static string ConvertTextEn(string text)
        {
            return text is null || text == "" ? "" : Regex.Replace(text.ToLower().Trim(), @"\s+", " ");
        }

        public static string ConvertTextVn(string text)
        {
            return text is null || text == "" ? "" : Regex.Replace(text.Trim(), @"\s+", " ");
        }

        public override async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ViewSumary([FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config, [FromBody] string sum, [FromQuery] string group, [FromQuery] string tablename, [FromQuery] string refname, [FromQuery] string formatsumary, [FromQuery] string orderby, [FromQuery] string sql, [FromQuery] string where)
        {
            var connectionStr = Startup.GetConnectionString(serviceProvider, config, "Default");
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
                                 from ({sql}) as [{tablename}] where 1=1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")})";
                }
                else
                {
                    reportQuery += $@" select *
                                 from [{refname}] 
                                 where Id in (select distinct {group}
                                              from [{tablename}]
                                 where 1 = 1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")})";

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

        public override async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> SubTotal([FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config, [FromBody] string sum, [FromQuery] string group, [FromQuery] string tablename, [FromQuery] string refname, [FromQuery] string formatsumary, [FromQuery] string orderby, [FromQuery] string sql, [FromQuery] string where)
        {
            var connectionStr = Startup.GetConnectionString(serviceProvider, config, "Default");
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

        [HttpPost("api/Transportation/RequestUnLock")]
        public async Task RequestUnLock([FromBody] Transportation transportation)
        {
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
            await _taskService.SendMessageAllUser(new WebSocketResponse<Transportation>
            {
                EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                Data = transportation
            });
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
                var tran = await db.Transportation.Where(x => x.Id == transportation.Id).FirstOrDefaultAsync();
                var tranRequest = new TransportationRequest();
                tranRequest.Id = 0;
                tranRequest.IsRequestUnLockExploit = true;
                tranRequest.ReasonUnLockExploit = transportation.ReasonUnLockExploit;
                tranRequest.TransportationId = transportation.Id;
                tranRequest.Active = true;
                tranRequest.InsertedBy = UserId;
                tranRequest.InsertedDate = DateTime.Now.Date;
                db.Add(tranRequest);
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = listUser.Select(user => new TaskNotification
                {
                    Title = $"{currentUser.FullName}",
                    Description = $"Đã gửi yêu cầu mở khóa",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = tranRequest.Id,
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

        [HttpPost("api/Transportation/RequestUnLockAccountant")]
        public async Task RequestUnLockAccountant([FromBody] Transportation transportation)
        {
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
            await _taskService.SendMessageAllUser(new WebSocketResponse<Transportation>
            {
                EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                Data = transportation
            });
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
                var tran = await db.Transportation.Where(x => x.Id == transportation.Id).FirstOrDefaultAsync();
                var tranRequest = new TransportationRequest();
                tranRequest.Id = 0;
                tranRequest.IsRequestUnLockAccountant = true;
                tranRequest.ReasonUnLockAccountant = transportation.ReasonUnLockAccountant;
                tranRequest.TransportationId = transportation.Id;
                tranRequest.Active = true;
                tranRequest.InsertedBy = UserId;
                tranRequest.InsertedDate = DateTime.Now.Date;
                db.Add(tranRequest);
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = listUser.Select(user => new TaskNotification
                {
                    Title = $"{currentUser.FullName}",
                    Description = $"Đã gửi yêu cầu mở khóa",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = transportation.Id,
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

        [HttpPost("api/Transportation/RequestUnLockAll")]
        public async Task RequestUnLockAll([FromBody] Transportation transportation)
        {
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
            await _taskService.SendMessageAllUser(new WebSocketResponse<Transportation>
            {
                EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                Data = transportation
            });
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
                var tran = await db.Transportation.Where(x => x.Id == transportation.Id).FirstOrDefaultAsync();
                var tranRequest = new TransportationRequest();
                tranRequest.Id = 0;
                tranRequest.IsRequestUnLockAll = true;
                tranRequest.ReasonUnLockAll = transportation.ReasonUnLockAll;
                tranRequest.TransportationId = transportation.Id;
                tranRequest.Active = true;
                tranRequest.InsertedBy = UserId;
                tranRequest.InsertedDate = DateTime.Now.Date;
                db.Add(tranRequest);
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = listUser.Select(user => new TaskNotification
                {
                    Title = $"{currentUser.FullName}",
                    Description = $"Đã gửi yêu cầu mở khóa",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = transportation.Id,
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

        [HttpPost("api/Transportation/RequestUnLockShip")]
        public async Task RequestUnLockShip([FromBody] Transportation transportation)
        {
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
            await _taskService.SendMessageAllUser(new WebSocketResponse<Transportation>
            {
                EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                Data = transportation
            });
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
                var tran = await db.Transportation.Where(x => x.Id == transportation.Id).FirstOrDefaultAsync();
                var tranRequest = new TransportationRequest();
                tranRequest.Id = 0;
                tranRequest.IsRequestUnLockShip = true;
                tranRequest.ReasonUnLockShip = transportation.ReasonUnLockShip;
                tranRequest.TransportationId = transportation.Id;
                tranRequest.Active = true;
                tranRequest.InsertedBy = UserId;
                tranRequest.InsertedDate = DateTime.Now.Date;
                db.Add(tranRequest);
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = listUser.Select(user => new TaskNotification
                {
                    Title = $"{currentUser.FullName}",
                    Description = $"Đã gửi yêu cầu mở khóa",
                    EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                    RecordId = transportation.Id,
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

        [HttpPost("api/Transportation/ApproveUnLockAll")]
        public async Task<bool> ApproveUnLockAll([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var cmd = $"Update [{nameof(Transportation)}] set IsLocked = 0" +
                $" where Id in ({ids.Combine()})";
            cmd += $" Update [{nameof(TransportationRequest)}] set Active = 0" +
                $" where Id in ({tranRequestIds.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
            return true;
        }

        [HttpPost("api/Transportation/ApproveUnLockTransportation")]
        public async Task<bool> ApproveUnLockTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var cmd = $"Update [{nameof(Transportation)}] set IsKt = 0" +
                $" where Id in ({ids.Combine()})";
            cmd += $" Update [{nameof(TransportationRequest)}] set Active = 0" +
                $" where Id in ({tranRequestIds.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
            return true;
        }

        [HttpPost("api/Transportation/ApproveUnLockAccountantTransportation")]
        public async Task<bool> ApproveUnLockAccountantTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var cmd = $"Update [{nameof(Transportation)}] set IsSubmit = 0" +
                $" where Id in ({ids.Combine()})";
            cmd += $" Update [{nameof(TransportationRequest)}] set Active = 0" +
                $" where Id in ({tranRequestIds.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
            return true;
        }

        [HttpPost("api/Transportation/ApproveUnLockShip")]
        public async Task<bool> ApproveUnLockShip([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var tranRequestIds = await db.TransportationRequest.Where(x => ids.Contains((int)x.TransportationId)).Select(x => x.Id).ToListAsync();
            var cmd = $"Update [{nameof(Transportation)}] set LockShip = 0" +
                $" where Id in ({ids.Combine()})";
            cmd += $" Update [{nameof(TransportationRequest)}] set Active = 0" +
                $" where Id in ({tranRequestIds.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
            return true;
        }

        [HttpPost("api/Transportation/LockAllTransportation")]
        public async Task<bool> LockAllTransportation([FromBody] List<Transportation> transportations)
        {
            if (transportations == null)
            {
                return false;
            }
            var ids = transportations.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Transportation)}] set IsLocked = 1" +
                $" where Id in ({ids.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
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
                $" where Id in ({ids.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
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
                $" where Id in ({ids.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
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
                $" where Id in ({ids.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
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
                $" where Id in ({ids.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
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
                $" where Id in ({ids.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
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
                $" where Id in ({ids.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
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
                $" where Id in ({ids.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
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
                $" where Id in ({ids.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
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
                $" where Id in ({ids.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
            return true;
        }

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
    }
}
