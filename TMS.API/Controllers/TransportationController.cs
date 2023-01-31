using Aspose.Cells;
using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using MimeKit;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using Windows.Data.Html;
using static Org.BouncyCastle.Math.EC.ECCurve;
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
            Workbook workbook = new Workbook();
            Worksheet worksheet = workbook.Worksheets[0];
            var closingId = transportations.FirstOrDefault().ClosingId;
            var closing = await db.Vendor.FirstOrDefaultAsync(x => x.Id == closingId);
            worksheet.Cells["A1"].PutValue(closing.Name);
            worksheet.Cells["A3"].PutValue("Địa chỉ");
            worksheet.Cells["A4"].PutValue("MST");
            worksheet.Cells["A6"].PutValue($"BẢNG KÊ ĐỐI CHIẾU CƯỚC VC XE TỪ NGÀY {transportations.FirstOrDefault().ClosingDate} ĐẾN {transportations.LastOrDefault().ClosingDate}");
            worksheet.Cells["A7"].PutValue($"Kính gửi: Công Ty Cổ Phần Logistics Đông Á");
            worksheet.Cells["A9"].PutValue($"STT");
            SetBackgroundColor(workbook, "A9");
            SetBorder(workbook, "A10");
            worksheet.Cells.Merge(8, 0, 2, 1);
            worksheet.Cells["B9"].PutValue($"Ngày đóng hàng");
            SetBackgroundColor(workbook, "B9");
            SetBorder(workbook, "B10");
            worksheet.Cells["C9"].PutValue($"Chủ hàng");
            worksheet.Cells.Merge(8, 1, 2, 1);
            SetBackgroundColor(workbook, "C9");
            SetBorder(workbook, "C10");
            worksheet.Cells["D9"].PutValue($"Số cont");
            worksheet.Cells.Merge(8, 2, 2, 1);
            SetBackgroundColor(workbook, "D9");
            SetBorder(workbook, "D10");
            worksheet.Cells["E9"].PutValue($"Số seal");
            worksheet.Cells.Merge(8, 3, 2, 1);
            SetBackgroundColor(workbook, "E9");
            SetBorder(workbook, "E10");
            worksheet.Cells["F9"].PutValue($"Cont 20");
            worksheet.Cells.Merge(8, 4, 2, 1);
            SetBackgroundColor(workbook, "F9");
            SetBorder(workbook, "F10");
            worksheet.Cells["G9"].PutValue($"Cont 40");
            worksheet.Cells.Merge(8, 5, 2, 1);
            SetBackgroundColor(workbook, "G9");
            SetBorder(workbook, "G10");
            worksheet.Cells["H9"].PutValue($"Địa điểm nhận hàng");
            worksheet.Cells.Merge(8, 6, 2, 1);
            SetBackgroundColor(workbook, "H9");
            SetBorder(workbook, "H10");
            worksheet.Cells["I9"].PutValue($"Nơi lấy rỗng");
            worksheet.Cells.Merge(8, 7, 2, 1);
            SetBackgroundColor(workbook, "I9");
            SetBorder(workbook, "I10");
            worksheet.Cells["J9"].PutValue($"Cảng hạ hàng");
            worksheet.Cells.Merge(8, 8, 2, 1);
            SetBorder(workbook, "J10");
            SetBackgroundColor(workbook, "J9");
            worksheet.Cells["K9"].PutValue($"Phí nâng");
            worksheet.Cells.Merge(8, 9, 2, 1);
            SetBackgroundColor(workbook, "K9");
            SetBorder(workbook, "K10");
            worksheet.Cells["L9"].PutValue($"Phí hạ");
            worksheet.Cells.Merge(8, 10, 2, 1);
            SetBackgroundColor(workbook, "L9");
            SetBorder(workbook, "L10");
            worksheet.Cells.Merge(8, 12, 1, 3);
            worksheet.Cells["M9"].PutValue($"Chi phí (có HĐ - VAT hiện hành)");
            worksheet.Cells.Merge(8, 11, 2, 1);
            SetAlign(workbook, "M9");
            SetBackgroundColor(workbook, "M9");
            SetBorder(workbook, "N9");
            SetBorder(workbook, "O9");
            worksheet.Cells.Merge(8, 15, 1, 6);
            worksheet.Cells["P9"].PutValue($"Chi phí (không HĐ)");
            worksheet.Cells.Merge(8, 21, 2, 1);
            SetBackgroundColor(workbook, "P9");
            SetAlign(workbook, "P9");
            SetBorder(workbook, "Q9");
            SetBorder(workbook, "R9");
            SetBorder(workbook, "S9");
            SetBorder(workbook, "T9");
            SetBorder(workbook, "U9");
            worksheet.Cells["V9"].PutValue($"Thu lại tiền nhà xe (lưu vỏ, lưu cont, sửa chữa…) nhập số âm");
            worksheet.Cells.Merge(8, 22, 2, 1);
            SetBackgroundColor(workbook, "V9");
            SetBorder(workbook, "V10");
            worksheet.Cells["W9"].PutValue($"% kết hợp");
            worksheet.Cells.Merge(8, 23, 2, 1);
            SetBackgroundColor(workbook, "W9");
            SetBorder(workbook, "W10");
            worksheet.Cells["X9"].PutValue($"Cước VC 10 % ");
            worksheet.Cells.Merge(8, 24, 2, 1);
            SetBackgroundColor(workbook, "X9");
            SetBorder(workbook, "X10");
            worksheet.Cells["Y9"].PutValue($"Cước VC 8 % ");
            worksheet.Cells.Merge(8, 25, 2, 1);
            SetBackgroundColor(workbook, "Y9");
            SetBorder(workbook, "Y10");
            worksheet.Cells["Z9"].PutValue($"Tổng Cước VC(theo thuế hiện hành");
            worksheet.Cells.Merge(8, 26, 2, 1);
            SetBackgroundColor(workbook, "Z9");
            SetBorder(workbook, "Z10");
            worksheet.Cells["AA9"].PutValue($"Ghi chú");
            SetBackgroundColor(workbook, "AA9");
            SetBorder(workbook, "AA10");
            worksheet.Cells["M10"].PutValue($"HĐ xuất cho ĐA");
            SetBackgroundColor(workbook, "M10");
            worksheet.Cells["N10"].PutValue($"HĐ xuất cho KH");
            SetBackgroundColor(workbook, "N10");
            worksheet.Cells["O10"].PutValue($"Phí vé cầu đường, cao tốc, trạm thu phí...");
            SetBackgroundColor(workbook, "O10");
            worksheet.Cells["P10"].PutValue($"Bốc xếp");
            SetBackgroundColor(workbook, "P10");
            worksheet.Cells["Q10"].PutValue($"Phí hạ xa (HP, SP, …)");
            SetBackgroundColor(workbook, "Q10");
            worksheet.Cells["R10"].PutValue($"2 kho, chuyển kho");
            SetBackgroundColor(workbook, "R10");
            worksheet.Cells["S10"].PutValue($"Neo xe/Cân");
            SetBackgroundColor(workbook, "S10");
            worksheet.Cells["T10"].PutValue($"CP phát sinh khác");
            SetBackgroundColor(workbook, "T10");
            worksheet.Cells["U10"].PutValue($"Chênh lệch hạ vỏ");
            SetBackgroundColor(workbook, "U10");
            var sql = @$"select ClosingDate, ClosingDateCheck, ClosingDateUpload
                    ,ClosingDate, ClosingDateCheck, ClosingDateUpload
                    ,b.Name as Boss, BossCheck, BossCheckUpload
                    ,ContainerNo, ContainerNoCheck, ContainerNoUpload
                    ,SealNo, SealCheck, SealCheckUpload
                    ,FORMAT(Cont20,'#,#') as Cont20, Cont20Check, Cont20CheckUpload
                    ,FORMAT(Cont40,'#,#') as Cont40, Cont40Check, Cont40CheckUpload
                    ,r.Description as Received, ReceivedCheck, ReceivedCheckUpload
                    ,FORMAT(CollectOnBehaftInvoinceNoFee,'#,#') as CollectOnBehaftInvoinceNoFee, CollectOnBehaftInvoinceNoFeeCheck, CollectOnBehaftInvoinceNoFeeUpload
                    ,FORMAT(CollectOnBehaftFee,'#,#') as CollectOnBehaftFee, CollectOnBehaftFeeCheck, CollectOnBehaftFeeUpload
                    ,pi.Description as PickupEmpty, PickupEmptyCheck, PickupEmptyUpload
                    ,po.Description as PortLoading, PortLoadingCheck, PortLoadingUpload
                    ,FORMAT(LiftFee,'#,#') as LiftFee, LiftFeeCheck, LiftFeeCheckUpload
                    ,FORMAT(LandingFee,'#,#') as LandingFee, LandingFeeCheck, LandingFeeUpload
                    ,FORMAT(CollectOnSupPrice,'#,#') as CollectOnSupPrice, CollectOnSupPriceCheck, CollectOnSupPriceUpload
                    ,FORMAT(ClosingPercent,'#,#') as ClosingPercent, ClosingPercentCheck, ClosingPercentUpload
                    ,FORMAT(ClosingCombinationUnitPrice,'#,#') as ClosingCombinationUnitPrice, ClosingCombinationUnitPriceCheck, ClosingCombinationUnitPriceUpload
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
            var start = 11;
            foreach (var item in data[0])
            {
                worksheet.Cells["A" + start].PutValue(start - 10);
                worksheet.Cells["B" + start].PutValue(item[nameof(Transportation.ClosingDate)]);
                worksheet.Cells["C" + start].PutValue(item["Boss"]);
                worksheet.Cells["D" + start].PutValue(item["ContainerNo"]);
                var seal = item["SealNo"];
                var sealUpload = item["SealCheckUpload"];
                worksheet.Cells["E" + start].PutValue(seal);
                if (seal != sealUpload)
                {
                    SetColor(workbook, "E" + start);
                }
                worksheet.Cells["F" + start].PutValue(item["Cont20"]);
                worksheet.Cells["G" + start].PutValue(item["Cont40"]);
                worksheet.Cells["H" + start].PutValue(item["Received"]);
                var pick = item["PickupEmpty"];
                var pickUpload = item["PickupEmptyUpload"];
                worksheet.Cells["I" + start].PutValue(pick);
                if (pick != pickUpload)
                {
                    SetColor(workbook, "I" + start);
                }
                var port = item["PortLoading"];
                var portUpload = item["PortLoadingUpload"];
                worksheet.Cells["J" + start].PutValue(item["PortLoading"]);
                if (port != portUpload)
                {
                    SetColor(workbook, "J" + start);
                }
                worksheet.Cells["K" + start].PutValue(item["LiftFee"]);
                worksheet.Cells["L" + start].PutValue(item["LandingFee"]);
                worksheet.Cells["O" + start].PutValue(item["CollectOnBehaftInvoinceNoFee"]);
                worksheet.Cells["P" + start].PutValue(item["CollectOnBehaftFee"]);
                var closingPercent = item["ClosingPercent"];
                var closingPercentUpload = item["ClosingPercentUpload"];
                worksheet.Cells["W" + start].PutValue(closingPercent);
                if (closingPercent != closingPercentUpload)
                {
                    SetColor(workbook, "W" + start);
                }
                var closingCombinationUnitPrice = item["ClosingCombinationUnitPrice"];
                var closingCombinationUnitPriceUpload = item["ClosingCombinationUnitPriceUpload"];
                worksheet.Cells["X" + start].PutValue(closingCombinationUnitPrice);
                if (closingCombinationUnitPrice != closingCombinationUnitPriceUpload)
                {
                    SetColor(workbook, "X" + start);
                }
                start++;
            }
            for (int i = 1; i < 25; i++)
            {
                worksheet.AutoFitColumn(i);
            }
            var url = $"BangKe{closing.Name}{transportations.FirstOrDefault().ClosingDate.Value.ToString("dd-MM-yyyy")}Den{transportations.LastOrDefault().ClosingDate.Value.ToString("dd-MM-yyyy")}.xlsx";
            workbook.Save($"wwwroot\\excel\\Download\\{url}", new OoxmlSaveOptions(SaveFormat.Xlsx));
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
                        CollectOnSupPrice = decimal.Parse(worksheet.Cells.Rows[row][21].Value is null || worksheet.Cells.Rows[row][21].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][21].Value.ToString().Replace(",", "").Trim()),
                        ClosingPercentCheck = decimal.Parse(worksheet.Cells.Rows[row][22].Value is null || worksheet.Cells.Rows[row][22].Value.ToString() == "" ? "0" : worksheet.Cells.Rows[row][22].Value.ToString().Replace("%", "").Replace(",", "").Trim()),
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
                        tran.CollectOnBehaftFeeCheck = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5;
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
                        tran.CollectOnBehaftFeeUpload = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5;
                        tran.CollectOnSupPriceUpload = x.CollectOnSupPrice;
                        tran.TotalPriceAfterTaxUpload = x.TotalPriceAfterTax;
                        if (tran.IsSeftPayment || tran.IsEmptyLift || (x.PickupEmpty != null && x.PickupEmpty.Contains("kết hợp")))
                        {
                            tran.LiftFee = 0;
                        }
                        if (tran.IsSeftPaymentLand || tran.IsLanding)
                        {
                            tran.LandingFee = 0;
                        }
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
                            CollectOnBehaftFeeCheck = x.Fee1 + x.Fee2 + x.Fee3 + x.Fee4 + x.Fee5,
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
    }
}
