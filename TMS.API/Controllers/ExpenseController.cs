using Aspose.Cells;
using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ExpenseController : TMSController<Expense>
    {
        public ExpenseController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<Expense>> PatchAsync([FromQuery] ODataQueryOptions<Expense> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            Expense entity = default;
            Expense oldEntity = default;
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            if (id != null && id.TryParseInt() > 0)
            {
                var idInt = id.TryParseInt() ?? 0;
                entity = await db.Set<Expense>().FindAsync(idInt);
                oldEntity = await db.Expense.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
            }
            else
            {
                entity = await GetEntityByOdataOptions(options);
                oldEntity = await GetEntityByOdataOptions(options);
            }
            patch.ApplyTo(entity);
            SetAuditInfo(entity);
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.ExpenseTypeId)) &&
            (oldEntity.ExpenseTypeId != entity.ExpenseTypeId))
            {
                await CheckDuplicates(entity);
            }
            if ((int)entity.GetPropValue(IdField) <= 0)
            {
                db.Add(entity);
            }
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.IsWet) ||
            x.Field == nameof(oldEntity.SteamingTerms) ||
            x.Field == nameof(oldEntity.BreakTerms)) &&
            (oldEntity.IsWet != entity.IsWet) ||
            (oldEntity.SteamingTerms != entity.SteamingTerms) ||
            (oldEntity.BreakTerms != entity.BreakTerms))
            {
                if (entity.IsWet && entity.SteamingTerms && entity.BreakTerms)
                {
                    throw new ApiException("Không thể cùng lúc có nhiều hơn 2 điều khoản") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.TransportationTypeId) ||
            x.Field == nameof(oldEntity.JourneyId) ||
            x.Field == nameof(oldEntity.CustomerTypeId) ||
            x.Field == nameof(oldEntity.CommodityId) ||
            x.Field == nameof(oldEntity.IsBought) ||
            x.Field == nameof(oldEntity.IsWet)) &&
            (oldEntity.TransportationTypeId != entity.TransportationTypeId) ||
            (oldEntity.JourneyId != entity.JourneyId) ||
            (oldEntity.CustomerTypeId != entity.CustomerTypeId) ||
            (oldEntity.CommodityId != entity.CommodityId) ||
            (oldEntity.IsBought != entity.IsBought) ||
            (oldEntity.IsWet != entity.IsWet))
            {
                var expenseType = await db.MasterData.Where(x => x.ParentId == 7577 && x.Name.Contains("Bảo hiểm")).FirstOrDefaultAsync();
                if (entity.ExpenseTypeId == expenseType.Id)
                {
                    var transportation = await db.Transportation.Where(x => x.Id == entity.TransportationId).FirstOrDefaultAsync();
                    var transportationPlan = await db.TransportationPlan.Where(x => x.Id == transportation.TransportationPlanId).FirstOrDefaultAsync();
                    transportationPlan.TransportationTypeId = entity.TransportationTypeId;
                    transportationPlan.JourneyId = entity.JourneyId;
                    transportationPlan.CustomerTypeId = entity.CustomerTypeId;
                    transportationPlan.CommodityId = entity.CommodityId;
                    transportationPlan.IsBought = entity.IsBought;
                    transportationPlan.IsWet = entity.IsWet;
                }
            }
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.TransportationTypeId) ||
            x.Field == nameof(oldEntity.JourneyId) ||
            x.Field == nameof(oldEntity.CustomerTypeId)) &&
            (oldEntity.TransportationTypeId != entity.TransportationTypeId) ||
            (oldEntity.JourneyId != entity.JourneyId) ||
            (oldEntity.CustomerTypeId != entity.CustomerTypeId))
            {
                var expenseTypeInsurance = await db.MasterData.Where(x => x.ParentId == 7577 && x.Name.Contains("Bảo hiểm")).FirstOrDefaultAsync();
                var expenseTypeSOC = await db.MasterData.Where(x => x.ParentId == 7577 && x.Name.Contains("BH SOC")).FirstOrDefaultAsync();
                if (entity.ExpenseTypeId == expenseTypeInsurance.Id)
                {
                    await SetWetAndJourneyForExpense(entity, oldEntity);
                    var expense = await db.Expense.Where(x => x.ExpenseTypeId == expenseTypeSOC.Id && x.TransportationId == entity.TransportationId && x.RequestChangeId == null).FirstOrDefaultAsync();
                    if (expense != null)
                    {
                        expense.TransportationTypeId = entity.TransportationTypeId;
                        expense.CustomerTypeId = entity.CustomerTypeId;
                        await SetWetAndJourneyForExpense(entity, oldEntity);
                        await CalcInsuranceFees(expense, true);
                    }
                    await CalcInsuranceFees(entity, false);
                }
                else if (entity.ExpenseTypeId == expenseTypeSOC.Id)
                {
                    var expense = await db.Expense.Where(x => x.ExpenseTypeId == expenseTypeInsurance.Id && x.TransportationId == entity.TransportationId && x.RequestChangeId == null).FirstOrDefaultAsync();
                    if (expense != null)
                    {
                        expense.TransportationTypeId = entity.TransportationTypeId;
                        expense.CustomerTypeId = entity.CustomerTypeId;
                        await SetWetAndJourneyForExpense(expense, oldEntity);
                        await CalcInsuranceFees(expense, false);
                    }
                    await CalcInsuranceFees(entity, true);
                }
            }
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.IsWet) ||
            x.Field == nameof(oldEntity.IsBought)) &&
            (oldEntity.IsWet != entity.IsWet) ||
            (oldEntity.IsBought != entity.IsBought))
            {
                await CalcInsuranceFees(entity, false);
            }
            await db.SaveChangesAsync();
            await db.Entry(entity).ReloadAsync();
            RealTimeUpdate(entity);
            return entity;
        }

        private void RealTimeUpdate(Expense entity)
        {
            var thead = new Thread(async () =>
            {
                try
                {
                    await _taskService.SendMessageAllUser(new WebSocketResponse<Expense>
                    {
                        EntityId = _entitySvc.GetEntity(typeof(Expense).Name).Id,
                        Data = entity
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("RealtimeUpdate error at {0}: {1} {2}", DateTimeOffset.Now, ex.Message, ex.StackTrace);
                }
            });
            thead.Start();
        }

        private async Task SetWetAndJourneyForExpense(Expense expense, Expense oldExpense)
        {
            if (expense.TransportationTypeId != null)
            {
                if (expense.TransportationTypeId != 11673) //Tàu
                {
                    if (expense.IsPurchasedInsurance == false && expense.RequestChangeId == null)
                    {
                        expense.IsWet = expense.TransportationTypeId == 11677 ? true : false;
                        expense.JourneyId = expense.TransportationTypeId != 11677 ? 12114 : null;
                    }
                }
                var transportation = await db.Transportation.Where(x => x.Id == expense.TransportationId).FirstOrDefaultAsync();
                if (expense.JourneyId == 12114 || expense.JourneyId == 16001)
                {
                    expense.StartShip = transportation.ClosingDate;
                }
                else
                {
                    expense.StartShip = transportation.StartShip;
                }
            }
        }

        private async Task CalcInsuranceFees(Expense expense, bool isSOC)
        {
            var containerId = await CheckContainerType(expense);
            if (expense.IsPurchasedInsurance == false && expense.RequestChangeId == null)
            {
                bool isSubRatio = false;
                if (((expense.IsWet || expense.SteamingTerms || expense.BreakTerms) && expense.IsBought == false) || (expense.IsBought && expense.IsWet))
                {
                    isSubRatio = true;
                }
                var insuranceFeesRateDB = await db.InsuranceFeesRate.Where(x => x.TransportationTypeId == expense.TransportationTypeId && x.JourneyId == expense.JourneyId && x.IsBought == expense.IsBought && x.IsSubRatio == isSubRatio && x.IsSOC == isSOC).FirstOrDefaultAsync();
                if (insuranceFeesRateDB != null)
                {
                    var getContainerType = await db.MasterData.Where(x => x.Id == expense.ContainerTypeId).FirstOrDefaultAsync();
                    if (getContainerType != null && getContainerType.Description.Contains("Lạnh") && insuranceFeesRateDB.TransportationTypeId == 11673 && insuranceFeesRateDB.JourneyId == 12114)
                    {
                        var insuranceFeesRateColdDB = await db.MasterData.Where(x => x.Id == 25391).FirstOrDefaultAsync();
                        expense.InsuranceFeeRate = insuranceFeesRateColdDB != null ? decimal.Parse(insuranceFeesRateColdDB.Name) : 0;
                    }
                    else
                    {
                        expense.InsuranceFeeRate = insuranceFeesRateDB.Rate;
                    }
                    if (insuranceFeesRateDB.IsSubRatio && expense.IsBought == false)
                    {
                        var extraInsuranceFeesRateDB = await db.MasterData.Where(x => x.Active == true && x.ParentId == 25374).ToListAsync();
                        extraInsuranceFeesRateDB.ForEach(x =>
                        {
                            foreach (var prop in expense.GetType().GetProperties())
                            {
                                if (prop.Name == x.Name && bool.Parse(prop.GetValue(expense, null).ToString()))
                                {
                                    expense.InsuranceFeeRate += decimal.Parse(x.Code);
                                    break;
                                }
                            }
                        });
                    }
                }
                else
                {
                    expense.InsuranceFeeRate = 0;
                    expense.TotalPriceBeforeTax = 0;
                    expense.TotalPriceAfterTax = 0;
                }
                if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == true)
                {
                    CalcInsuranceFeeNoVAT(expense);
                }
                else if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == false)
                {
                    CalcInsuranceFee(expense);
                }
            }
        }

        private int containerId = 0;

        public async Task<int> CheckContainerType(Expense expense)
        {
            var containerTypes = await db.MasterData.Where(x => x.ParentId == 7565).ToListAsync();
            var containerTypeCodes = containerTypes.ToDictionary(x => x.Id);
            var containerTypeName = containerTypeCodes.GetValueOrDefault((int)expense.ContainerTypeId);
            var containers = await db.MasterData.Where(x => x.Name.Contains("40HC") || x.Name.Contains("20DC") || x.Name.Contains("45HC") || x.Name.Contains("50DC")).ToListAsync();
            var masterDataDB = await db.MasterData.Where(x => x.Id == 11685).FirstOrDefaultAsync();
            if (containerTypeName.Description.Contains("Cont 20"))
            {
                containerId = containers.Find(x => x.Name.Contains("20DC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 40"))
            {
                containerId = containers.Find(x => x.Name.Contains("40HC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 45"))
            {
                containerId = containers.Find(x => x.Name.Contains("45HC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 50"))
            {
                containerId = containers.Find(x => x.Name.Contains("50DC")).Id;
            }
            return containerId;
        }

        private void CalcInsuranceFee(Expense expense)
        {
            expense.TotalPriceBeforeTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
            expense.TotalPriceAfterTax = expense.TotalPriceBeforeTax + Math.Round(expense.TotalPriceBeforeTax * expense.Vat / 100, 0);
        }

        private void CalcInsuranceFeeNoVAT(Expense expense)
        {
            expense.TotalPriceAfterTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
            expense.TotalPriceBeforeTax = Math.Round(expense.TotalPriceAfterTax / (decimal)1.1, 0);
        }

        public override async Task<ActionResult<Expense>> CreateAsync([FromBody] Expense entity)
        {
            await CheckDuplicates(entity);
            return await base.CreateAsync(entity);
        }

        public async Task CheckDuplicates(Expense expense)
        {
            var expenseType = await db.MasterData.Where(x => x.ParentId == 7577 && x.Name.Contains("Bảo hiểm")).FirstOrDefaultAsync();
            var expenseTypeSOC = await db.MasterData.Where(x => x.ParentId == 7577 && x.Name.Contains("BH SOC")).FirstOrDefaultAsync();
            if (expense.ExpenseTypeId == expenseType.Id || expense.ExpenseTypeId == expenseTypeSOC.Id)
            {
                var check = await db.Expense.Where(x => x.Active && x.TransportationId == expense.TransportationId && x.ExpenseTypeId == expense.ExpenseTypeId && x.RequestChangeId == null).FirstOrDefaultAsync();
                if (check != null && expense.RequestChangeId == null)
                {
                    throw new ApiException("Đã tồn tại trong hệ thống") { StatusCode = HttpStatusCode.BadRequest };
                }
            }
        }

        public override async Task<ActionResult<bool>> RequestApprove([FromBody] Expense entity)
        {
            var id = entity.GetPropValue(nameof(GridPolicy.Id)) as int?;
            var (statusField, value) = entity.GetComplexProp("StatusId");
            if (statusField)
            {
                entity.SetPropValue("StatusId", (int)ApprovalStatusEnum.Approving);
            }

            if (id <= 0)
            {
                await CreateAsync(entity);
            }
            var approvalConfig = await GetApprovalConfig(entity);
            if (approvalConfig.Nothing())
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
            if (matchApprovalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            await Approving(entity);
            var oldEntity = await db.Expense.FindAsync(entity.RequestChangeId);
            await db.Entry(oldEntity).ReloadAsync();
            await _taskService.SendMessageAllUser(new WebSocketResponse<Expense>
            {
                EntityId = _entitySvc.GetEntity(typeof(Expense).Name).Id,
                Data = oldEntity
            });
            await db.SaveChangesAsync();
            var listUser = await GetApprovalUsers(entity, matchApprovalConfig);
            if (listUser.HasElement())
            {
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = listUser.Select(user => new TaskNotification
                {
                    Title = $"{currentUser.FullName}",
                    Description = $"Đã gửi yêu chỉnh sửa ạ ",
                    EntityId = _entitySvc.GetEntity(typeof(Expense).Name).Id,
                    RecordId = oldEntity.Id,
                    Attachment = "fal fa-paper-plane",
                    AssignedId = user.Id,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now.Date,
                });
                SetAuditInfo(tasks);
                db.AddRange(tasks);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(tasks);
            }
            return true;
        }

        public override async Task<ActionResult<bool>> Approve([FromBody] Expense entity, string reasonOfChange = "")
        {
            var rs = await base.Approve(entity, reasonOfChange);
            await db.Entry(entity).ReloadAsync();
            var oldEntity = await db.Expense.FindAsync(entity.RequestChangeId);
            oldEntity.CopyPropFrom(entity, nameof(Expense.Id), nameof(Expense.RequestChangeId), nameof(Expense.InsertedDate), nameof(Expense.InsertedBy));
            var user = await db.User.FindAsync(UserId);
            var taskNotification = new TaskNotification
            {
                Title = $"{user.FullName}",
                Description = $"Đã duyệt yêu cầu chỉnh sửa ạ",
                EntityId = _entitySvc.GetEntity(typeof(Expense).Name).Id,
                RecordId = oldEntity.Id,
                Attachment = "fal fa-check",
                AssignedId = entity.InsertedBy,
                StatusId = (int)TaskStateEnum.UnreadStatus,
                RemindBefore = 540,
                Deadline = DateTime.Now.Date,
            };
            SetAuditInfo(taskNotification);
            db.AddRange(taskNotification);
            await db.SaveChangesAsync();
            await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            await db.Entry(entity).ReloadAsync();
            await _taskService.SendMessageAllUser(new WebSocketResponse<Expense>
            {
                EntityId = _entitySvc.GetEntity(typeof(Expense).Name).Id,
                Data = oldEntity
            });
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
            try
            {
                var deleteCommand = $"delete from [{typeof(Expense).Name}] where RequestChangeId in ({string.Join(",", ids)}); delete from [{typeof(Expense).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteCommand);
                return true;
            }
            catch
            {
                throw new ApiException("Không thể xóa dữ liệu!") { StatusCode = HttpStatusCode.BadRequest };
            }
        }

        [HttpPost("api/Expense/ExportCheckChange")]
        public async Task<string> ExportCheckChange([FromBody] List<int> expenseIds)
        {
            var expenses = await db.Expense.Where(x => expenseIds.Contains(x.Id)).ToListAsync();
            Workbook workbook = new Workbook();
            Worksheet worksheet = workbook.Worksheets[0];
            worksheet.Cells["A9"].PutValue($"STT");
            SetBackgroundColor(workbook, "A9");
            SetBorder(workbook, "A10");
            worksheet.Cells.Merge(8, 0, 2, 1);
            worksheet.Cells["B9"].PutValue($"Đã chốt");
            SetBackgroundColor(workbook, "B9");
            SetBorder(workbook, "B10");
            worksheet.Cells["C9"].PutValue($"Đã mua BH");
            worksheet.Cells.Merge(8, 1, 2, 1);
            SetBackgroundColor(workbook, "C9");
            SetBorder(workbook, "C10");
            worksheet.Cells["D9"].PutValue($"Ngày mua BH");
            worksheet.Cells.Merge(8, 2, 2, 1);
            SetBackgroundColor(workbook, "D9");
            SetBorder(workbook, "D10");
            worksheet.Cells["E9"].PutValue($"Loại vận chuyển");
            worksheet.Cells.Merge(8, 3, 2, 1);
            SetBackgroundColor(workbook, "E9");
            SetBorder(workbook, "E10");
            worksheet.Cells["F9"].PutValue($"Hành trình BH");
            worksheet.Cells.Merge(8, 4, 2, 1);
            SetBackgroundColor(workbook, "F9");
            SetBorder(workbook, "F10");
            worksheet.Cells["G9"].PutValue($"Tuyến vận chuyển");
            worksheet.Cells.Merge(8, 5, 2, 1);
            SetBackgroundColor(workbook, "G9");
            SetBorder(workbook, "G10");
            worksheet.Cells["H9"].PutValue($"Tên tàu");
            worksheet.Cells.Merge(8, 6, 2, 1);
            SetBackgroundColor(workbook, "H9");
            SetBorder(workbook, "H10");
            worksheet.Cells["I9"].PutValue($"Ngày đóng hàng/Ngày tàu chạy");
            worksheet.Cells.Merge(8, 7, 2, 1);
            SetBackgroundColor(workbook, "I9");
            SetBorder(workbook, "I10");
            worksheet.Cells["J9"].PutValue($"Chủ hàng");
            worksheet.Cells.Merge(8, 8, 2, 1);
            SetBorder(workbook, "J10");
            SetBackgroundColor(workbook, "J9");
            worksheet.Cells["K9"].PutValue($"Vật tư hàng hóa");
            worksheet.Cells.Merge(8, 9, 2, 1);
            SetBackgroundColor(workbook, "K9");
            SetBorder(workbook, "K10");
            worksheet.Cells["L9"].PutValue($"Loại container");
            worksheet.Cells.Merge(8, 10, 2, 1);
            SetBackgroundColor(workbook, "L9");
            SetBorder(workbook, "L10");
            worksheet.Cells["M9"].PutValue($"Số chuyến");
            worksheet.Cells.Merge(8, 11, 2, 1);
            SetBackgroundColor(workbook, "M9");
            SetBorder(workbook, "M10");
            worksheet.Cells["N9"].PutValue($"Số cont");
            worksheet.Cells.Merge(8, 12, 2, 1);
            SetBackgroundColor(workbook, "N9");
            SetBorder(workbook, "N10");
            worksheet.Cells["O9"].PutValue($"Số seal");
            worksheet.Cells.Merge(8, 13, 2, 1);
            SetBackgroundColor(workbook, "O9");
            SetBorder(workbook, "O10");
            worksheet.Cells["P9"].PutValue($"GTHH");
            worksheet.Cells.Merge(8, 14, 2, 1);
            SetBackgroundColor(workbook, "P9");
            SetBorder(workbook, "P10");
            worksheet.Cells["Q9"].PutValue($"Ghi chú GTHH");
            worksheet.Cells.Merge(8, 15, 2, 1);
            SetBackgroundColor(workbook, "Q9");
            SetBorder(workbook, "Q10");
            worksheet.Cells["R9"].PutValue($"Mua hộ BH");
            worksheet.Cells.Merge(8, 16, 2, 1);
            SetBackgroundColor(workbook, "R9");
            SetBorder(workbook, "R10");
            worksheet.Cells["S9"].PutValue($"BH ướt");
            worksheet.Cells.Merge(8, 17, 2, 1);
            SetBackgroundColor(workbook, "S9");
            SetBorder(workbook, "S10");
            worksheet.Cells["T9"].PutValue($"Tỷ lệ phí");
            worksheet.Cells.Merge(8, 18, 2, 1);
            SetBackgroundColor(workbook, "T9");
            SetBorder(workbook, "T10");
            worksheet.Cells["U9"].PutValue($"Phí bảo hiểm (Chưa VAT)");
            worksheet.Cells.Merge(8, 19, 2, 1);
            SetBackgroundColor(workbook, "U9");
            SetBorder(workbook, "U10");
            worksheet.Cells["V9"].PutValue($"VAT");
            worksheet.Cells.Merge(8, 20, 2, 1);
            SetBackgroundColor(workbook, "V9");
            SetBorder(workbook, "V10");
            worksheet.Cells["W9"].PutValue($"Phí bảo hiểm");
            worksheet.Cells.Merge(8, 21, 2, 1);
            SetBackgroundColor(workbook, "W9");
            SetBorder(workbook, "W10");
            worksheet.Cells["X9"].PutValue($"Yêu cầu chứng thư");
            worksheet.Cells.Merge(8, 22, 2, 1);
            SetBackgroundColor(workbook, "X9");
            SetBorder(workbook, "X10");
            worksheet.Cells["Y9"].PutValue($"Ghi chú cont hàng");
            worksheet.Cells.Merge(8, 23, 2, 1);
            SetBackgroundColor(workbook, "Y9");
            SetBorder(workbook, "Y10");
            worksheet.Cells["Z9"].PutValue($"Ghi chú phí BH");
            worksheet.Cells.Merge(8, 24, 2, 1);
            SetBackgroundColor(workbook, "Z9");
            SetBorder(workbook, "Z10");
            worksheet.Cells.Merge(8, 25, 2, 1);
            var ids = expenses.Select(x => x.Id).ToList();
            var sql = @$"select e.Id,
            e.IsClosing,
            e.IsPurchasedInsurance,
            e.DatePurchasedInsurance,
            m1.Name as TransportationType,
            m2.Name as Journey,
            r.Name as Route,
            s.Name as Ship,
            e.StartShip,
            v1.Name as Boss,
            m3.Description as Commodity,
            m4.Description as ContainerType,
            e.Trip,
            e.ContainerNo,
            e.SealNo,
            e.CommodityValue,
            e.CommodityValueNotes,
            e.IsBought,
            e.IsWet,
            e.InsuranceFeeRate,
            e.TotalPriceBeforeTax,
            e.Vat,
            e.TotalPriceAfterTax,
            m5.Description as CustomerType,
            e.Notes,
            e.NotesInsuranceFees
            from Expense e
            left join MasterData m1 on e.TransportationTypeId = m1.Id
            left join MasterData m2 on e.JourneyId = m2.Id
            left join Route r on e.RouteId = r.Id
            left join Ship s on e.ShipId = s.Id
            left join Vendor v1 on e.BossId = v1.Id
            left join MasterData m3 on e.CommodityId = m3.Id
            left join MasterData m4 on e.ContainerTypeId = m4.Id
            left join MasterData m5 on e.CustomerTypeId = m5.Id
            where e.Id in ({ids.Combine()})";
            var data = await ConverSqlToDataSet(sql);
            var start = 11;
            var expensePurchasedIds = expenses.Where(x => x.IsPurchasedInsurance).Select(x => x.Id).ToList();
            var expenseChanges = await db.Expense.Where(x => expensePurchasedIds.Contains((int)x.RequestChangeId)).ToListAsync();
            foreach (var item in data[0])
            {
                worksheet.Cells["A" + start].PutValue(start - 10);
                worksheet.Cells["B" + start].PutValue(item["IsClosing"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cells["C" + start].PutValue(item["IsPurchasedInsurance"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cells["D" + start].PutValue(item["DatePurchasedInsurance"] is null ? "" : DateTime.Parse(item[nameof(Expense.DatePurchasedInsurance)].ToString()).ToString("dd/MM/yyyy"));
                worksheet.Cells["E" + start].PutValue(item["TransportationType"] is null ? "" : item["TransportationType"].ToString());
                worksheet.Cells["F" + start].PutValue(item["Journey"] is null ? "" : item["Journey"].ToString());
                worksheet.Cells["G" + start].PutValue(item["Route"] is null ? "" : item["Route"].ToString());
                worksheet.Cells["H" + start].PutValue(item["Ship"] is null ? "" : item["Ship"].ToString());
                worksheet.Cells["I" + start].PutValue(item["StartShip"] is null ? "" : DateTime.Parse(item[nameof(Expense.StartShip)].ToString()).ToString("dd/MM/yyyy"));
                worksheet.Cells["J" + start].PutValue(item["Boss"] is null ? "" : item["Boss"].ToString());
                worksheet.Cells["K" + start].PutValue(item["Commodity"] is null ? "" : item["Commodity"].ToString());
                worksheet.Cells["L" + start].PutValue(item["ContainerType"] is null ? "" : item["ContainerType"].ToString());
                worksheet.Cells["M" + start].PutValue(item["Trip"] is null ? "" : item["Trip"].ToString());
                worksheet.Cells["N" + start].PutValue(item["ContainerNo"] is null ? "" : item["ContainerNo"].ToString());
                worksheet.Cells["O" + start].PutValue(item["SealNo"] is null ? "" : item["SealNo"].ToString());
                worksheet.Cells["P" + start].PutValue(item["CommodityValue"] is null ? "0" : $"{decimal.Parse(item["CommodityValue"].ToString()):n0}");
                worksheet.Cells["Q" + start].PutValue(item["CommodityValueNotes"] is null ? "" : item["CommodityValueNotes"].ToString());
                worksheet.Cells["R" + start].PutValue(item["IsBought"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cells["S" + start].PutValue(item["IsWet"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cells["T" + start].PutValue(item["InsuranceFeeRate"] is null ? "0" : item["InsuranceFeeRate"].ToString());
                worksheet.Cells["U" + start].PutValue(item["TotalPriceBeforeTax"] is null ? "0" : $"{decimal.Parse(item["TotalPriceBeforeTax"].ToString()):n0}");
                worksheet.Cells["V" + start].PutValue(item["Vat"] is null ? "" : $"{decimal.Parse(item["Vat"].ToString()):n0}");
                worksheet.Cells["W" + start].PutValue(item["TotalPriceAfterTax"] is null ? "0" : $"{decimal.Parse(item["TotalPriceAfterTax"].ToString()):n0}");
                worksheet.Cells["X" + start].PutValue(item["CustomerType"] is null ? "" : item["CustomerType"].ToString());
                worksheet.Cells["Y" + start].PutValue(item["Notes"] is null ? "" : item["Notes"].ToString());
                worksheet.Cells["Z" + start].PutValue(item["NotesInsuranceFees"] is null ? "" : item["NotesInsuranceFees"].ToString());
                var id = int.Parse(item["Id"].ToString());
                var expenseChangesOfItem = expenseChanges.Where(x => x.RequestChangeId == id && x.StatusId == 1).ToList();
                if (expenseChangesOfItem != null)
                {
                    var fieldChanges = new List<string>();
                    foreach (var change in expenseChangesOfItem)
                    {
                        var cutting = await db.Expense.Where(x => x.Id == id).FirstOrDefaultAsync();
                        var fieldNames = GetVariance(change, cutting).ToList();
                        fieldNames.ForEach(x =>
                        {
                            var check = fieldChanges.Where(y => y.Contains(x.Name)).FirstOrDefault();
                            if (check == null)
                            {
                                fieldChanges.Add(x.Name);
                            }
                        });
                    }
                    fieldChanges.ForEach(x =>
                    {
                        if (x == nameof(Expense.IsClosing))
                        {
                            SetColor(workbook, "B" + start);
                        }
                        else if (x == nameof(Expense.IsPurchasedInsurance))
                        {
                            SetColor(workbook, "C" + start);
                        }
                        else if (x == nameof(Expense.DatePurchasedInsurance))
                        {
                            SetColor(workbook, "D" + start);
                        }
                        else if (x == nameof(Expense.TransportationTypeId))
                        {
                            SetColor(workbook, "E" + start);
                        }
                        else if (x == nameof(Expense.JourneyId))
                        {
                            SetColor(workbook, "F" + start);
                        }
                        else if (x == nameof(Expense.RouteId))
                        {
                            SetColor(workbook, "G" + start);
                        }
                        else if (x == nameof(Expense.ShipId))
                        {
                            SetColor(workbook, "H" + start);
                        }
                        else if (x == nameof(Expense.StartShip))
                        {
                            SetColor(workbook, "I" + start);
                        }
                        else if (x == nameof(Expense.BossId))
                        {
                            SetColor(workbook, "J" + start);
                        }
                        else if (x == nameof(Expense.CommodityId))
                        {
                            SetColor(workbook, "K" + start);
                        }
                        else if (x == nameof(Expense.ContainerTypeId))
                        {
                            SetColor(workbook, "L" + start);
                        }
                        else if (x == nameof(Expense.Trip))
                        {
                            SetColor(workbook, "M" + start);
                        }
                        else if (x == nameof(Expense.ContainerNo))
                        {
                            SetColor(workbook, "N" + start);
                        }
                        else if (x == nameof(Expense.SealNo))
                        {
                            SetColor(workbook, "O" + start);
                        }
                        else if (x == nameof(Expense.CommodityValue))
                        {
                            SetColor(workbook, "P" + start);
                        }
                        else if (x == nameof(Expense.CommodityValueNotes))
                        {
                            SetColor(workbook, "Q" + start);
                        }
                        else if (x == nameof(Expense.IsBought))
                        {
                            SetColor(workbook, "R" + start);
                        }
                        else if (x == nameof(Expense.IsWet))
                        {
                            SetColor(workbook, "S" + start);
                        }
                        else if (x == nameof(Expense.InsuranceFeeRate))
                        {
                            SetColor(workbook, "T" + start);
                        }
                        else if (x == nameof(Expense.TotalPriceBeforeTax))
                        {
                            SetColor(workbook, "U" + start);
                        }
                        else if (x == nameof(Expense.Vat))
                        {
                            SetColor(workbook, "V" + start);
                        }
                        else if (x == nameof(Expense.TotalPriceAfterTax))
                        {
                            SetColor(workbook, "W" + start);
                        }
                        else if (x == nameof(Expense.CustomerTypeId))
                        {
                            SetColor(workbook, "X" + start);
                        }
                        else if (x == nameof(Expense.Notes))
                        {
                            SetColor(workbook, "Y" + start);
                        }
                        else if (x == nameof(Expense.NotesInsuranceFees))
                        {
                            SetColor(workbook, "Z" + start);
                        }
                    });
                }
                start++;
            }
            var url = $"Phí bảo hiểm.xlsx";
            workbook.Save($"wwwroot\\excel\\Download\\{url}", new OoxmlSaveOptions(SaveFormat.Xlsx));
            return url;
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
            style.Font.IsBold = true;
            workbook.Worksheets[0].Cells[cell].SetStyle(style);
            SetBorder(workbook, cell);
        }

        private void SetColor(Workbook workbook, string cell)
        {
            var style = workbook.Worksheets[0].Cells[cell].GetStyle();
            style.Font.Color = Color.Red;
            style.Font.IsBold = true;
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
    }
}
