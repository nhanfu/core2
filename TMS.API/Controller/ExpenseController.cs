using Aspose.Cells;
using ClosedXML.Excel;
using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq.Dynamic.Core;
using System.Reflection;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ExpenseController : TMSController<Expense>
    {
        public ExpenseController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<Expense>> PatchAsync([FromQuery] ODataQueryOptions<Expense> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
            var entity = await db.Expense.FindAsync(idInt);
            if (patch.Changes.Any(x => x.Field == nameof(entity.ExpenseTypeId)) && entity.UnitPrice > 0)
            {
                var entityCheck = new Expense();
                entityCheck.CopyPropFrom(entity);
                var expenseTypeChange = patch.Changes.Where(x => x.Field == nameof(Expense.ExpenseTypeId)).FirstOrDefault();
                entityCheck.ExpenseTypeId = expenseTypeChange != null ? int.Parse(expenseTypeChange.Value) : entityCheck.ExpenseTypeId;
                await CheckDuplicates(entityCheck);
            }
            if (patch.Changes.Any(x => x.Field == nameof(entity.IsWet) ||
            x.Field == nameof(entity.SteamingTerms) ||
            x.Field == nameof(entity.BreakTerms)))
            {
                var isWetChange = patch.Changes.Where(x => x.Field == nameof(Expense.IsWet)).FirstOrDefault();
                var isWet = isWetChange != null ? bool.Parse(isWetChange.Value) : entity.IsWet;
                var steamingTermsChange = patch.Changes.Where(x => x.Field == nameof(Expense.SteamingTerms)).FirstOrDefault();
                var steamingTerms = steamingTermsChange != null ? bool.Parse(steamingTermsChange.Value) : entity.SteamingTerms;
                var breakTermsChange = patch.Changes.Where(x => x.Field == nameof(Expense.BreakTerms)).FirstOrDefault();
                var breakTerms = breakTermsChange != null ? bool.Parse(breakTermsChange.Value) : entity.BreakTerms;
                if (isWet && steamingTerms && breakTerms)
                {
                    throw new ApiException("Không thể cùng lúc có nhiều hơn 2 điều khoản") { StatusCode = HttpStatusCode.BadRequest };
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
                            command.CommandText += $" DISABLE TRIGGER ALL ON [{nameof(Expense)}];";
                        }
                        else
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(Expense)}];";
                        }
                        command.CommandText += $" UPDATE [{nameof(Expense)}] SET {update.Combine()} WHERE Id = {idInt};";
                        //
                        if (disableTrigger)
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(Expense)}];";
                        }
                        foreach (var item in updates)
                        {
                            command.Parameters.AddWithValue($"@{item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                        }
                        var rs = command.ExecuteNonQuery();
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

        private void CalcInsuranceFees(Expense expense, bool isSOC, List<InsuranceFeesRate> insuranceFeesRates, List<MasterData> extraInsuranceFeesRateDB, MasterData containerExpense, MasterData insuranceFeesRateColdDB)
        {
            if (expense.IsPurchasedInsurance == false && expense.RequestChangeId == null)
            {
                bool isSubRatio = false;
                if (((expense.IsWet || expense.SteamingTerms || expense.BreakTerms) && expense.IsBought == false) || (expense.IsBought && expense.IsWet))
                {
                    isSubRatio = true;
                }
                InsuranceFeesRate insuranceFeesRateDB = null;
                if (expense.IsBought)
                {
                    insuranceFeesRateDB = insuranceFeesRates.Where(x => x.TransportationTypeId == expense.TransportationTypeId && x.JourneyId == expense.JourneyId && x.IsBought == expense.IsBought && x.IsSOC == isSOC && x.IsSubRatio == isSubRatio).FirstOrDefault();
                }
                else
                {
                    insuranceFeesRateDB = insuranceFeesRates.Where(x => x.TransportationTypeId == expense.TransportationTypeId && x.JourneyId == expense.JourneyId && x.IsBought == expense.IsBought && x.IsSOC == isSOC).FirstOrDefault();
                }
                if (insuranceFeesRateDB != null)
                {
                    if (containerExpense != null && containerExpense.Description.ToLower().Contains("lạnh") && insuranceFeesRateDB.TransportationTypeId == 11673 && insuranceFeesRateDB.JourneyId == 12114)
                    {
                        expense.InsuranceFeeRate = insuranceFeesRateColdDB != null ? decimal.Parse(insuranceFeesRateColdDB.Name) : 0;
                    }
                    else
                    {
                        expense.InsuranceFeeRate = insuranceFeesRateDB.Rate;
                    }
                    if (insuranceFeesRateDB.IsSubRatio && expense.IsBought == false)
                    {
                        extraInsuranceFeesRateDB.ForEach(x =>
                        {
                            var prop = expense.GetType().GetProperties().Where(y => y.Name == x.Name && bool.Parse(y.GetValue(expense, null).ToString())).FirstOrDefault();
                            if (prop != null)
                            {
                                expense.InsuranceFeeRate += decimal.Parse(x.Code);
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

        public async Task<int> CheckContainerType(Transportation transportation)
        {
            var containerTypes = await db.MasterData.Where(x => x.ParentId == 7565).ToListAsync();
            var containerTypeCodes = containerTypes.ToDictionary(x => x.Id);
            var containerTypeName = containerTypeCodes.GetValueOrDefault((int)transportation.ContainerTypeId);
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
            RealTimeUpdateUser(oldEntity);
            return rs;
        }

        private void RealTimeUpdateUser(Expense entity)
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
                var exps = await db.Expense.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync();
                var deleteCommand = $"delete from [{typeof(Expense).Name}] where RequestChangeId in ({string.Join(",", ids)}); delete from [{typeof(Expense).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteCommand);
                var tranids = exps.Select(x => x.TransportationId).Where(x => x != null).Distinct().ToList();
                var trans = await db.Transportation.Include(x => x.Expense).Where(x => tranids.Contains(x.Id)).ToListAsync();
                var expenseTypeIds = exps.Select(x => x.ExpenseTypeId).Where(x => x != null).Distinct().ToList();
                var expenseTypes = await db.MasterData.AsNoTracking().Where(x => expenseTypeIds.Contains(x.Id)).ToListAsync();
                foreach (var item in trans)
                {
                    var details = new List<PatchUpdateDetail>();
                    var expenses = item.Expense;
                    foreach (var itemDetail in expenseTypes.Select(x => x.Additional).Distinct().Where(x => !x.IsNullOrWhiteSpace()).ToList())
                    {
                        var expenseTypeThisIds = expenseTypes.Where(x => x.Additional == itemDetail).Select(x => x.Id).Distinct().ToList();
                        var totalThisValue = expenses.Where(x => expenseTypeThisIds.Contains(x.ExpenseTypeId.Value)).Sum(x => x.TotalPriceAfterTax);
                        item.SetPropValue(itemDetail, totalThisValue);
                    }
                }
                await db.SaveChangesAsync();
                return true;
            }
            catch
            {
                throw new ApiException("Không thể xóa dữ liệu!") { StatusCode = HttpStatusCode.BadRequest };
            }
        }

        [HttpPost("api/Expense/PurchasedInsuranceFees")]
        public async Task<bool> PurchasedInsuranceFees([FromBody] List<int> ids)
        {
            if (ids == null)
            {
                return false;
            }
            var cmd = $"Update [{nameof(Expense)}] set IsPurchasedInsurance = 1, DatePurchasedInsurance = '{DateTime.Now.ToString("yyyy-MM-dd")}'" +
                $" where Id in ({ids.Combine()}) and ExpenseTypeId in (15981, 15939) and RequestChangeId is null;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Expense;", "ENABLE TRIGGER ALL ON Expense;");
            return true;
        }

        [HttpPost("api/Expense/ClosingInsuranceFees")]
        public async Task<bool> ClosingInsuranceFees([FromBody] List<Expense> expenses)
        {
            if (expenses == null)
            {
                return false;
            }
            var expensePurchased = expenses.Where(x => x.IsPurchasedInsurance).ToList();
            var idPurchaseds = expensePurchased.Select(x => x.Id).ToList();
            var expenseNoPurchased = expenses.Where(x => x.IsPurchasedInsurance == false).ToList();
            var idNoPurchaseds = expenseNoPurchased.Select(x => x.Id).ToList();
            var cmd = $"Update [{nameof(Expense)}] set IsClosing = 1" +
                $" where Id in ({idPurchaseds.Combine()}) and ExpenseTypeId in (15981, 15939) and RequestChangeId is null";
            cmd += $" Update [{nameof(Expense)}] set IsClosing = 1, IsPurchasedInsurance = 1, DatePurchasedInsurance = '{DateTime.Now.ToString("yyyy-MM-dd")}'" +
                $" where Id in ({idNoPurchaseds.Combine()}) and ExpenseTypeId in (15981, 15939) and RequestChangeId is null;";
            await ExecSql(cmd, "DISABLE TRIGGER ALL ON Expense;", "ENABLE TRIGGER ALL ON Expense;");
            return true;
        }

        [HttpPost("api/Expense/UpdateDataFromTransportation")]
        public async Task<bool> UpdateDataFromTransportation([FromBody] Expense expense)
        {
            var expenseTypes = await db.MasterData.Where(x => x.Active && x.ParentId == 7577 && (x.Name.Contains("Bảo hiểm") || x.Name.Contains("BH SOC"))).ToListAsync();
            var expenseTypeIds = expenseTypes.Select(x => x.Id).ToList();
            var trans = await db.Transportation.AsNoTracking().Where(x => ((x.ClosingDate >= expense.FromDate && x.ClosingDate <= expense.ToDate) || (x.StartShip >= expense.FromDate && x.StartShip <= expense.ToDate)) && x.Active).ToListAsync();
            //var trans = await db.Transportation.AsNoTracking().Where(x => (x.ClosingDate >= DateTime.Parse("2023/01/01") || x.StartShip >= DateTime.Parse("2023/01/01")) && x.Active).ToListAsync();
            if (trans == null)
            {
                return false;
            }

            var containerTypes = await db.MasterData.Where(x => x.ParentId == 7565).ToListAsync();
            var containerTypeIds = containerTypes.ToDictionary(x => x.Id);
            var containers = await db.MasterData.Where(x => x.Name.Contains("40HC") || x.Name.Contains("20DC") || x.Name.Contains("45HC") || x.Name.Contains("50DC")).ToListAsync();

            var bossIds = trans.Select(x => x.BossId).ToList();
            var commodityIds = trans.Select(x => x.CommodityId).ToList();
            var commodityValues = await db.CommodityValue.Where(x => bossIds.Contains(x.BossId) && commodityIds.Contains(x.CommodityId) && x.Active).ToListAsync();
            var commodityValuesSOC = await db.CommodityValue.Where(x => x.CommodityId == 15764 && x.Active).ToListAsync();
            var commodityValueOfTrans = new Dictionary<int, CommodityValue>();

            var transportationTypes = await db.MasterData.Where(x => x.ParentId == 11670).ToListAsync();
            var routes = await db.Route.Where(x => trans.Select(y => y.RouteId).ToList().Contains(x.Id)).ToListAsync();
            var vendors = await db.Vendor.Where(x => trans.Select(y => y.ClosingId).ToList().Contains(x.Id)).ToListAsync();
            foreach (var item in trans)
            {
                MasterData container = null;
                if (item.ContainerTypeId != null)
                {
                    container = containerTypeIds.GetValueOrDefault((int)item.ContainerTypeId);
                    if (container == null)
                    {
                        continue;
                    }
                    var containerId = 0;
                    if (container.Description.Contains("Cont 20"))
                    {
                        containerId = containers.Find(x => x.Name.Contains("20DC")).Id;
                    }
                    else if (container.Description.Contains("Cont 40"))
                    {
                        containerId = containers.Find(x => x.Name.Contains("40HC")).Id;
                    }
                    else if (container.Description.Contains("Cont 45"))
                    {
                        containerId = containers.Find(x => x.Name.Contains("45HC")).Id;
                    }
                    else if (container.Description.Contains("Cont 50"))
                    {
                        containerId = containers.Find(x => x.Name.Contains("50DC")).Id;
                    }
                    var commodityValue = commodityValues.Where(x => x.BossId == item.BossId && x.CommodityId == item.CommodityId && x.ContainerId == containerId && (x.StartDate >= item.StartShip || item.StartShip == null)).FirstOrDefault();
                    if (commodityValue != null) { commodityValueOfTrans.Add(item.Id, commodityValue); }
                }
            }

            var tranIds = trans.Select(x => x.Id).ToList();
            var expenses = await db.Expense.Where(x => tranIds.Contains((int)x.TransportationId) && expenseTypeIds.Contains((int)x.ExpenseTypeId) && x.RequestChangeId == null && x.Active).ToListAsync();
            if (expenses == null)
            {
                return false;
            }
            var expenseIds = expenses.Select(x => x.Id).ToList();
            var checkRequests = await db.Expense.Where(x => expenseIds.Contains((int)x.RequestChangeId) && x.StatusId == (int)ApprovalStatusEnum.Approving && x.Active).ToListAsync();

            var extraInsuranceFeesRateDB = await db.MasterData.Where(x => x.Active == true && x.ParentId == 25374).ToListAsync();
            var insuranceFeesRateColdDB = await db.MasterData.Where(x => x.Id == 25391).FirstOrDefaultAsync();
            var containerTypeIdExpenses = expenses.Select(x => x.ContainerTypeId).ToList();
            var containerTypeExpenses = await db.MasterData.Where(x => containerTypeIdExpenses.Contains(x.Id)).ToListAsync();
            var containerTypeOfExpenses = new Dictionary<int, MasterData>();

            var insuranceFeesRates = await db.InsuranceFeesRate.Where(x => x.Active).ToListAsync();
            foreach (var item in expenses)
            {
                var container = containerTypeExpenses.Where(x => x.Id == item.ContainerTypeId).FirstOrDefault();
                containerTypeOfExpenses.Add(item.Id, container);
            }
            var querys = "";
            var queryExs = "";
            foreach (var tran in trans)
            {
                if (tran.RouteId != null || tran.ClosingId != null)
                {
                    var route = routes.Where(x => x.Id == tran.RouteId).FirstOrDefault();
                    var vendor = vendors.Where(x => x.Id == tran.ClosingId).FirstOrDefault();
                    if (route != null || vendor != null)
                    {
                        if (vendor != null && vendor.Name.ToLower().Contains("sà lan"))
                        {
                            tran.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("sà lan")).FirstOrDefault().Id;
                        }
                        else if (route != null && route.Name.ToLower().Contains("sắt"))
                        {
                            tran.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("sắt")).FirstOrDefault().Id;
                        }
                        else if (route != null && (route.Name.ToLower().Contains("bộ") || route.Name.ToLower().Contains("trucking vtqt")))
                        {
                            tran.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("bộ")).FirstOrDefault().Id;
                        }
                        else
                        {
                            tran.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("tàu")).FirstOrDefault().Id;
                        }
                    }
                }
                var expensesByTran = expenses.Where(x => x.TransportationId == tran.Id).ToList();
                foreach (var ex in expensesByTran)
                {
                    var check = checkRequests.Where(x => x.RequestChangeId == ex.Id).Any();
                    if (((tran.RouteId != ex.RouteId) ||
                        (tran.ShipId != ex.ShipId) ||
                        (tran.BossId != ex.BossId) ||
                        (tran.ReceivedId != ex.ReceivedId) ||
                        (tran.CommodityId != ex.CommodityId && ex.ExpenseTypeId != 15981) ||
                        (tran.TransportationTypeId != ex.TransportationTypeId) ||
                        (tran.ContainerTypeId != ex.ContainerTypeId) ||
                        (tran.Trip?.Trim() != ex.Trip?.Trim()) ||
                        (tran.ContainerNo?.Trim() != ex.ContainerNo?.Trim()) ||
                        (tran.SealNo?.Trim() != ex.SealNo?.Trim()) ||
                        (tran.Cont20 != ex.Cont20) ||
                        (tran.Cont40 != ex.Cont40) ||
                        (tran.Note2?.Trim() != ex.Notes?.Trim()) ||
                        (tran.YearText?.Trim() != ex.YearText?.Trim()) ||
                        (tran.MonthText?.Trim() != ex.MonthText?.Trim()) ||
                        (tran.ClosingDate != ex.StartShip && (ex.JourneyId == 12114 || ex.JourneyId == 16001)) ||
                        (tran.StartShip != ex.StartShip && (ex.JourneyId != 12114 && ex.JourneyId != 16001))) && check == false)
                    {
                        if (ex.IsPurchasedInsurance)
                        {
                            var history = new Expense();
                            history.CopyPropFrom(ex);
                            history.Id = 0;
                            history.StatusId = (int)ApprovalStatusEnum.New;
                            history.RequestChangeId = ex.Id;
                            ex.IsHasChange = true;

                            var stringPropNames = history.GetType().GetProperties().Where(x =>
                                    x.Name != nameof(Expense.Id) &&
                                    x.Name != nameof(Expense.FromDate) &&
                                    x.Name != nameof(Expense.ToDate) &&
                                    x.Name != nameof(Expense.TransportationIds) &&
                                    x.Name != nameof(Expense.Allotment) &&
                                    x.Name != nameof(Expense.Transportation)).Select(x => x.Name).ToList();
                            var queryIn = $"INSERT INTO {nameof(Expense)}({stringPropNames.Combine()}";
                            queryIn += $") VALUES (";
                            foreach (var prop in history.GetType().GetProperties())
                            {
                                if (prop.Name != nameof(Expense.Id) &&
                                   prop.Name != nameof(Expense.FromDate) &&
                                   prop.Name != nameof(Expense.ToDate) &&
                                   prop.Name != nameof(Expense.TransportationIds) &&
                                   prop.Name != nameof(Expense.Allotment) &&
                                   prop.Name != nameof(Expense.Transportation))
                                {
                                    if (prop.PropertyType.Name == nameof(DateTime) || prop.PropertyType.FullName.Contains(nameof(DateTime)))
                                    {
                                        queryIn += prop.GetValue(history) != null ? $"'{DateTime.Parse(prop.GetValue(history).ToString()).ToString("yyyy-MM-dd")}', " : "NULL, ";
                                    }
                                    else
                                    {
                                        queryIn += prop.GetValue(history) != null ? $"'{prop.GetValue(history).ToString()}', " : "NULL, ";
                                    }
                                }
                            }
                            queryIn = queryIn.Remove(queryIn.Length - 2);
                            queryIn += "); ";
                            queryExs += queryIn;
                        }
                        if ((tran.BossId != ex.BossId ||
                            tran.CommodityId != ex.CommodityId ||
                            tran.ContainerTypeId != ex.ContainerTypeId ||
                            tran.TransportationTypeId != ex.TransportationTypeId) && ex.IsPurchasedInsurance == false)
                        {
                            var containerExpense = containerTypeOfExpenses.GetValueOrDefault(ex.Id);
                            CommodityValue commodityValue = null;
                            if (ex.ExpenseTypeId == 15939)
                            {
                                commodityValue = commodityValueOfTrans.GetValueOrDefault(tran.Id);
                                if (commodityValue != null)
                                {
                                    ex.CommodityValue = commodityValue.TotalPrice;
                                    ex.JourneyId = commodityValue.JourneyId;
                                    ex.IsBought = commodityValue.IsBought;
                                    ex.IsWet = commodityValue.IsWet;
                                    ex.SteamingTerms = commodityValue.SteamingTerms;
                                    ex.BreakTerms = commodityValue.BreakTerms;
                                    ex.CustomerTypeId = commodityValue.CustomerTypeId;
                                    ex.CommodityValueNotes = commodityValue.Notes;
                                    CalcInsuranceFees(ex, false, insuranceFeesRates, extraInsuranceFeesRateDB, containerExpense, insuranceFeesRateColdDB);
                                }
                            }
                            else if (ex.ExpenseTypeId == 15981)
                            {
                                commodityValue = commodityValuesSOC.Where(x => x.ContainerId == containerTypeOfExpenses.GetValueOrDefault(ex.Id).Id).FirstOrDefault();
                                if (commodityValue != null)
                                {
                                    ex.CommodityValue = commodityValue.TotalPrice;
                                    ex.CustomerTypeId = commodityValue.CustomerTypeId;
                                    ex.CommodityValueNotes = commodityValue.Notes;
                                    CalcInsuranceFees(ex, true, insuranceFeesRates, extraInsuranceFeesRateDB, containerExpense, insuranceFeesRateColdDB);
                                }
                            }
                        }
                        if (tran.TransportationTypeId != ex.TransportationTypeId) { ex.TransportationTypeId = tran.TransportationTypeId; }
                        if (tran.BossId != ex.BossId) { ex.BossId = tran.BossId; }
                        if (tran.CommodityId != ex.CommodityId && ex.ExpenseTypeId != 15981) { ex.CommodityId = tran.CommodityId; }
                        if (tran.ContainerTypeId != ex.ContainerTypeId) { ex.ContainerTypeId = tran.ContainerTypeId; }
                        if (tran.RouteId != ex.RouteId) { ex.RouteId = tran.RouteId; }
                        if (tran.ShipId != ex.ShipId) { ex.ShipId = tran.ShipId; }
                        if (tran.ReceivedId != ex.ReceivedId) { ex.ReceivedId = tran.ReceivedId; }
                        if (tran.Trip?.Trim() != ex.Trip?.Trim()) { ex.Trip = tran.Trip; }
                        if (tran.ContainerNo?.Trim() != ex.ContainerNo?.Trim()) { ex.ContainerNo = tran.ContainerNo; }
                        if (tran.SealNo?.Trim() != ex.SealNo?.Trim()) { ex.SealNo = tran.SealNo; }
                        if (tran.Cont20 != ex.Cont20) { ex.Cont20 = tran.Cont20; }
                        if (tran.Cont40 != ex.Cont40) { ex.Cont40 = tran.Cont40; }
                        if (tran.Note2?.Trim() != ex.Notes?.Trim()) { ex.Notes = tran.Note2; }
                        if (tran.YearText?.Trim() != ex.YearText?.Trim()) { ex.YearText = tran.YearText; }
                        if (tran.MonthText?.Trim() != ex.MonthText?.Trim()) { ex.MonthText = tran.MonthText; }
                        if (tran.ClosingDate != ex.StartShip && (ex.JourneyId == 12114 || ex.JourneyId == 16001)) { ex.StartShip = tran.ClosingDate; }
                        if (tran.StartShip != ex.StartShip && (ex.JourneyId != 12114 && ex.JourneyId != 16001)) { ex.StartShip = tran.StartShip; }

                        var query = $"Update {nameof(Expense)} set ";
                        foreach (var prop in ex.GetType().GetProperties())
                        {
                            if (prop.Name != nameof(Expense.Id) &&
                               prop.Name != nameof(Expense.FromDate) &&
                               prop.Name != nameof(Expense.ToDate) &&
                               prop.Name != nameof(Expense.TransportationIds) &&
                               prop.Name != nameof(Expense.Allotment) &&
                               prop.Name != nameof(Expense.Transportation))
                            {
                                query += $"{prop.Name} = ";
                                if (prop.PropertyType.Name == nameof(DateTime) || prop.PropertyType.FullName.Contains(nameof(DateTime)))
                                {
                                    query += prop.GetValue(ex) != null ? $"'{DateTime.Parse(prop.GetValue(ex).ToString()).ToString("yyyy-MM-dd")}', " : "NULL, ";
                                }
                                else
                                {
                                    query += prop.GetValue(ex) != null ? $"'{prop.GetValue(ex).ToString()}', " : "NULL, ";
                                }
                            }
                        }
                        query = query.Remove(query.Length - 2);
                        query += $" where Id = {ex.Id}; ";
                        queryExs += query;
                    }
                }
                var insuranceFee = expenses.Where(x => x.TransportationId == tran.Id && x.IsPurchasedInsurance).ToList().Sum(x => x.TotalPriceAfterTax);
                if (tran.InsuranceFee != insuranceFee)
                {
                    var query = $"Update {nameof(Transportation)} set InsuranceFee = {insuranceFee} where Id = {tran.Id}; ";
                    querys += query;
                }
            }
            if (queryExs != null && queryExs != "") { await ExecSql(queryExs, "DISABLE TRIGGER ALL ON Expense;", "ENABLE TRIGGER ALL ON Expense;"); }
            if (querys != null && querys != "") { await ExecSql(querys, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;"); }
            return true;
        }


        [HttpPost("api/Expense/ExportCheckChange")]
        public async Task<string> ExportCheckChange([FromBody] List<int> expenseIds)
        {
            var expenses = await db.Expense.Where(x => expenseIds.Contains(x.Id)).ToListAsync();
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
            worksheet.Cell("B1").Value = $"BH cập nhật";
            worksheet.Cell("B1").Style.Alignment.WrapText = true;
            worksheet.Cell("B1").Style.Font.Bold = true;
            worksheet.Cell("B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("B1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("B1:C2").Column(1).Merge();
            worksheet.Cell("C1").Value = $"Đã chốt";
            worksheet.Cell("C1").Style.Alignment.WrapText = true;
            worksheet.Cell("C1").Style.Font.Bold = true;
            worksheet.Cell("C1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("C1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("C1:D2").Column(1).Merge();
            worksheet.Cell("D1").Value = $"Đã mua BH";
            worksheet.Cell("D1").Style.Alignment.WrapText = true;
            worksheet.Cell("D1").Style.Font.Bold = true;
            worksheet.Cell("D1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("D1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("D1:E2").Column(1).Merge();
            worksheet.Cell("E1").Value = $"Ngày mua BH";
            worksheet.Cell("E1").Style.Alignment.WrapText = true;
            worksheet.Cell("E1").Style.Font.Bold = true;
            worksheet.Cell("E1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("E1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("E1:F2").Column(1).Merge();
            worksheet.Cell("F1").Value = $"Loại vận chuyển";
            worksheet.Cell("F1").Style.Alignment.WrapText = true;
            worksheet.Cell("F1").Style.Font.Bold = true;
            worksheet.Cell("F1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("F1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("F1:G2").Column(1).Merge();
            worksheet.Cell("G1").Value = $"Hành trình BH";
            worksheet.Cell("G1").Style.Alignment.WrapText = true;
            worksheet.Cell("G1").Style.Font.Bold = true;
            worksheet.Cell("G1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("G1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("G1:H2").Column(1).Merge();
            worksheet.Cell("H1").Value = $"Tuyến vận chuyển";
            worksheet.Cell("H1").Style.Alignment.WrapText = true;
            worksheet.Cell("H1").Style.Font.Bold = true;
            worksheet.Cell("H1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("H1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("H1:I2").Column(1).Merge();
            worksheet.Cell("I1").Value = $"Tên tàu";
            worksheet.Cell("I1").Style.Alignment.WrapText = true;
            worksheet.Cell("I1").Style.Font.Bold = true;
            worksheet.Cell("I1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("I1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("I1:J2").Column(1).Merge();
            worksheet.Cell("J1").Value = $"Ngày đóng hàng/Ngày tàu chạy";
            worksheet.Cell("J1").Style.Alignment.WrapText = true;
            worksheet.Cell("J1").Style.Font.Bold = true;
            worksheet.Cell("J1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("J1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("J1:K2").Column(1).Merge();
            worksheet.Cell("K1").Value = $"Địa điểm nhận hàng";
            worksheet.Cell("K1").Style.Alignment.WrapText = true;
            worksheet.Cell("K1").Style.Font.Bold = true;
            worksheet.Cell("K1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("K1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("K1:L2").Column(1).Merge();
            worksheet.Cell("L1").Value = $"Chủ hàng";
            worksheet.Cell("L1").Style.Alignment.WrapText = true;
            worksheet.Cell("L1").Style.Font.Bold = true;
            worksheet.Cell("L1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("L1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("L1:M2").Column(1).Merge();
            worksheet.Cell("M1").Value = $"Nhân viên bán hàng";
            worksheet.Cell("M1").Style.Alignment.WrapText = true;
            worksheet.Cell("M1").Style.Font.Bold = true;
            worksheet.Cell("M1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("M1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("M1:N2").Column(1).Merge();
            worksheet.Cell("N1").Value = $"Vật tư hàng hóa";
            worksheet.Cell("N1").Style.Alignment.WrapText = true;
            worksheet.Cell("N1").Style.Font.Bold = true;
            worksheet.Cell("N1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("N1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("N1:O2").Column(1).Merge();
            worksheet.Cell("O1").Value = $"Loại container";
            worksheet.Cell("O1").Style.Alignment.WrapText = true;
            worksheet.Cell("O1").Style.Font.Bold = true;
            worksheet.Cell("O1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("O1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("O1:P2").Column(1).Merge();
            worksheet.Cell("P1").Value = $"Số chuyến";
            worksheet.Cell("P1").Style.Alignment.WrapText = true;
            worksheet.Cell("P1").Style.Font.Bold = true;
            worksheet.Cell("P1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("P1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("P1:Q2").Column(1).Merge();
            worksheet.Cell("Q1").Value = $"Số cont";
            worksheet.Cell("Q1").Style.Alignment.WrapText = true;
            worksheet.Cell("Q1").Style.Font.Bold = true;
            worksheet.Cell("Q1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("Q1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("Q1:R2").Column(1).Merge();
            worksheet.Cell("R1").Value = $"Số seal";
            worksheet.Cell("R1").Style.Alignment.WrapText = true;
            worksheet.Cell("R1").Style.Font.Bold = true;
            worksheet.Cell("R1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("R1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("R1:S2").Column(1).Merge();
            worksheet.Cell("S1").Value = $"Cont20";
            worksheet.Cell("S1").Style.Alignment.WrapText = true;
            worksheet.Cell("S1").Style.Font.Bold = true;
            worksheet.Cell("S1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("S1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("S1:T2").Column(1).Merge();
            worksheet.Cell("T1").Value = $"Cont40";
            worksheet.Cell("T1").Style.Alignment.WrapText = true;
            worksheet.Cell("T1").Style.Font.Bold = true;
            worksheet.Cell("T1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("T1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("T1:U2").Column(1).Merge();
            worksheet.Cell("U1").Value = $"GTHH";
            worksheet.Cell("U1").Style.Alignment.WrapText = true;
            worksheet.Cell("U1").Style.Font.Bold = true;
            worksheet.Cell("U1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("U1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("U1:V2").Column(1).Merge();
            worksheet.Cell("V1").Value = $"Ghi chú GTHH";
            worksheet.Cell("V1").Style.Alignment.WrapText = true;
            worksheet.Cell("V1").Style.Font.Bold = true;
            worksheet.Cell("V1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("V1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("V1:W2").Column(1).Merge();
            worksheet.Cell("W1").Value = $"Mua hộ BH";
            worksheet.Cell("W1").Style.Alignment.WrapText = true;
            worksheet.Cell("W1").Style.Font.Bold = true;
            worksheet.Cell("W1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("W1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("W1:X2").Column(1).Merge();
            worksheet.Cell("X1").Value = $"BH ướt";
            worksheet.Cell("X1").Style.Alignment.WrapText = true;
            worksheet.Cell("X1").Style.Font.Bold = true;
            worksheet.Cell("X1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("X1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("X1:Y2").Column(1).Merge();
            worksheet.Cell("Y1").Value = $"Hấp hơi";
            worksheet.Cell("Y1").Style.Alignment.WrapText = true;
            worksheet.Cell("Y1").Style.Font.Bold = true;
            worksheet.Cell("Y1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("Y1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("Y1:Z2").Column(1).Merge();
            worksheet.Cell("Z1").Value = $"Bể vỡ";
            worksheet.Cell("Z1").Style.Alignment.WrapText = true;
            worksheet.Cell("Z1").Style.Font.Bold = true;
            worksheet.Cell("Z1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("Z1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("Z1:AA2").Column(1).Merge();
            worksheet.Cell("AA1").Value = $"Tỷ lệ phí";
            worksheet.Cell("AA1").Style.Alignment.WrapText = true;
            worksheet.Cell("AA1").Style.Font.Bold = true;
            worksheet.Cell("AA1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AA1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AA1:AB2").Column(1).Merge();
            worksheet.Cell("AB1").Value = $"Phí bảo hiểm (Chưa VAT)";
            worksheet.Cell("AB1").Style.Alignment.WrapText = true;
            worksheet.Cell("AB1").Style.Font.Bold = true;
            worksheet.Cell("AB1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AB1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AB1:AC2").Column(1).Merge();
            worksheet.Cell("AC1").Value = $"VAT";
            worksheet.Cell("AC1").Style.Alignment.WrapText = true;
            worksheet.Cell("AC1").Style.Font.Bold = true;
            worksheet.Cell("AC1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AC1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AC1:AD2").Column(1).Merge();
            worksheet.Cell("AD1").Value = $"Phí bảo hiểm";
            worksheet.Cell("AD1").Style.Alignment.WrapText = true;
            worksheet.Cell("AD1").Style.Font.Bold = true;
            worksheet.Cell("AD1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AD1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AD1:AE2").Column(1).Merge();
            worksheet.Cell("AE1").Value = $"Yêu cầu chứng thư";
            worksheet.Cell("AE1").Style.Alignment.WrapText = true;
            worksheet.Cell("AE1").Style.Font.Bold = true;
            worksheet.Cell("AE1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AE1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AE1:AF2").Column(1).Merge();
            worksheet.Cell("AF1").Value = $"Ghi chú cont hàng";
            worksheet.Cell("AF1").Style.Alignment.WrapText = true;
            worksheet.Cell("AF1").Style.Font.Bold = true;
            worksheet.Cell("AF1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AF1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AF1:AG2").Column(1).Merge();
            worksheet.Cell("AG1").Value = $"Ghi chú phí BH";
            worksheet.Cell("AG1").Style.Alignment.WrapText = true;
            worksheet.Cell("AG1").Style.Font.Bold = true;
            worksheet.Cell("AG1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("AG1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range("AG1:AH2").Column(1).Merge();
            var ids = expenses.Select(x => x.Id).ToList();
            var sql = @$"select e.Id,
            e.IsApproveChange,
            e.IsClosing,
            e.IsPurchasedInsurance,
            e.DatePurchasedInsurance,
            m1.Name as TransportationType,
            m2.Name as Journey,
            r.Name as Route,
            s.Name as Ship,
            e.StartShip,
            l.Description as Received,
            v1.Name as Boss,
            u.FullName as Sale,
            m3.Description as Commodity,
            m4.Description as ContainerType,
            e.Trip,
            e.ContainerNo,
            e.SealNo,
            e.Cont20,
            e.Cont40,
            e.CommodityValue,
            e.CommodityValueNotes,
            e.IsBought,
            e.IsWet,
            e.SteamingTerms,
            e.BreakTerms,
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
            left join [User] u on e.SaleId = u.Id
            left join Location l on e.ReceivedId = l.Id
            where e.Id in ({ids.Combine()}) order by e.Id desc";
            var data = await ConverSqlToDataSet(sql);
            var start = 3;
            var expensePurchasedIds = expenses.Where(x => x.IsPurchasedInsurance).Select(x => x.Id).ToList();
            var expenseChanges = await db.Expense.Where(x => expensePurchasedIds.Contains((int)x.RequestChangeId)).ToListAsync();
            foreach (var item in data[0])
            {
                worksheet.Cell("A" + start).SetValue(start - 2);
                worksheet.Cell("B" + start).SetValue(item["IsApproveChange"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cell("C" + start).SetValue(item["IsClosing"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cell("D" + start).SetValue(item["IsPurchasedInsurance"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cell("E" + start).SetValue(item["DatePurchasedInsurance"] is null ? "" : DateTime.Parse(item[nameof(Expense.DatePurchasedInsurance)].ToString()));
                worksheet.Cell("F" + start).SetValue(item["TransportationType"] is null ? "" : item["TransportationType"].ToString());
                worksheet.Cell("G" + start).SetValue(item["Journey"] is null ? "" : item["Journey"].ToString());
                worksheet.Cell("H" + start).SetValue(item["Route"] is null ? "" : item["Route"].ToString());
                worksheet.Cell("I" + start).SetValue(item["Ship"] is null ? "" : item["Ship"].ToString());
                worksheet.Cell("J" + start).SetValue(item["StartShip"] is null ? "" : DateTime.Parse(item[nameof(Expense.StartShip)].ToString()));
                worksheet.Cell("K" + start).SetValue(item["Received"] is null ? "" : item["Received"].ToString());
                worksheet.Cell("L" + start).SetValue(item["Boss"] is null ? "" : item["Boss"].ToString());
                worksheet.Cell("M" + start).SetValue(item["Sale"] is null ? "" : item["Sale"].ToString());
                worksheet.Cell("N" + start).SetValue(item["Commodity"] is null ? "" : item["Commodity"].ToString());
                worksheet.Cell("O" + start).SetValue(item["ContainerType"] is null ? "" : item["ContainerType"].ToString());
                worksheet.Cell("P" + start).SetValue(item["Trip"] is null ? "" : item["Trip"].ToString());
                worksheet.Cell("Q" + start).SetValue(item["ContainerNo"] is null ? "" : item["ContainerNo"].ToString());
                worksheet.Cell("R" + start).SetValue(item["SealNo"] is null ? "" : item["SealNo"].ToString());
                worksheet.Cell("S" + start).SetValue(item["Cont20"] is null ? "" : decimal.Parse(item["Cont20"].ToString()));
                worksheet.Cell("T" + start).SetValue(item["Cont40"] is null ? "" : decimal.Parse(item["Cont40"].ToString()));
                worksheet.Cell("U" + start).SetValue(item["CommodityValue"] is null ? default(decimal) : decimal.Parse(item["CommodityValue"].ToString()));
                worksheet.Cell("U" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("V" + start).SetValue(item["CommodityValueNotes"] is null ? "" : item["CommodityValueNotes"].ToString());
                worksheet.Cell("W" + start).SetValue(item["IsBought"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cell("X" + start).SetValue(item["IsWet"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cell("Y" + start).SetValue(item["SteamingTerms"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cell("Z" + start).SetValue(item["BreakTerms"].ToString().Contains("False") ? "Không" : "Có");
                worksheet.Cell("AA" + start).SetValue(item["InsuranceFeeRate"] is null ? default(decimal) : decimal.Parse(item["InsuranceFeeRate"].ToString()));
                worksheet.Cell("AB" + start).SetValue(item["TotalPriceBeforeTax"] is null ? default(decimal) : decimal.Parse(item["TotalPriceBeforeTax"].ToString()));
                worksheet.Cell("AB" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("AC" + start).SetValue(item["Vat"] is null ? default(decimal) : decimal.Parse(item["Vat"].ToString()));
                worksheet.Cell("AC" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("AD" + start).SetValue(item["TotalPriceAfterTax"] is null ? default(decimal) : decimal.Parse(item["TotalPriceAfterTax"].ToString()));
                worksheet.Cell("AD" + start).Style.NumberFormat.Format = "#,##";
                worksheet.Cell("AE" + start).SetValue(item["CustomerType"] is null ? "" : item["CustomerType"].ToString());
                worksheet.Cell("AF" + start).SetValue(item["Notes"] is null ? "" : item["Notes"].ToString());
                worksheet.Cell("AG" + start).SetValue(item["NotesInsuranceFees"] is null ? "" : item["NotesInsuranceFees"].ToString());
                var id = int.Parse(item["Id"].ToString());
                var expenseChangesOfItem = expenseChanges.Where(x => x.RequestChangeId == id && x.StatusId == 2).ToList();
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
                            worksheet.Cell("C" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.IsPurchasedInsurance))
                        {
                            worksheet.Cell("D" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.DatePurchasedInsurance))
                        {
                            worksheet.Cell("E" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.TransportationTypeId))
                        {
                            worksheet.Cell("F" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.JourneyId))
                        {
                            worksheet.Cell("G" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.RouteId))
                        {
                            worksheet.Cell("H" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.ShipId))
                        {
                            worksheet.Cell("I" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.StartShip))
                        {
                            worksheet.Cell("J" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.ReceivedId))
                        {
                            worksheet.Cell("K" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.BossId))
                        {
                            worksheet.Cell("L" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.SaleId))
                        {
                            worksheet.Cell("M" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.CommodityId))
                        {
                            worksheet.Cell("N" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.ContainerTypeId))
                        {
                            worksheet.Cell("O" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.Trip))
                        {
                            worksheet.Cell("P" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.ContainerNo))
                        {
                            worksheet.Cell("Q" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.SealNo))
                        {
                            worksheet.Cell("R" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.Cont20))
                        {
                            worksheet.Cell("S" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.Cont40))
                        {
                            worksheet.Cell("T" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.CommodityValue))
                        {
                            worksheet.Cell("U" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.CommodityValueNotes))
                        {
                            worksheet.Cell("V" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.IsBought))
                        {
                            worksheet.Cell("W" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.IsWet))
                        {
                            worksheet.Cell("X" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.SteamingTerms))
                        {
                            worksheet.Cell("Y" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.BreakTerms))
                        {
                            worksheet.Cell("Z" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.InsuranceFeeRate))
                        {
                            worksheet.Cell("AA" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.TotalPriceBeforeTax))
                        {
                            worksheet.Cell("AB" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.Vat))
                        {
                            worksheet.Cell("AC" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.TotalPriceAfterTax))
                        {
                            worksheet.Cell("AD" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.CustomerTypeId))
                        {
                            worksheet.Cell("AE" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.Notes))
                        {
                            worksheet.Cell("AF" + start).Style.Font.FontColor = XLColor.Red;
                        }
                        else if (x == nameof(Expense.NotesInsuranceFees))
                        {
                            worksheet.Cell("AG" + start).Style.Font.FontColor = XLColor.Red;
                        }
                    });
                }
                worksheet.Row(start).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Row(start).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Row(start).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Row(start).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                start++;
            }
            var url = $"Phí bảo hiểm.xlsx";
            workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
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

        [HttpPost("api/Expense/UpdateDataForInsuranceFees")]
        public async Task<bool> UpdateDataForInsuranceFees([FromBody] Expense expense)
        {
            var expenses = await db.Expense.Where(x => (x.ExpenseTypeId == 15981 || x.ExpenseTypeId == 15939) && x.RequestChangeId == null && x.IsPurchasedInsurance == false && x.Active && x.StartShip >= DateTime.Parse("2023/01/01") && x.IsPurchasedInsurance == false).ToListAsync();
            var commodityValues = await db.CommodityValue.Where(x => x.Active).OrderBy(x => x.Id).ToListAsync();
            var containerTypes = await db.MasterData.Where(x => x.ParentId == 7565).ToListAsync();
            var containerTypeIds = containerTypes.ToDictionary(x => x.Id);
            var containerCommodityValues = await db.MasterData.Where(x => x.Name.Contains("40HC") || x.Name.Contains("20DC") || x.Name.Contains("45HC") || x.Name.Contains("50DC")).ToListAsync();
            var commodityValueOfExpenses = new Dictionary<int, CommodityValue>();
            var transportationTypes = await db.MasterData.Where(x => x.Active && x.ParentId == 11670).ToListAsync();
            var routes = await db.Route.Where(x => x.Active).ToListAsync();
            var insuranceFeesRates = await db.InsuranceFeesRate.Where(x => x.Active).ToListAsync();
            var extraInsuranceFeesRateDB = await db.MasterData.Where(x => x.Active == true && x.ParentId == 25374).ToListAsync();
            var insuranceFeesRateColdDB = await db.MasterData.Where(x => x.Id == 25391).FirstOrDefaultAsync();
            foreach (var item in expenses)
            {
                MasterData container = null;
                if (item.ContainerTypeId != null)
                {
                    container = containerTypeIds.GetValueOrDefault((int)item.ContainerTypeId);
                    var containerId = 0;
                    if (container.Description.Contains("Cont 20"))
                    {
                        containerId = containerCommodityValues.Find(x => x.Name.Contains("20DC")).Id;
                    }
                    else if (container.Description.Contains("Cont 40"))
                    {
                        containerId = containerCommodityValues.Find(x => x.Name.Contains("40HC")).Id;
                    }
                    else if (container.Description.Contains("Cont 45"))
                    {
                        containerId = containerCommodityValues.Find(x => x.Name.Contains("45HC")).Id;
                    }
                    else if (container.Description.Contains("Cont 50"))
                    {
                        containerId = containerCommodityValues.Find(x => x.Name.Contains("50DC")).Id;
                    }
                    var commodityValue = commodityValues.Where(x => x.BossId == item.BossId && x.CommodityId == item.CommodityId && x.ContainerId == containerId && (x.StartDate >= item.StartShip || item.StartShip == null)).FirstOrDefault();
                    if (commodityValue != null) { commodityValueOfExpenses.Add(item.Id, commodityValue); }
                }
            }
            foreach (var item in expenses)
            {
                var commodityValue = commodityValueOfExpenses.GetValueOrDefault(item.Id);
                var route = routes.Where(x => x.Id == item.RouteId).FirstOrDefault();
                if (item.TransportationTypeId == null)
                {
                    if (route.Name.ToLower().Contains("sắt"))
                    {
                        item.TransportationTypeId = transportationTypes.Where(x => x.Name.Contains("Sắt")).FirstOrDefault().Id;
                    }
                    else if (route.Name.ToLower().Contains("bộ") || route.Name.ToLower().Contains("trucking vtqt"))
                    {
                        item.TransportationTypeId = transportationTypes.Where(x => x.Name.Contains("Bộ")).FirstOrDefault().Id;
                    }
                    else
                    {
                        item.TransportationTypeId = transportationTypes.Where(x => x.Name.Contains("Tàu")).FirstOrDefault().Id;
                    }
                }
                if (item.TransportationTypeId == 11672 && route.Name.ToLower().Contains("bộ") == false)
                {
                    item.TransportationTypeId = 11673;
                }
                if (item.ExpenseTypeId == 15939 && commodityValue != null)
                {
                    item.CommodityValue = commodityValue.TotalPrice;
                    item.BreakTerms = commodityValue.BreakTerms;
                    item.SteamingTerms = commodityValue.SteamingTerms;
                    item.IsWet = commodityValue.IsWet;
                    item.IsBought = commodityValue.IsBought;
                    item.JourneyId = commodityValue.JourneyId;
                    item.CustomerTypeId = commodityValue.CustomerTypeId;
                    item.CommodityValueNotes = commodityValue.Notes;
                    if (item.JourneyId == null)
                    {
                        item.JourneyId = 12114;
                    }
                    var containerExpense = containerTypeIds.GetValueOrDefault((int)item.ContainerTypeId);
                    CalcInsuranceFees(item, false, insuranceFeesRates, extraInsuranceFeesRateDB, containerExpense, insuranceFeesRateColdDB);
                }
            }
            await db.SaveChangesAsync();
            return true;
        }
    }
}
