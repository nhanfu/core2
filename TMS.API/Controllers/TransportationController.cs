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
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
            Transportation entity = default;
            patch.Changes.AddRange(new List<PatchUpdateDetail>
            {
               new PatchUpdateDetail { Field = nameof(Transportation.ExportListReturnId), Value = VendorId.ToString() },
               new PatchUpdateDetail { Field = nameof(Transportation.UserReturnId), Value = UserId.ToString() },
            });
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
            entity = await db.Set<Transportation>().FindAsync(idInt);
            patch.ApplyTo(entity);
            if (!entity.IsEmptyCombination && !entity.IsClosingCustomer)
            {
                entity.ClosingPercent = 0;
            }
            SetAuditInfo(entity);
            if (patch.Changes.Any(x => x.Field != nameof(entity.Notes) &&
            x.Field != nameof(entity.Id) &&
            x.Field != nameof(entity.ExportListReturnId) &&
            x.Field != nameof(entity.UserReturnId)))
            {
                var oldEntity = await db.Transportation.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
                if (oldEntity.IsLocked && entity.IsLocked)
                {
                    throw new ApiException("DSVC này đã được khóa. Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
                }
                if (patch.Changes.Any(x => x.Field == nameof(entity.ShipPrice) ||
                x.Field == nameof(entity.PolicyId) ||
                x.Field == nameof(entity.RouteId) ||
                x.Field == nameof(entity.BrandShipId) ||
                x.Field == nameof(entity.ShipId) ||
                x.Field == nameof(entity.Trip) ||
                x.Field == nameof(entity.StartShip) ||
                x.Field == nameof(entity.ContainerTypeId) ||
                x.Field == nameof(entity.BookingId)))
                {
                    if (entity.LockShip)
                    {
                        throw new ApiException("DSVC này đã được khóa cước tàu.") { StatusCode = HttpStatusCode.BadRequest };
                    }
                }
                if (patch.Changes.Any(x => x.Field == nameof(entity.MonthText)
                || x.Field == nameof(entity.YearText)
                || x.Field == nameof(entity.ExportListId)
                || x.Field == nameof(entity.RouteId)
                || x.Field == nameof(entity.ShipId)
                || x.Field == nameof(entity.Trip)
                || x.Field == nameof(entity.ClosingDate)
                || x.Field == nameof(entity.StartShip)
                || x.Field == nameof(entity.ContainerTypeId)
                || x.Field == nameof(entity.ContainerNo)
                || x.Field == nameof(entity.SealNo)
                || x.Field == nameof(entity.BossId)
                || x.Field == nameof(entity.UserId)
                || x.Field == nameof(entity.CommodityId)
                || x.Field == nameof(entity.Cont20)
                || x.Field == nameof(entity.Cont40)
                || x.Field == nameof(entity.Weight)
                || x.Field == nameof(entity.ReceivedId)
                || x.Field == nameof(entity.FreeText2)
                || x.Field == nameof(entity.LeftDate)))
                {
                    if (entity.IsKt)
                    {
                        throw new ApiException("DSVC này đã được khóa. Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
                    }
                }
                if (patch.Changes.Any(x => x.Field == nameof(entity.LotNo)
                || x.Field == nameof(entity.LotDate)
                || x.Field == nameof(entity.Vat)
                || x.Field == nameof(entity.UnitPriceAfterTax)
                || x.Field == nameof(entity.UnitPriceBeforeTax)
                || x.Field == nameof(entity.ReceivedPrice)
                || x.Field == nameof(entity.CollectOnBehaftPrice)
                || x.Field == nameof(entity.NotePayment)
                || x.Field == nameof(entity.VendorVatId)))
                {
                    if (entity.IsSubmit)
                    {
                        throw new ApiException("DSVC này đã được khóa. Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
                    }
                }
                if (patch.Changes.Any(x => x.Field == nameof(entity.ClosingDate)
                || x.Field == nameof(entity.RouteId)
                || x.Field == nameof(entity.BrandShipId)
                || x.Field == nameof(entity.LineId)
                || x.Field == nameof(entity.ShipId)
                || x.Field == nameof(entity.Trip)
                || x.Field == nameof(entity.StartShip)
                || x.Field == nameof(entity.ContainerTypeId)
                || x.Field == nameof(entity.SocId)))
                {
                    var bookingList = await db.BookingList.Where(x => x.Id == entity.BookingListId).FirstOrDefaultAsync();
                    if (bookingList != null && bookingList.Submit)
                    {
                        throw new ApiException("DSVC này đã được khóa. Vì đã được khóa ở danh sách book tàu.") { StatusCode = HttpStatusCode.BadRequest };
                    }
                }
            }

            var expenseTypes = await db.MasterData.Where(x => x.ParentId == 7577 && (x.Name.Contains("Bảo hiểm") || x.Name.Contains("BH SOC"))).ToListAsync();
            var expenseTypeIds = expenseTypes.Select(x => x.Id.ToString()).ToList();
            var expense = await db.Expense.Where(x => x.TransportationId == entity.Id && expenseTypeIds.Contains(x.ExpenseTypeId.ToString()) && x.RequestChangeId == null && x.IsPurchasedInsurance && x.Active).ToListAsync();
            if (expense != null)
            {
                var oldEntity = await db.Transportation.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
                if (patch.Changes.Any(x =>
                x.Field == nameof(oldEntity.BookingId) ||
                x.Field == nameof(oldEntity.ShipId) ||
                x.Field == nameof(oldEntity.SaleId) ||
                x.Field == nameof(oldEntity.Trip) ||
                x.Field == nameof(oldEntity.SealNo) ||
                x.Field == nameof(oldEntity.ContainerNo) ||
                x.Field == nameof(oldEntity.Notes) ||
                x.Field == nameof(oldEntity.StartShip) ||
                x.Field == nameof(oldEntity.ClosingDate)) &&
                (oldEntity.BookingId != entity.BookingId) ||
                (oldEntity.ShipId != entity.ShipId) ||
                (oldEntity.SaleId != entity.SaleId) ||
                (oldEntity.Trip != entity.Trip) ||
                (oldEntity.SealNo != entity.SealNo) ||
                (oldEntity.ContainerNo != entity.ContainerNo) ||
                (oldEntity.Notes != entity.Notes) ||
                (oldEntity.StartShip != entity.StartShip) ||
                (oldEntity.ClosingDate != entity.ClosingDate))
                {
                    expense.ForEach(x =>
                    {
                        var newExpense = new Expense();
                        newExpense.CopyPropFrom(x);
                        newExpense.Id = 0;
                        newExpense.StatusId = 1;
                        newExpense.RequestChangeId = x.Id;
                        db.Add(newExpense);
                        x.ShipId = entity.ShipId;
                        x.SaleId = entity.SaleId;
                        x.Trip = entity.Trip;
                        x.SealNo = entity.SealNo;
                        x.ContainerNo = entity.ContainerNo;
                        x.Notes = entity.Notes;
                        if (x.JourneyId == 12114 || x.JourneyId == 16001)
                        {
                            x.StartShip = entity.ClosingDate;
                        }
                        else
                        {
                            x.StartShip = entity.StartShip;
                        }
                    });
                }
            }
            if (disableTrigger)
            {
                db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER ALL ON Transportation");
            }
            await db.SaveChangesAsync();
            if (disableTrigger)
            {
                db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER ALL ON Transportation");
            }
            else
            {
                await db.Entry(entity).ReloadAsync();
            }
            return entity;
        }

        private void DisableTrigger(PatchUpdate patch, Transportation entity, string action)
        {
            if (patch.Changes.Any(x => x.Field == nameof(entity.BetAmount) || x.Field == nameof(entity.IsBet)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_BetAmount ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.ReturnId)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_BetFee ON Transportation");
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_IsSplitBill ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.ClosingId)
            || x.Field == nameof(entity.BossId)
            || x.Field == nameof(entity.ContainerTypeId)
            || x.Field == nameof(entity.ReceivedId)
            || x.Field == nameof(entity.ClosingDate)
            || x.Field == nameof(entity.IsClampingFee)
            || x.Field == nameof(entity.IsClosingCustomer)
            || x.Field == nameof(entity.IsEmptyCombination)
            || x.Field == nameof(entity.ClosingUnitPrice)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_ClosingUnitPrice ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.StartShip)
            && (x.Field == nameof(entity.BrandShipId)
            || x.Field == nameof(entity.IsEmptyCombination)
            || x.Field == nameof(entity.IsClosingCustomer))))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_CombinationFee ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.ContainerTypeId)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_Cont20_40 ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.DemDate)
            || x.Field == nameof(entity.ReturnDate)
            || x.Field == nameof(entity.ShipDate)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_Dem ON Transportation");
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_DemDate ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.ContainerTypeId)
            || x.Field == nameof(entity.IsLanding)
            || x.Field == nameof(entity.PortLoadingId)
            || x.Field == nameof(entity.ClosingDate)
            || x.Field == nameof(entity.LandingFee)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_LandingFee ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.ContainerTypeId)
            || x.Field == nameof(entity.PickupEmptyId)
            || x.Field == nameof(entity.ClosingDate)
            || x.Field == nameof(entity.IsEmptyLift)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_LiftFee ON Transportation");
            }
            if (patch.Changes.Any(x => (x.Field == nameof(entity.ContainerTypeId)
            || x.Field == nameof(entity.IsClosingEmptyFee)
            || x.Field == nameof(entity.ReturnEmptyId)
            || x.Field == nameof(entity.ReturnClosingFee)) && x.Field == nameof(entity.ShipDate)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_ReturnClosingFee ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.SplitBill)
            || x.Field == nameof(entity.ShipDate)
            || x.Field == nameof(entity.ReturnDate)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_ReturnDate ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.EmptyCombinationId)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_ReturnEmptyId ON Transportation");
            }
            if (patch.Changes.Any(x => (x.Field == nameof(entity.ContainerTypeId)
            || x.Field == nameof(entity.IsLiftFee)
            || x.Field == nameof(entity.PortLiftId)
            || x.Field == nameof(entity.ReturnLiftFee)) && x.Field == nameof(entity.ShipDate)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_ReturnLiftFee ON Transportation");
            }
            if (patch.Changes.Any(x => (x.Field == nameof(entity.ReturnVendorId)
            || x.Field == nameof(entity.BossId)
            || x.Field == nameof(entity.ContainerTypeId)
            || x.Field == nameof(entity.IsClampingReturnFee)
            || x.Field == nameof(entity.ReturnId)) && x.Field == nameof(entity.ReturnDate)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_ReturnUnitPrice ON Transportation");
            }
            if (patch.Changes.Any(x => (x.Field == nameof(entity.BrandShipId)
            || x.Field == nameof(entity.LevelId)) && x.Field == nameof(entity.StartShip)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_ReturnVs ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.LeftDate)
           || x.Field == nameof(entity.ClosingCont)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_ShellDate ON Transportation");
            }
            if (patch.Changes.Any(x => (x.Field == nameof(entity.ContainerTypeId)
            || x.Field == nameof(entity.LineId)
            || x.Field == nameof(entity.BrandShipId)) && x.Field == nameof(entity.StartShip)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_ShipUnitPrice ON Transportation");
            }
            if (patch.Changes.Any(x => (x.Field == nameof(entity.ContainerTypeId)
            || x.Field == nameof(entity.LineId)
            || x.Field == nameof(entity.BrandShipId)) && x.Field == nameof(entity.StartShip)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_ShipUnitPrice ON Transportation");
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.BookingId)))
            {
                db.Transportation.FromSqlInterpolated($"{action} TRIGGER tr_Transportation_UpdateBooking ON Transportation");
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
                var deleteExpense = $"delete from [{typeof(Expense).Name}] where TransportationId in ({string.Join(",", ids)}) delete from [{typeof(Transportation).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteExpense);
                var deleteRevenue = $"delete from [{typeof(Revenue).Name}] where TransportationId in ({string.Join(",", ids)}) delete from [{typeof(Transportation).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteRevenue);
                var sql1 = $@"select * from [{typeof(TransportationPlan).Name}] where Id in ({string.Join(",", data.Select(x => x.TransportationPlanId).ToList())})";
                var data1 = await db.Set<TransportationPlan>().FromSqlRaw(sql1).ToListAsync();
                data1.ForEach(async x =>
                {
                    await _taskService.SendMessageAllUser(new WebSocketResponse<TransportationPlan>
                    {
                        EntityId = _entitySvc.GetEntity(typeof(TransportationPlan).Name).Id,
                        Data = x
                    });
                });
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
                    ,CollectOnBehaftFee as CollectOnBehaftFee, CollectOnBehaftFeeCheck, CollectOnBehaftFeeUpload
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
                    from Transportation t
                    left join Vendor b on b.Id = t.BossId
                    left join Location r on r.Id = t.ReceivedId
                    left join Location pi on pi.Id = t.PickupEmptyId
                    left join Location po on po.Id = t.PortLoadingId";
            if (transportations.FirstOrDefault().CheckFeeHistoryId != null)
            {
                sql += $" where t.CheckFeeHistoryId = { transportations.FirstOrDefault().CheckFeeHistoryId }";
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
                var seal = item["SealNo"];
                var sealUpload = item["SealCheckUpload"];
                worksheet.Cell("E" + start).SetValue(seal is null ? null : seal.ToString());
                if (seal != sealUpload)
                {
                    worksheet.Cell("E" + start).Style.Font.FontColor = XLColor.Red;
                }
                worksheet.Cell("F" + start).SetValue(item["Cont20"] is null ? default(decimal) : decimal.Parse(item["Cont20"].ToString()));
                worksheet.Cell("G" + start).SetValue(item["Cont40"] is null ? default(decimal) : decimal.Parse(item["Cont40"].ToString()));
                worksheet.Cell("H" + start).SetValue(item["Received"] is null ? null : item["Received"].ToString().DecodeSpecialChar());
                var pick = item["PickupEmpty"];
                var pickUpload = item["PickupEmptyUpload"];
                worksheet.Cell("I" + start).SetValue(pick is null ? null : pick.ToString().DecodeSpecialChar());
                if (pick != pickUpload)
                {
                    worksheet.Cell("I" + start).Style.Font.FontColor = XLColor.Red;
                }
                var port = item["PortLoading"];
                var portUpload = item["PortLoadingUpload"];
                worksheet.Cell("J" + start).SetValue(port is null ? null : port.ToString().DecodeSpecialChar());
                if (port != portUpload)
                {
                    worksheet.Cell("J" + start).Style.Font.FontColor = XLColor.Red;
                }
                worksheet.Cell("K" + start).SetValue(item["LiftFee"] is null ? default(decimal) : decimal.Parse(item["LiftFee"].ToString()));
                worksheet.Cell("K" + start).Style.NumberFormat.Format = "#,##";
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

                var closingPercent = item["ClosingPercent"];
                var closingPercentUpload = item["ClosingPercentUpload"];
                worksheet.Cell("W" + start).SetValue(closingPercent is null ? default(decimal) : decimal.Parse(closingPercent.ToString()));
                worksheet.Cell("W" + start).Style.NumberFormat.Format = "#,##";
                if (closingPercent != closingPercentUpload)
                {
                    worksheet.Cell("W" + start).Style.Font.FontColor = XLColor.Red;
                }
                var closingCombinationUnitPrice = item["ClosingCombinationUnitPrice"];
                var closingCombinationUnitPriceUpload = item["ClosingCombinationUnitPriceUpload"];
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
        public async Task<List<Transportation>> CheckFee([FromServices] IWebHostEnvironment host, List<IFormFile> fileCheckFee, int type)
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
            var vendor = list.Where(x => x.Vendor != null).Select(x => x.Vendor.ToLower()).Distinct().ToList();
            var containerNo = list.Where(x => x.ContainerNo != null).Select(x => x.ContainerNo.ToLower()).Distinct().ToList();
            var closingDay = list.Where(x => x.ClosingDate != null).Select(x => x.ClosingDate.Value.Date).Distinct().ToList();
            var vendorDB = await db.Vendor.Where(x => vendor.Contains(x.Name.ToLower())).ToListAsync();
            var first = vendorDB.FirstOrDefault();
            if (first is null)
            {
                throw new ApiException("Tên công ty không trùng với hệ thống!") { StatusCode = HttpStatusCode.BadRequest };
            }
            if (type == 1)
            {
                var qr = db.Transportation.Where(x =>
               containerNo.Contains(x.ContainerNo.ToLower().Trim())
               && closingDay.Contains(x.ClosingDate.Value.Date) && x.ClosingId == first.Id).OrderBy(x => x.ClosingDate).AsQueryable();
                var transportations = await qr.ToListAsync();
                var lastHis = new CheckFeeHistory()
                {
                    ClosingId = first.Id,
                    FromDate = transportations.FirstOrDefault().ClosingDate,
                    ToDate = transportations.LastOrDefault().ClosingDate,
                    TypeId = type
                };
                SetAuditInfo(lastHis);
                db.Add(lastHis);
                await db.SaveChangesAsync();
                var checks = list.Select(x =>
                {
                    var tran = transportations.FirstOrDefault(y => y.ContainerNo.ToLower().Trim() == x.ContainerNo.ToLower()
                    && y.ClosingDate.Value.Date == x.ClosingDate.Value.Date);
                    if (tran != null)
                    {
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
                            ClosingId = first.Id,
                            ReceivedCheck = x.Received,
                            ClosingDateCheck = x.ClosingDate,
                            SealCheck = x.SealNo,
                            BossCheck = x.Boss,
                            ContainerNoCheck = x.ContainerNo,
                            Cont20Check = x.Cont20,
                            Cont40Check = x.Cont40,
                            PickupEmptyCheck = x.PickupEmpty,
                            PortLoadingCheck = x.PortLoading,
                            ClosingPercentCheck = x.ClosingPercentCheck,
                            LiftFeeCheck = x.LiftFee,
                            LandingFeeCheck = x.LandingFee,
                            CollectOnBehaftInvoinceNoFeeCheck = x.FeeVat1 + x.FeeVat2 + x.FeeVat3,
                            CollectOnBehaftFeeCheck = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5 + x.Fee6,
                            CollectOnSupPriceCheck = x.CollectOnSupPrice,
                            TotalPriceAfterTaxCheck = x.TotalPriceAfterTax,
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
               containerNo.Contains(x.ContainerNo.ToLower().Trim())
               && closingDay.Contains(x.ReturnDate.Value.Date)).AsQueryable();
                var transportations = await qr.ToListAsync();
                var checks = list.Select(x =>
                {
                    var tran = transportations.FirstOrDefault(y => y.ContainerNo.ToLower().Trim() == x.ContainerNo.ToLower()
                    && y.ReturnDate.Value.Date == x.ClosingDate.Value.Date);
                    if (tran != null)
                    {
                        tran.ReceivedReturnCheck = x.Received;
                        tran.ClosingDateReturnCheck = x.ClosingDate;
                        tran.SealReturnCheck = x.SealNo;
                        tran.ContainerNoReturnCheck = x.ContainerNo;
                        tran.BossReturnCheck = x.Boss;
                        tran.Cont20ReturnCheck = x.Cont20;
                        tran.Cont40ReturnCheck = x.Cont40;
                        //tran.ClosingPercentReturnCheck = x.ClosingPercentCheck;
                        tran.PickupEmptyReturnCheck = x.PickupEmpty;
                        tran.PortLoadingReturnCheck = x.PortLoading;
                        tran.LiftFeeReturnCheck = x.LiftFee;
                        tran.LandingFeeReturnCheck = x.LandingFee;
                        tran.CollectOnBehaftInvoinceNoFeeReturnCheck = x.FeeVat1 + x.FeeVat2 + x.FeeVat3;
                        tran.CollectOnBehaftFeeReturnCheck = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5;
                        tran.CollectOnSupPriceReturnCheck = x.CollectOnSupPrice;
                        tran.TotalPriceAfterTaxReturnCheck = x.TotalPriceAfterTax;
                        if (tran.IsSeftPaymentReturn || tran.IsLiftFee)
                        {
                            tran.ReturnLiftFee = 0;
                        }
                        if (tran.IsSeftPaymentLandReturn || tran.IsClosingEmptyFee)
                        {
                            tran.ReturnClosingFee = 0;
                        }
                    }
                    else
                    {
                        tran = new Transportation()
                        {
                            ReceivedReturnCheck = x.Received,
                            ClosingDateReturnCheck = x.ClosingDate,
                            SealReturnCheck = x.SealNo,
                            BossReturnCheck = x.Boss,
                            ContainerNoReturnCheck = x.ContainerNo,
                            Cont20ReturnCheck = x.Cont20,
                            Cont40ReturnCheck = x.Cont40,
                            PickupEmptyReturnCheck = x.PickupEmpty,
                            PortLoadingReturnCheck = x.PortLoading,
                            //ClosingPercentReturnCheck = x.ClosingPercentCheck,
                            LiftFeeReturnCheck = x.LiftFee,
                            LandingFeeReturnCheck = x.LandingFee,
                            CollectOnBehaftInvoinceNoFeeReturnCheck = x.FeeVat1 + x.FeeVat2 + x.FeeVat3,
                            CollectOnBehaftFeeReturnCheck = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5,
                            CollectOnSupPriceReturnCheck = x.CollectOnSupPrice,
                            TotalPriceAfterTaxReturnCheck = x.TotalPriceAfterTax,
                        };
                    }
                    return tran;
                }).ToList();
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
                $" where ShipId = '{entity.ShipId}' and (BrandShipId = '{entity.BrandShipId}' or '{entity.BrandShipId}' = '') and Trip = '{entity.Trip}' and RouteId in ({entity.RouteIds.Combine()})";
            db.Transportation.FromSqlInterpolated($"DISABLE TRIGGER tr_Transportation_UpdateTeus ON Transportation");
            await db.Database.ExecuteSqlRawAsync(cmd);
            db.Transportation.FromSqlInterpolated($"ENABLE TRIGGER tr_Transportation_UpdateTeus ON Transportation");
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
            else if (RoleIds.Contains(25) || RoleIds.Contains(22))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25044))";
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
                                 where 1 = 1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")} ";

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
                tran.IsRequestUnLockExploit = true;
                tran.ReasonUnLockExploit = transportation.ReasonUnLockExploit;
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
                tran.IsRequestUnLockAccountant = true;
                tran.ReasonUnLockAccountant = transportation.ReasonUnLockAccountant;
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
                tran.IsRequestUnLockAll = true;
                tran.ReasonUnLockAll = transportation.ReasonUnLockAll;
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
                tran.IsRequestUnLockShip = true;
                tran.ReasonUnLockShip = transportation.ReasonUnLockShip;
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

        [HttpPost("api/Transportation/ExportTransportationAndRevenue")]
        public async Task<string> ExportTransportationAndRevenue([FromBody] List<int> tranIds)
        {
            var trans = await db.Transportation.Where(x => tranIds.Contains(x.Id)).ToListAsync();
            Workbook workbook = new Workbook();
            Worksheet worksheet = workbook.Worksheets[0];
            worksheet.Cells["A1"].PutValue($"STT");
            SetBackgroundColor(workbook, "A1");
            SetBorder(workbook, "A2");
            worksheet.Cells.Merge(0, 0, 2, 1);
            worksheet.Cells["B1"].PutValue($"Khóa hệ thống");
            SetBackgroundColor(workbook, "B1");
            SetBorder(workbook, "B2");
            worksheet.Cells["C1"].PutValue($"Tháng");
            worksheet.Cells.Merge(0, 1, 2, 1);
            SetBackgroundColor(workbook, "C1");
            SetBorder(workbook, "C2");
            worksheet.Cells["D1"].PutValue($"Năm");
            worksheet.Cells.Merge(0, 2, 2, 1);
            SetBackgroundColor(workbook, "D1");
            SetBorder(workbook, "D2");
            worksheet.Cells["E1"].PutValue($"List xuất");
            worksheet.Cells.Merge(0, 3, 2, 1);
            SetBackgroundColor(workbook, "E1");
            SetBorder(workbook, "E2");
            worksheet.Cells["F1"].PutValue($"Tuyến vận chuyển");
            worksheet.Cells.Merge(0, 4, 2, 1);
            SetBackgroundColor(workbook, "F1");
            SetBorder(workbook, "F2");
            worksheet.Cells["G1"].PutValue($"SOC");
            worksheet.Cells.Merge(0, 5, 2, 1);
            SetBackgroundColor(workbook, "G1");
            SetBorder(workbook, "G2");
            worksheet.Cells["H1"].PutValue($"Tên tàu");
            worksheet.Cells.Merge(0, 6, 2, 1);
            SetBackgroundColor(workbook, "H1");
            SetBorder(workbook, "H2");
            worksheet.Cells["I1"].PutValue($"Số chuyến");
            worksheet.Cells.Merge(0, 7, 2, 1);
            SetBackgroundColor(workbook, "I1");
            SetBorder(workbook, "I2");
            worksheet.Cells["J1"].PutValue($"Ngày đóng hàng");
            worksheet.Cells.Merge(0, 8, 2, 1);
            SetBorder(workbook, "J2");
            SetBackgroundColor(workbook, "J1");
            worksheet.Cells["K1"].PutValue($"Ngày tàu chạy");
            worksheet.Cells.Merge(0, 9, 2, 1);
            SetBackgroundColor(workbook, "K1");
            SetBorder(workbook, "K2");
            worksheet.Cells["L1"].PutValue($"Loại container");
            worksheet.Cells.Merge(0, 10, 2, 1);
            SetBackgroundColor(workbook, "L1");
            SetBorder(workbook, "L2");
            worksheet.Cells["M1"].PutValue($"Số cont");
            worksheet.Cells.Merge(0, 11, 2, 1);
            SetBackgroundColor(workbook, "M1");
            SetBorder(workbook, "M2");
            worksheet.Cells["N1"].PutValue($"Số seal");
            worksheet.Cells.Merge(0, 12, 2, 1);
            SetBackgroundColor(workbook, "N1");
            SetBorder(workbook, "N2");
            worksheet.Cells["O1"].PutValue($"Chủ hàng");
            worksheet.Cells.Merge(0, 13, 2, 1);
            SetBackgroundColor(workbook, "O1");
            SetBorder(workbook, "O2");
            worksheet.Cells["P1"].PutValue($"Nhân viên bán hàng");
            worksheet.Cells.Merge(0, 14, 2, 1);
            SetBackgroundColor(workbook, "P1");
            SetBorder(workbook, "P2");
            worksheet.Cells["Q1"].PutValue($"Vật tư hàng hóa");
            worksheet.Cells.Merge(0, 15, 2, 1);
            SetBackgroundColor(workbook, "Q1");
            SetBorder(workbook, "Q2");
            worksheet.Cells["R1"].PutValue($"Cont 20");
            worksheet.Cells.Merge(0, 16, 2, 1);
            SetBackgroundColor(workbook, "R1");
            SetBorder(workbook, "R2");
            worksheet.Cells["S1"].PutValue($"Cont 40");
            worksheet.Cells.Merge(0, 17, 2, 1);
            SetBackgroundColor(workbook, "S1");
            SetBorder(workbook, "S2");
            worksheet.Cells["T1"].PutValue($"Trọng lượng");
            worksheet.Cells.Merge(0, 18, 2, 1);
            SetBackgroundColor(workbook, "T1");
            SetBorder(workbook, "T2");
            worksheet.Cells["U1"].PutValue($"Địa điểm nhận hàng");
            worksheet.Cells.Merge(0, 19, 2, 1);
            SetBackgroundColor(workbook, "U1");
            SetBorder(workbook, "U2");
            worksheet.Cells["V1"].PutValue($"Phát sinh đóng hàng");
            worksheet.Cells.Merge(0, 20, 2, 1);
            SetBackgroundColor(workbook, "V1");
            SetBorder(workbook, "V2");
            worksheet.Cells["W1"].PutValue($"Phí bảo hiểm");
            worksheet.Cells.Merge(0, 21, 2, 1);
            SetBackgroundColor(workbook, "W1");
            SetBorder(workbook, "W2");
            worksheet.Cells["X1"].PutValue($"Ngày tàu cập");
            worksheet.Cells.Merge(0, 22, 2, 1);
            SetBackgroundColor(workbook, "X1");
            SetBorder(workbook, "X2");
            worksheet.Cells["Y1"].PutValue($"Ngày trả hàng");
            worksheet.Cells.Merge(0, 23, 2, 1);
            SetBackgroundColor(workbook, "Y1");
            SetBorder(workbook, "Y2");
            worksheet.Cells["Z1"].PutValue($"Địa điểm trả hàng");
            worksheet.Cells.Merge(0, 24, 2, 1);
            SetBackgroundColor(workbook, "Z1");
            SetBorder(workbook, "Z2");
            worksheet.Cells["AA1"].PutValue($"Phát sinh trả hàng");
            worksheet.Cells.Merge(0, 25, 2, 1);
            SetBackgroundColor(workbook, "AA1");
            SetBorder(workbook, "AA2");
            worksheet.Cells["AB1"].PutValue($"Khóa khai thác");
            worksheet.Cells.Merge(0, 26, 2, 1);
            SetBackgroundColor(workbook, "AB1");
            SetBorder(workbook, "AB2");
            worksheet.Cells["AC1"].PutValue($"Khóa kế toán");
            worksheet.Cells.Merge(0, 27, 2, 1);
            SetBackgroundColor(workbook, "AC1");
            SetBorder(workbook, "AC2");
            worksheet.Cells["AD1"].PutValue($"Số bảng kê");
            worksheet.Cells.Merge(0, 28, 2, 1);
            SetBackgroundColor(workbook, "AD1");
            SetBorder(workbook, "AD2");
            worksheet.Cells["AE1"].PutValue($"Ngày bảng kê");
            worksheet.Cells.Merge(0, 29, 2, 1);
            SetBackgroundColor(workbook, "AE1");
            SetBorder(workbook, "AE2");
            worksheet.Cells["AF1"].PutValue($"Số hóa đơn");
            worksheet.Cells.Merge(0, 30, 2, 1);
            SetBackgroundColor(workbook, "AF1");
            SetBorder(workbook, "AF2");
            worksheet.Cells["AG1"].PutValue($"Ngày hóa đơn");
            worksheet.Cells.Merge(0, 31, 2, 1);
            SetBackgroundColor(workbook, "AG1");
            SetBorder(workbook, "AG2");
            worksheet.Cells["AH1"].PutValue($"% GTGT");
            worksheet.Cells.Merge(0, 32, 2, 1);
            SetBackgroundColor(workbook, "AH1");
            SetBorder(workbook, "AH2");
            worksheet.Cells["AI1"].PutValue($"Đơn giá (chưa VAT)");
            worksheet.Cells.Merge(0, 33, 2, 1);
            SetBackgroundColor(workbook, "AI1");
            SetBorder(workbook, "AI2");
            worksheet.Cells["AJ1"].PutValue($"Đơn giá (có VAT)");
            worksheet.Cells.Merge(0, 34, 2, 1);
            SetBackgroundColor(workbook, "AJ1");
            SetBorder(workbook, "AJ2");
            worksheet.Cells["AK1"].PutValue($"Thu khác");
            worksheet.Cells.Merge(0, 35, 2, 1);
            SetBackgroundColor(workbook, "AK1");
            SetBorder(workbook, "AK2");
            worksheet.Cells["AL1"].PutValue($"Thu chi hộ");
            worksheet.Cells.Merge(0, 36, 2, 1);
            SetBackgroundColor(workbook, "AL1");
            SetBorder(workbook, "AL2");
            worksheet.Cells["AM1"].PutValue($"Ghi chú doanh thu");
            worksheet.Cells.Merge(0, 37, 2, 1);
            SetBackgroundColor(workbook, "AM1");
            SetBorder(workbook, "AM2");
            worksheet.Cells["AN1"].PutValue($"Giá trị (chưa VAT)");
            worksheet.Cells.Merge(0, 38, 2, 1);
            SetBackgroundColor(workbook, "AN1");
            SetBorder(workbook, "AN2");
            worksheet.Cells["AO1"].PutValue($"Thuế GTGT");
            worksheet.Cells.Merge(0, 39, 2, 1);
            SetBackgroundColor(workbook, "AO1");
            SetBorder(workbook, "AO2");
            worksheet.Cells["AP1"].PutValue($"Tổng giá trị");
            worksheet.Cells.Merge(0, 40, 2, 1);
            SetBackgroundColor(workbook, "AP1");
            SetBorder(workbook, "AP2");
            worksheet.Cells["AQ1"].PutValue($"Đơn vị xuất hóa đơn");
            worksheet.Cells.Merge(0, 41, 2, 1);
            SetBackgroundColor(workbook, "AQ1");
            SetBorder(workbook, "AQ2");
            worksheet.Cells["AR1"].PutValue($"Ghi chú");
            worksheet.Cells.Merge(0, 42, 2, 1);
            SetBackgroundColor(workbook, "AR1");
            SetBorder(workbook, "AR2");
            worksheet.Cells["AS1"].PutValue($"Thanh toán");
            worksheet.Cells.Merge(0, 43, 2, 1);
            SetBackgroundColor(workbook, "AS1");
            SetBorder(workbook, "AS2");
            worksheet.Cells.Merge(0, 44, 2, 1);
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
            [dbo].Get_Revenue(t.Id, 'LotNo') as LotNo,
            [dbo].Get_Revenue(t.Id, 'LotDate') as LotDate,
            [dbo].Get_Revenue(t.Id, 'InvoinceNo') as InvoinceNo,
            [dbo].Get_Revenue(t.Id, 'InvoinceDate') as InvoinceDate,
            [dbo].Get_Revenue(t.Id, 'Vat') as Vat,
            t.UnitPriceBeforeTax,
            t.UnitPriceAfterTax,
            t.ReceivedPrice,
            t.CollectOnBehaftPrice,
            [dbo].Get_Revenue(t.Id, 'NotePayment') as NotePayment,
            t.TotalPriceBeforTax,
            t.VatPrice,
            t.TotalPrice,
            [dbo].Get_Revenue(t.Id, 'VendorVatId') as VendorVat,
            [dbo].Get_Revenue(t.Id, 'Note') as Note,
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
                worksheet.Cells["A" + start].PutValue(start - 2);
                worksheet.Cells["B" + start].PutValue(item["IsLocked"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cells["C" + start].PutValue(item["MonthText"] is null ? "" : item["MonthText"].ToString());
                worksheet.Cells["D" + start].PutValue(item["YearText"] is null ? "" : item["YearText"].ToString());
                worksheet.Cells["E" + start].PutValue(item["ExportList"] is null ? "" : item["ExportList"].ToString());
                worksheet.Cells["F" + start].PutValue(item["Route"] is null ? "" : item["Route"].ToString());
                worksheet.Cells["G" + start].PutValue(item["Soc"] is null ? "" : item["Soc"].ToString());
                worksheet.Cells["H" + start].PutValue(item["Ship"] is null ? "" : item["Ship"].ToString());
                worksheet.Cells["I" + start].PutValue(item["Trip"] is null ? "" : item["Trip"].ToString());
                worksheet.Cells["J" + start].PutValue(item["ClosingDate"] is null ? "" : DateTime.Parse(item["ClosingDate"].ToString()).ToString("dd/MM/yyyy"));
                worksheet.Cells["K" + start].PutValue(item["StartShip"] is null ? "" : DateTime.Parse(item["StartShip"].ToString()).ToString("dd/MM/yyyy"));
                worksheet.Cells["L" + start].PutValue(item["ContainerType"] is null ? "" : item["ContainerType"].ToString());
                worksheet.Cells["M" + start].PutValue(item["ContainerNo"] is null ? "" : item["ContainerNo"].ToString());
                worksheet.Cells["N" + start].PutValue(item["SealNo"] is null ? "" : item["SealNo"].ToString());
                worksheet.Cells["O" + start].PutValue(item["Boss"] is null ? "" : item["Boss"].ToString());
                worksheet.Cells["P" + start].PutValue(item["User"] is null ? "" : item["User"].ToString());
                worksheet.Cells["Q" + start].PutValue(item["Commodity"] is null ? "" : item["Commodity"].ToString());
                worksheet.Cells["R" + start].PutValue(item["Cont20"] is null ? "" : item["Cont20"].ToString());
                worksheet.Cells["S" + start].PutValue(item["Cont40"] is null ? "" : item["Cont40"].ToString());
                worksheet.Cells["T" + start].PutValue(item["Weight"] is null ? "0" : item["Weight"].ToString());
                worksheet.Cells["U" + start].PutValue(item["Received"] is null ? "0" : item["Received"].ToString());
                worksheet.Cells["V" + start].PutValue(item["FreeText2"] is null ? "0" : item["FreeText2"].ToString());
                worksheet.Cells["W" + start].PutValue(item["InsuranceFee"] is null ? "0" : $"{decimal.Parse(item["InsuranceFee"].ToString()):n0}");
                worksheet.Cells["X" + start].PutValue(item["ShipDate"] is null ? "" : DateTime.Parse(item["ShipDate"].ToString()).ToString("dd/MM/yyyy"));
                worksheet.Cells["Y" + start].PutValue(item["ReturnDate"] is null ? "" : DateTime.Parse(item["ReturnDate"].ToString()).ToString("dd/MM/yyyy"));
                worksheet.Cells["Z" + start].PutValue(item["Return"] is null ? "" : item["Return"].ToString());
                worksheet.Cells["AA" + start].PutValue(item["FreeText3"] is null ? "" : item["FreeText3"].ToString());
                worksheet.Cells["AB" + start].PutValue(item["IsKt"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cells["AC" + start].PutValue(item["IsSubmit"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cells["AD" + start].PutValue(item["LotNo"] is null ? "" : item["LotNo"].ToString());
                worksheet.Cells["AE" + start].PutValue(item["LotDate"] is null ? "" : item["LotDate"].ToString());
                worksheet.Cells["AF" + start].PutValue(item["InvoinceNo"] is null ? "" : item["InvoinceNo"].ToString());
                worksheet.Cells["AG" + start].PutValue(item["InvoinceDate"] is null ? "" : item["InvoinceDate"].ToString());
                worksheet.Cells["AH" + start].PutValue(item["Vat"] is null ? "" : item["Vat"].ToString());
                worksheet.Cells["AI" + start].PutValue(item["UnitPriceBeforeTax"] is null ? "0" : $"{decimal.Parse(item["UnitPriceBeforeTax"].ToString()):n0}");
                worksheet.Cells["AJ" + start].PutValue(item["UnitPriceAfterTax"] is null ? "0" : $"{decimal.Parse(item["UnitPriceAfterTax"].ToString()):n0}");
                worksheet.Cells["AK" + start].PutValue(item["ReceivedPrice"] is null ? "0" : $"{decimal.Parse(item["ReceivedPrice"].ToString()):n0}");
                worksheet.Cells["AL" + start].PutValue(item["CollectOnBehaftPrice"] is null ? "0" : $"{decimal.Parse(item["CollectOnBehaftPrice"].ToString()):n0}");
                worksheet.Cells["AM" + start].PutValue(item["NotePayment"] is null ? "" : item["NotePayment"].ToString());
                worksheet.Cells["AN" + start].PutValue(item["TotalPriceBeforTax"] is null ? "0" : $"{decimal.Parse(item["TotalPriceBeforTax"].ToString()):n0}");
                worksheet.Cells["AO" + start].PutValue(item["VatPrice"] is null ? "0" : $"{decimal.Parse(item["VatPrice"].ToString()):n0}");
                worksheet.Cells["AP" + start].PutValue(item["TotalPrice"] is null ? "0" : $"{decimal.Parse(item["TotalPrice"].ToString()):n0}");
                worksheet.Cells["AQ" + start].PutValue(item["VendorVat"] is null ? "" : item["VendorVat"].ToString());
                worksheet.Cells["AR" + start].PutValue(item["Note"] is null ? "" : item["Note"].ToString());
                worksheet.Cells["AS" + start].PutValue(item["IsPayment"].ToString().Contains("False") ? "Không" : "Có");
                start++;
            }
            var url = $"Transportation.xlsx";
            workbook.Save($"wwwroot\\excel\\Download\\{url}", new OoxmlSaveOptions(SaveFormat.Xlsx));
            return url;
        }
    }
}
