using Core.Enums;
using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class TaskNotificationController : TMSController<TaskNotification>
    {
        public TaskNotificationController(
            TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        [HttpPost("api/[Controller]/MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var updateCommand = string.Format($"Update [TaskNotification] set StatusId = {(int)TaskStateEnum.Read} " +
                $"where StatusId = {(int)TaskStateEnum.UnreadStatus} and AssignedId = {UserId}");
            await db.Database.ExecuteSqlRawAsync(updateCommand);
            return Ok(true);
        }

        public override async Task<ActionResult<TaskNotification>> CreateAsync([FromBody] TaskNotification entity)
        {
            var res = await base.CreateAsync(entity);
            await _taskService.NotifyAsync(new List<TaskNotification> { entity });
            return res;
        }

        public override async Task<ActionResult<TaskNotification>> UpdateAsync([FromBody] TaskNotification entity, string reasonOfChange = "")
        {
            entity.ClearReferences();
            SetAuditInfo(entity);
            db.Set<TaskNotification>().Update(entity);
            await db.SaveChangesAsync();
            return entity;
        }

        public override Task<OdataResult<TaskNotification>> Get(ODataQueryOptions<TaskNotification> options)
        {
            var query = db.TaskNotification.Where(x => x.AssignedId == UserId);
            return ApplyQuery(options, query);
        }

        public override async Task<List<TaskNotification>> BulkUpdateAsync([FromBody] List<TaskNotification> entities, string reasonOfChange)
        {
            entities = entities.Where(x => x.Id <= 0).ToList();
            var roleIds = entities.Where(x => x.AssignedId is null && x.RoleId.HasValue).Select(x => x.RoleId);
            var userRoles = await db.UserRole.Where(x => roleIds.Contains(x.RoleId)).ToListAsync();
            var assignedTasks = entities
                .Where(x => x.AssignedId is null && x.RoleId.HasValue)
                .Select(task => new
                {
                    Task = task,
                    UserIds = userRoles.Where(ur => ur.RoleId == task.RoleId).Select(x => x.UserId).Distinct()
                })
                .SelectMany(task =>
                {
                    return task.UserIds.Select(id =>
                    {
                        var newTask = new TaskNotification();
                        newTask.CopyPropFrom(task.Task);
                        newTask.AssignedId = id;
                        return newTask;
                    });
                }).ToList();
            var allAssignedTasks = assignedTasks.Union(entities.Where(x => x.AssignedId.HasValue)).ToList();
            var updatedTasks = await base.BulkUpdateAsync(allAssignedTasks, reasonOfChange);
            await _taskService.NotifyAsync(updatedTasks);
            return updatedTasks;
        }
    }
}
