using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Hangfire;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;
using TMS.API.Models;
using TMS.API.Services;

namespace TMS.API.Controllers
{
    public class TransportationPlanController : TMSController<TransportationPlan>
    {
        private readonly HistoryContext hdb;
        private TransportationService _transportationService;
        public TransportationPlanController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor, HistoryContext historyContext, TransportationService transportationService) : base(context, entityService, httpContextAccessor)
        {
            hdb = historyContext;
            _transportationService = transportationService;
        }

        protected override IQueryable<TransportationPlan> GetQuery()
        {
            var qr = base.GetQuery();
            if (RoleIds.Contains(10))
            {
                qr =
                    from tranp in qr
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == tranp.BossId && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where tranp.InsertedBy == UserId
                        || policy != null || tranp.Id == _userSvc.VendorId || tranp.UserId == UserId
                    select tranp;
            }
            else if (RoleIds.Contains(17))
            {
                qr =
                    from tranp in qr
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == tranp.BossId && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where tranp.InsertedBy == UserId
                        || policy != null || tranp.Id == _userSvc.VendorId || tranp.UserId == UserId || (tranp.UserId == 78 && policy != null)
                    select tranp;
            }
            else if (RoleIds.Contains(43))
            {
                qr =
                    from tranp in qr
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == tranp.BossId && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where tranp.InsertedBy == UserId
                        || policy != null || tranp.Id == _userSvc.VendorId || tranp.UserId == UserId || tranp.UserId == 78
                    select tranp;
            }
            else if (RoleIds.Contains(25) || RoleIds.Contains(27))
            {
                qr = from tr in qr
                     join route in db.UserRoute.AsNoTracking()
                     on tr.RouteId equals route.RouteId
                     where route.UserId == UserId && route.TypeId == 25045
                     select tr;
            }
            return qr;
        }

        [HttpGet("api/[Controller]/CreateTransportation")]
        public async Task<List<TransportationPlan>> CreateTransportation([FromBody] List<int> selectedIds)
        {
            var selected = await db.TransportationPlan.AsNoTracking().Where(x => selectedIds.Contains(x.Id)).ToListAsync();
            if (selected.Nothing())
            {
                throw new ApiException("Vui lòng chọn kết hoạch vận chuyển");
            }
            if (selected.Any(x => x.ContainerTypeId is null))
            {
                throw new ApiException("Vui lòng chọn loại cont");
            }
            if (selected.Any(x => x.RouteId is null))
            {
                throw new ApiException("Vui lòng chọn tuyến đường");
            }
            if (selected.Any(x => x.BossId is null))
            {
                throw new ApiException("Vui lòng chọn chủ hàng");
            }
            if (selected.Any(x => x.CommodityId is null))
            {
                throw new ApiException("Vui lòng chọn vật tư hàng hóa");
            }
            if (selected.Any(x => x.TotalContainer is null || x.TotalContainer == 0))
            {
                throw new ApiException("Vui lòng nhập số lượng cont");
            }
            if (selected.Any(x => x.TotalContainerRemain == 0))
            {
                throw new ApiException("Có kế hoạch vận chuyển đã được lấy qua");
            }
            if (selected.Any(x => x.ReceivedId is null))
            {
                throw new ApiException("Vui lòng chọn địa chỉ nhận hàng");
            }
            if (selected.Any(x => x.ClosingDate is null))
            {
                throw new ApiException("Vui lòng chọn ngày đóng hàng");
            }
            if (selected.Any(x => x.Id < 0))
            {
                throw new ApiException("Vui lòng lưu trước khi vận chuyển");
            }
            var list = new List<TransportationPlan>();
            foreach (var item in selected)
            {
                if (await CheckContract(item))
                {
                    list.Add(item);
                }
                else
                {
                    list.Remove(item);
                }
            }
            return list;
        }

