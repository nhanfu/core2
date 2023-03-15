using Core.Exceptions;
using Core.Enums;
using Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.AspNet.OData.Query;
using Core.ViewModels;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace TMS.API.Controllers
{
    public class FreightRateController : TMSController<FreightRate>
    {
        public FreightRateController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public override async Task<ActionResult<FreightRate>> PatchAsync([FromQuery] ODataQueryOptions<FreightRate> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
            var entity = await db.FreightRate.FindAsync(idInt);
            var oldEntity = await db.FreightRate.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idInt);
            if (oldEntity.IsClosing && entity.IsClosing)
            {
                throw new ApiException("Biểu giá CVC này đã được khóa. Vui lòng tạo yêu cầu mở khóa để được cập nhật.") { StatusCode = HttpStatusCode.BadRequest };
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
                            command.CommandText += $" DISABLE TRIGGER ALL ON [{nameof(FreightRate)}];";
                        }
                        else
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(FreightRate)}];";
                        }
                        command.CommandText += $" UPDATE [{nameof(FreightRate)}] SET {update.Combine()} WHERE Id = {idInt};";
                        //
                        if (disableTrigger)
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [{nameof(FreightRate)}];";
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

        protected override IQueryable<FreightRate> GetQuery()
        {
            var rs = base.GetQuery();
            //Sale
            if (AllRoleIds.Contains(10))
            {
                //Biểu giá CVC
                rs =
                from fr in db.FreightRate
                from policy in db.FeaturePolicy
                    .Where(x => x.RecordId == fr.Id && x.EntityId == _entitySvc.GetEntity(nameof(FreightRate)).Id && x.CanRead)
                    .Where(x => x.InsertedBy == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                    .DefaultIfEmpty()
                where fr.InsertedBy == _userSvc.UserId
                    || policy != null
                select fr;
            }
            return rs;
        }

        [HttpPost("api/FreightRate/RequestUnLock")]
        public async Task RequestUnLock([FromBody] FreightRate freightRate)
        {
            var entityType = _entitySvc.GetEntity(typeof(FreightRate).Name);
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
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = listUser.Select(user => new TaskNotification
                {
                    Title = $"{currentUser.FullName}",
                    Description = $"Đã gửi yêu cầu mở khóa",
                    EntityId = _entitySvc.GetEntity(typeof(FreightRate).Name).Id,
                    RecordId = freightRate.Id,
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
