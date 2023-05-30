using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using TMS.API.Models;
using TMS.API.Services;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

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
            if (entity.IsTransportation && entity.RequestChangeId is null && !patch.Changes.Any(x => x.Field == nameof(TransportationPlan.TotalContainer)))
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
            };
            SetAuditInfo(taskNotification);
            db.AddRange(taskNotification);
            await db.SaveChangesAsync();
            await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            var transportations = await db.Transportation.AsNoTracking().Where(x => x.TransportationPlanId == oldEntity.Id).ToListAsync();
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
                            command.CommandText += " " + _transportationService.Transportation_ReturnEmptyId(patch, idInt);
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
                            command.ExecuteNonQuery();
                            transaction.Commit();
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

        public override Task<ActionResult<TransportationPlan>> UpdateAsync([FromBody] TransportationPlan entity, string reasonOfChange = "")
        {
            if (entity.IsTransportation && entity.RequestChangeId is null)
            {
                throw new ApiException("Kế hoạch đã được sử dụng!") { StatusCode = HttpStatusCode.BadRequest };
            }
            return base.UpdateAsync(entity, reasonOfChange);
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
                });
                SetAuditInfo(tasks);
                db.AddRange(tasks);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(tasks);
            }
            return true;
        }
    }
}