        public async Task<bool> CheckContract(TransportationPlan transportationPlan)
        {
            var transportationContract = await db.TransportationContract.FirstOrDefaultAsync(x => x.BossId == transportationPlan.BossId && x.StartDate >= transportationPlan.ClosingDate && x.EndDate <= transportationPlan.ClosingDate);
            return transportationContract is not null;
        }

        private void SetContainerTypes(List<TransportationPlan> transportationPlans, List<MasterData> containerTypes)
        {
            foreach (var item in transportationPlans)
            {
                var containerTypeName = containerTypes.FirstOrDefault(x => x.Id == (int)item.ContainerTypeId);
                if (containerTypeName.Description.Contains("Cont 20"))
                {
                    item.ActContainerId = containerTypes.FirstOrDefault(x => x.Name == "20DC").Id;
                }
                else if (containerTypeName.Description.Contains("Cont 40"))
                {
                    item.ActContainerId = containerTypes.FirstOrDefault(x => x.Name == "40HC").Id;
                }
                else if (containerTypeName.Description.Contains("Cont 45"))
                {
                    item.ActContainerId = containerTypes.FirstOrDefault(x => x.Name == "45HC").Id;
                }
                else if (containerTypeName.Description.Contains("Cont 50"))
                {
                    item.ActContainerId = containerTypes.FirstOrDefault(x => x.Name == "50DC").Id;
                }
            }
        }

        private async Task ActionCreateTransportation(List<TransportationPlan> transportationPlans)
        {
            var containerId = 0;
            var containerTypeIds = transportationPlans.Where(x => x.ContainerTypeId != null).Select(x => x.ContainerTypeId.Value).ToList();
            var containerTypeDb = await db.MasterData.AsNoTracking().Where(x => containerTypeIds.Contains(x.Id)).ToListAsync();
            var containers = await db.MasterData.AsNoTracking().Where(x => x.Active && x.ParentId == 7565).ToListAsync();
            var commodityTypeIds = transportationPlans.Where(x => x.CommodityId != null).Select(x => x.CommodityId.Value).ToList();
            var bossIds = transportationPlans.Where(x => x.BossId != null).Select(x => x.BossId.Value).ToList();
            var commodityValueDB = await db.CommodityValue.AsNoTracking().Where(x => x.Active && x.BossId != null && x.CommodityId != null && bossIds.Contains(x.BossId.Value) && commodityTypeIds.Contains(x.CommodityId.Value)).ToListAsync();
            var expenseTypeDB = await db.MasterData.AsNoTracking().FirstOrDefaultAsync(x => x.Active && x.ParentId == 7577 && x.Name.Contains("Bảo hiểm"));
            var masterDataDB = await db.MasterData.AsNoTracking().FirstOrDefaultAsync(x => x.Active && x.Id == 11685);
            var isWets = transportationPlans.Select(x => x.IsWet).Distinct().ToList();
            var isBoughts = transportationPlans.Select(x => x.IsBought).Distinct().ToList();
            var insuranceFeesRates = await db.InsuranceFeesRate.AsNoTracking().Where(x => x.Active && (x.IsSOC == null || !x.IsSOC.Value)).ToListAsync();
            var cont20Rs = containers.FirstOrDefault(x => x.Name == "20DC");
            var cont40Rs = containers.FirstOrDefault(x => x.Name == "40HC");
            var dir = containerTypeDb.ToDictionary(x => x.Id);
            SetContainerTypes(transportationPlans, containers);
            var rs = new List<Transportation>();
            var insuranceFeesRateColdDB = await db.MasterData.AsNoTracking().FirstOrDefaultAsync(x => x.Active && x.Id == 25391);
            var extraInsuranceFeesRateDB = await db.MasterData.AsNoTracking().Where(x => x.ParentId == 25374).ToListAsync();
            var transportationTypes = await db.MasterData.AsNoTracking().Where(x => x.Active && x.ParentId == 11670).ToListAsync();
            var routeIds = transportationPlans.Select(x => x.RouteId).ToList();
            var routes = await db.Route.AsNoTracking().Where(x => x.Active && routeIds.Contains(x.Id)).ToListAsync();
            foreach (var item in transportationPlans)
            {
                if (item.TransportationTypeId == null && item.RouteId != null)
                {
                    var route = routes.Where(x => x.Id == item.RouteId).FirstOrDefault();
                    if (route != null)
                    {
                        if (route.Name.ToLower().Contains("sắt"))
                        {
                            item.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("sắt")).FirstOrDefault().Id;
                        }
                        else if (route.Name.ToLower().Contains("bộ") || route.Name.ToLower().Contains("trucking vtqt"))
                        {
                            item.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("bộ")).FirstOrDefault().Id;
                        }
                        else
                        {
                            item.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("tàu")).FirstOrDefault().Id;
                        }
                    }
                }
                if (item.JourneyId == null)
                {
                    item.JourneyId = 12114;
                }
                var expense = new Expense();
                expense.CopyPropFrom(item);
                expense.Id = 0;
                expense.Quantity = 1;
                expense.ExpenseTypeId = expenseTypeDB.Id;
                expense.Vat = masterDataDB is null ? 0 : decimal.Parse(masterDataDB.Name);
                expense.SaleId = item.UserId;
                expense.Notes = "";
                if (expense.JourneyId == 12114 || expense.JourneyId == 16001)
                {
                    expense.StartShip = item.ClosingDate;
                }
                bool isSubRatio = false;
                if (((expense.IsWet || expense.SteamingTerms || expense.BreakTerms) && expense.IsBought == false) || (expense.IsBought && expense.IsWet))
                {
                    isSubRatio = true;
                }
                InsuranceFeesRate insuranceFeesRateDB = null;
                if (expense.IsBought)
                {
                    insuranceFeesRateDB = insuranceFeesRates.FirstOrDefault(x => x.TransportationTypeId == expense.TransportationTypeId
                    && x.JourneyId == expense.JourneyId
                    && x.IsBought == expense.IsBought
                    && x.IsSOC == false
                    && x.IsSubRatio == isSubRatio);
                }
                else
                {
                    insuranceFeesRateDB = insuranceFeesRates.FirstOrDefault(x => x.TransportationTypeId == expense.TransportationTypeId
                    && x.JourneyId == expense.JourneyId
                    && x.IsBought == expense.IsBought
                    && x.IsSOC == false);
                }
                if (insuranceFeesRateDB != null)
                {
                    var getContainerType = dir.GetValueOrDefault(expense.ContainerTypeId ?? 0);
                    if (getContainerType != null && getContainerType.Description.ToLower().Contains("lạnh") && insuranceFeesRateDB.TransportationTypeId == 11673 && insuranceFeesRateDB.JourneyId == 12114)
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
                }
                if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == true)
                {
                    expense.TotalPriceAfterTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
                    expense.TotalPriceBeforeTax = Math.Round(expense.TotalPriceAfterTax / (decimal)1.1, 0);
                }
                else if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == false)
                {
                    expense.TotalPriceBeforeTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
                    expense.TotalPriceAfterTax = expense.TotalPriceBeforeTax + Math.Round(expense.TotalPriceBeforeTax * expense.Vat / 100, 0);
                }
                var revenue = new Revenue();
                revenue.BossId = item.BossId;
                revenue.ContainerTypeId = item.ContainerTypeId;
                revenue.ClosingDate = item.ClosingDate;
                for (int i = 0; i < item.TotalContainerRemain; i++)
                {
                    var transportation = new Transportation();
                    transportation.CopyPropFrom(item, nameof(Transportation.Contact2Id));
                    transportation.Id = 0;
                    transportation.TransportationPlanId = item.Id;
                    transportation.Notes = null;
                    transportation.ClosingNotes = item.Notes;
                    transportation.ExportListId = VendorId;
                    SetAuditInfo(expense);
                    SetAuditInfo(revenue);
                    transportation.Expense.Add(expense);
                    transportation.Revenue.Add(revenue);
                    SetAuditInfo(transportation);
                    db.Add(transportation);
                }
                item.IsTransportation = true;
                var commodidtyValue = commodityValueDB.FirstOrDefault(x => x.BossId == item.BossId && x.CommodityId == item.CommodityId && x.ContainerId == item.ActContainerId);
                if (commodidtyValue == null && item.BossId != null && item.CommodityId != null && item.ContainerTypeId != null)
                {
                    var startDate1 = new DateTime(DateTime.Now.Year, 1, 1);
                    var endDate1 = new DateTime(DateTime.Now.Year, 6, 30);
                    var startDate2 = new DateTime(DateTime.Now.Year, 7, 1);
                    var endDate2 = new DateTime(DateTime.Now.Year, 12, 31);
                    var newCommodityValue = new CommodityValue();
                    newCommodityValue.CopyPropFrom(item);
                    newCommodityValue.Id = 0;
                    newCommodityValue.ContainerId = containerId;
                    newCommodityValue.TotalPrice = (decimal)item.CommodityValue;
                    newCommodityValue.SaleId = item.UserId;
                    newCommodityValue.StartDate = DateTime.Now.Date;
                    newCommodityValue.Notes = "";
                    newCommodityValue.Active = true;
                    newCommodityValue.InsertedDate = DateTime.Now.Date;
                    newCommodityValue.CreatedBy = item.InsertedBy;
                    if (DateTime.Now.Date >= startDate1 && DateTime.Now.Date <= endDate1)
                    {
                        newCommodityValue.EndDate = endDate1;
                    }
                    if (DateTime.Now.Date >= startDate2 && DateTime.Now.Date <= endDate2)
                    {
                        newCommodityValue.EndDate = endDate2;
                    }
                    SetAuditInfo(newCommodityValue);
                    db.Add(newCommodityValue);
                }
            }
            await db.SaveChangesAsync();
        }

        [HttpPost("api/[Controller]/CreateTransportation")]
        public async Task<bool> CreateTransportation([FromBody] List<TransportationPlan> transportationPlans)
        {
            try
            {
                await ActionCreateTransportation(transportationPlans);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        [HttpGet("api/[Controller]/GetByRole")]
        public Task<OdataResult<TransportationPlan>> UserClick(ODataQueryOptions<TransportationPlan> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(TransportationPlan).Name}]
                    where 1 = 1 and RequestChangeId is null";
            if (RoleIds.Contains(10))
            {
                sql += @$" and ((UserId = {UserId} or InsertedBy = {UserId} or User2Id = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
            }
            if (RoleIds.Contains(17))
            {
                sql += @$" and ((UserId = 78  and BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})) or User2Id = {UserId} or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
            }
            if (RoleIds.Contains(43))
            {
                sql += @$" and (UserId = 78 or User2Id = {UserId} or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
            }
            if (RoleIds.Contains(25) || RoleIds.Contains(27))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25045))";
            }
            var qr = db.TransportationPlan.FromSqlRaw(sql);
            return ApplyQuery(options, qr, sql: sql);
        }

        public override async Task<ActionResult<TransportationPlan>> PatchAsync([FromQuery] ODataQueryOptions<TransportationPlan> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
            var entity = await db.TransportationPlan.FindAsync(idInt);
            if (entity.IsTransportation && entity.RequestChangeId is null && !patch.Changes.Any(x => x.Field == nameof(TransportationPlan.TotalContainer) || x.Field == nameof(TransportationPlan.IsTransportation)))
            {
                throw new ApiException("Kế hoạch đã được sử dụng!") { StatusCode = HttpStatusCode.BadRequest };
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
                        if (RoleIds.Contains(43) || RoleIds.Contains(17) || RoleIds.Contains(10))
                        {
                            if (!patch.Changes.Any(x => x.Field == nameof(Transportation.UpdatedDate)))
                            {
                                patch.Changes.Add(new PatchUpdateDetail() { Field = nameof(Transportation.UpdatedDate), Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") });
                            }
                            if (!patch.Changes.Any(x => x.Field == nameof(Transportation.UpdatedBy)))
                            {
                                patch.Changes.Add(new PatchUpdateDetail() { Field = nameof(Transportation.UpdatedBy), Value = UserId.ToString() });
                            }
                        }
                        var updates = patch.Changes.Where(x => x.Field != IdField).ToList();
                        var update = updates.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
                        if (disableTrigger)
                        {
                            command.CommandText += $" DISABLE TRIGGER ALL ON [TransportationPlan];";
                        }
                        else
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [TransportationPlan];";
                        }
                        command.CommandText += $" UPDATE [TransportationPlan] SET {update.Combine()} WHERE Id = {idInt};";
                        if (disableTrigger)
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [TransportationPlan];";
                        }
                        foreach (var item in updates)
                        {
                            command.Parameters.AddWithValue($"@{item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                        }
                        command.ExecuteNonQuery();
                        transaction.Commit();
                        await db.Entry(entity).ReloadAsync();
                        BackgroundJob.Enqueue<TaskService>(x => x.SendMessageAllUserOtherMe(new WebSocketResponse<TransportationPlan>
                        {
                            EntityId = _entitySvc.GetEntity(typeof(TransportationPlan).Name).Id,
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

        public override async Task<ActionResult<bool>> Approve([FromBody] TransportationPlan entity, string reasonOfChange = "")
        {
            var rs = await base.Approve(entity, reasonOfChange);
            await db.Entry(entity).ReloadAsync();
            var oldEntity = await db.TransportationPlan.FindAsync(entity.RequestChangeId);
            var tempEntity = await db.TransportationPlan.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.RequestChangeId.Value);
            oldEntity.CopyPropFrom(entity, nameof(TransportationPlan.Id),
                nameof(TransportationPlan.RequestChangeId),
                nameof(TransportationPlan.InsertedDate),
                nameof(TransportationPlan.InsertedBy),
                nameof(TransportationPlan.UpdatedBy),
                nameof(TransportationPlan.UpdatedDate),
                nameof(TransportationPlan.StatusId),
                nameof(TransportationPlan.ReasonOfChange));
            entity.CopyPropFrom(tempEntity, nameof(TransportationPlan.Id),
                nameof(TransportationPlan.RequestChangeId),
                nameof(TransportationPlan.InsertedDate),
                nameof(TransportationPlan.InsertedBy),
                nameof(TransportationPlan.UpdatedBy),
                nameof(TransportationPlan.UpdatedDate),
                nameof(TransportationPlan.StatusId),
                nameof(TransportationPlan.ReasonOfChange));
            var user = await db.User.FindAsync(UserId);
            var taskNotification = new TaskNotification
            {
                Title = $"{user.FullName}",
                Description = $"Đã duyệt yêu cầu chỉnh sửa ạ",
                EntityId = _entitySvc.GetEntity(typeof(TransportationPlan).Name).Id,
                RecordId = oldEntity.Id,
                Attachment = "fal fa-check",
                AssignedId = entity.InsertedBy,
                StatusId = (int)TaskStateEnum.UnreadStatus,
                RemindBefore = 540,
                Deadline = DateTime.Now,
                InsertedBy = UserId,
                InsertedDate = DateTime.Now,
                Active = true
            };
            SetAuditInfo(taskNotification);
            db.AddRange(taskNotification);
            await db.SaveChangesAsync();
            BackgroundJob.Enqueue<TaskService>(x => x.SendMessageAllUserOtherMe(new WebSocketResponse<TransportationPlan>
            {
                EntityId = _entitySvc.GetEntity(typeof(TransportationPlan).Name).Id,
                TypeId = 1,
                Data = entity
            }, UserId));
            BackgroundJob.Enqueue<TaskService>(x => x.NotifyAndCountBadgeAsync(new List<TaskNotification> { taskNotification }));
            var transportations = await db.Transportation.Where(x => x.TransportationPlanId == oldEntity.Id).ToListAsync();
            foreach (var item in transportations)
            {
                var idInt = item.Id;
                var patch = new PatchUpdate()
                {
                    Changes = new List<PatchUpdateDetail>()
                    {
                        new PatchUpdateDetail()
                        {
                            Field = nameof(Transportation.Id),
                            Value = item.Id.ToString()
                        },
                        new PatchUpdateDetail()
                        {
                            Field = nameof(Transportation.UserId),
                            Value =oldEntity.UserId is null ? null : oldEntity.UserId.ToString()
                        },
                        new PatchUpdateDetail()
                        {
                            Field = nameof(Transportation.User2Id),
                            Value = oldEntity.User2Id is null ? null : oldEntity.User2Id.ToString()
                        },
                        new PatchUpdateDetail()
                        {
                            Field = nameof(Transportation.RouteId),
                            Value =oldEntity.RouteId is null ? null : oldEntity.RouteId.ToString()
                        },
                        new PatchUpdateDetail()
                        {
                            Field = nameof(Transportation.BossId),
                            Value = oldEntity.BossId is null ? null : oldEntity.BossId.ToString()
                        },
                        new PatchUpdateDetail()
                        {
                            Field = nameof(Transportation.ReceivedId),
                            Value = oldEntity.ReceivedId is null ? null : oldEntity.ReceivedId.ToString()
                        },
                        new PatchUpdateDetail()
                        {
                            Field = nameof(Transportation.ContainerTypeId),
                            Value = oldEntity.ContainerTypeId is null ? null : oldEntity.ContainerTypeId.ToString()
                        },
                        new PatchUpdateDetail()
                        {
                            Field = nameof(Transportation.CommodityId),
                            Value = oldEntity.CommodityId is null ? null : oldEntity.CommodityId.ToString()
                        },
                        new PatchUpdateDetail()
                        {
                            Field = nameof(Transportation.ClosingDate),
                            Value = oldEntity.ClosingDate is null ? null : oldEntity.ClosingDate.ToString()
                        }
                    }
                };
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
                            BackgroundJob.Enqueue<TaskService>(x => x.SendMessageAllUserOtherMe(new WebSocketResponse<Transportation>
                            {
                                EntityId = _entitySvc.GetEntity(typeof(Transportation).Name).Id,
                                TypeId = 1,
                                Data = item
                            }, UserId));
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                    }
                }
            }
            return rs;
        }

        public override async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> SubTotal([FromServices] IServiceProvider serviceProvider
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
                reportQuery += $@"select {sum}
                                  from ({sql}) as [{tablename}] where RequestChangeId is null {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
            }
            else
            {
                if (RoleIds.Contains(10))
                {
                    sql += @$" and ((UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
                }
                if (RoleIds.Contains(17))
                {
                    sql += @$" and ((UserId = 78 and BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})) or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
                }
                if (RoleIds.Contains(43))
                {
                    sql += @$" and (UserId = 78 or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
                }
                if (RoleIds.Contains(25) || RoleIds.Contains(27))
                {
                    sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25045))";
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

        public override async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ViewSumary([FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config, [FromBody] string sum, [FromQuery] string group, [FromQuery] string tablename, [FromQuery] string refname, [FromQuery] string formatsumary, [FromQuery] string orderby, [FromQuery] string sql, [FromQuery] string where)
        {
            var connectionStr = _config.GetConnectionString("Default");
            using var con = new SqlConnection(connectionStr);
            var reportQuery = string.Empty;
            if (!sql.IsNullOrWhiteSpace())
            {
                reportQuery += $@"select {group},{formatsumary} as TotalRecord,{sum}
                                  from ({sql})   as [{tablename}] where RequestChangeId is null {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
            }
            else
            {
                reportQuery += $@"select {group},{formatsumary} as TotalRecord,{sum}
                                 from [{tablename}]
                                 where RequestChangeId is null and 1 = 1 {(where.IsNullOrWhiteSpace() ? $"" : $"and {where}")}";
                if (RoleIds.Contains(10))
                {
                    sql += @$" and ((UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
                }
                if (RoleIds.Contains(17))
                {
                    sql += @$" and ((UserId = 78 and BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})) or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
                }
                if (RoleIds.Contains(43))
                {
                    sql += @$" and (UserId = 78 or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
                }
                if (RoleIds.Contains(25) || RoleIds.Contains(27))
                {
                    sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25045))";
                }
            }
            reportQuery += $@" group by {group}
                                 order by {formatsumary} {orderby}";
            if (!refname.IsNullOrEmpty())
            {
                if (!sql.IsNullOrWhiteSpace())
                {
                    reportQuery += $@" select *
                                 from [{refname}] 
                                 where Id in (select distinct {group}
                                 from ({sql})  as [{tablename}] where RequestChangeId is null {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")})";
                }
                else
                {
                    reportQuery += $@" select *
                                 from [{refname}] 
                                 where Id in (select {group}
                                              from [{tablename}]
                                 where RequestChangeId is null {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
                    if (RoleIds.Contains(10))
                    {
                        sql += @$" and ((UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
                    }
                    if (RoleIds.Contains(17))
                    {
                        sql += @$" and ((UserId = 78 and BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})) or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
                    }
                    if (RoleIds.Contains(43))
                    {
                        sql += @$" and (UserId = 78 or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
                    }
                    if (RoleIds.Contains(25) || RoleIds.Contains(27))
                    {
                        sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25045))";
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
            var check = await db.TransportationPlan.AnyAsync(x => ids.Contains(x.Id) && x.TotalContainerUsing > 0);
            if (check)
            {
                throw new ApiException("Dữ liệu đã được lấy qua danh sách, bạn không thể xóa!") { StatusCode = HttpStatusCode.BadRequest };
            }
            try
            {
                var deleteCommand = $"delete from [{typeof(TransportationPlan).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteCommand);
                return true;
            }
            catch
            {
                throw new ApiException("Không thể xóa dữ liệu!") { StatusCode = HttpStatusCode.BadRequest };
            }
        }

        public override async Task<ActionResult<TransportationPlan>> UpdateAsync([FromBody] TransportationPlan entity, string reasonOfChange = "")
        {
            var oldEntity = await db.TransportationPlan.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.Id);
            if (oldEntity.IsTransportation && oldEntity.RequestChangeId is null)
            {
                throw new ApiException("Kế hoạch đã được sử dụng!") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await base.UpdateAsync(entity, reasonOfChange);
        }

        public override async Task<ActionResult<bool>> RequestApprove([FromBody] TransportationPlan entity)
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
            var oldEntity = await db.TransportationPlan.FindAsync(entity.RequestChangeId);
            await db.Entry(oldEntity).ReloadAsync();
            var listUser = await GetApprovalUsers(entity, matchApprovalConfig);
            if (listUser.HasElement())
            {
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = listUser.Select(user => new TaskNotification
                {
                    Title = $"{currentUser.FullName}",
                    Description = entity.ReasonOfChange.IsNullOrWhiteSpace() ? $"Đã gửi yêu chỉnh sửa ạ lý do: " : entity.ReasonOfChange,
                    EntityId = _entitySvc.GetEntity(typeof(TransportationPlan).Name).Id,
                    RecordId = oldEntity.Id,
                    Attachment = "fal fa-paper-plane",
                    AssignedId = user.Id,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                    InsertedBy = UserId,
                    InsertedDate = DateTime.Now,
                    Active = true
                });
                db.AddRange(tasks);
                await db.SaveChangesAsync();
                BackgroundJob.Enqueue<TaskService>(x => x.NotifyAndCountBadgeAsync(tasks));
            }
            return true;
        }
    }
}
