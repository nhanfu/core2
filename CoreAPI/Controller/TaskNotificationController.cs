using Core.Enums;
using Core.Extensions;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using Core.Models;
using Core.ViewModels;

namespace Core.Controllers
{
    public class TaskNotificationController : TMSController<TaskNotification>
    {
        public TaskNotificationController(
            TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [HttpPost("api/[Controller]/SendRequest")]
        public async Task<IActionResult> MarkAllAsRead([FromBody] TaskNotification entity)
        {
            var listUser = await db.User.Where(x => x.Id != UserId).ToListAsync();
            if (listUser.HasElement())
            {
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = listUser.Select(user => new TaskNotification
                {
                    Title = entity.Title,
                    Description = entity.Description,
                    EntityId = entity.EntityId,
                    RecordId = entity.RecordId,
                    Attachment = entity.Attachment,
                    AssignedId = user.Id,
                    StatusId = ((int)TaskStateEnum.UnreadStatus).ToString(),
                    RemindBefore = 540,
                    Deadline = entity.Deadline,
                    Active = true,
                    InsertedBy = UserId,
                    InsertedDate = DateTimeOffset.Now
                });
                db.AddRange(tasks);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(tasks);
            }
            return Ok(true);
        }

        [HttpGet("api/[Controller]/SendChat")]
        public async Task<string> SendChat([FromBody] string chat)
        {
            var apiKey = "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";
            var endpoint = "https://api.openai.com/v1/chat/completions";
            string response = await GetChatGPTResponse(apiKey, endpoint, chat);
            return response;
        }

        private async Task<string> GetChatGPTResponse(string apiKey, string endpoint, string input)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var requestData = new ChatGptVM
                {
                    model = "gpt-3.5-turbo",
                    messages = new List<ChatGptMessVM>()
                    {
                        new ChatGptMessVM
                        {
                            role = "user",
                            content = "Tôi muốn học c#",
                            name = "1212121",
                        }
                    }
                };
                var jsonRequestData = JsonConvert.SerializeObject(requestData);
                var response = await httpClient.PostAsync(endpoint, new StringContent(jsonRequestData, Encoding.UTF8, "application/json"));
                var jsonResponseData = await response.Content.ReadAsStringAsync();
                return jsonResponseData;
            }
        }

        [HttpPost("api/[Controller]/MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var updateCommand = string.Format($"Update [TaskNotification] set StatusId = {(int)TaskStateEnum.Read} " +
                $"where StatusId = {(int)TaskStateEnum.UnreadStatus} and AssignedId = {UserId}");
            await db.Database.ExecuteSqlRawAsync(updateCommand);
            return Ok(true);
        }

        [HttpPost("api/[Controller]/GetUserActive")]
        public async Task<List<User>> GetUserActive()
        {
            var online = _taskService.GetAll().ToList();
            var us = online.Select(x =>
            {
                var split = x.Key.Split("/");
                return new User
                {
                    Id = split[0],
                    Recover = split[3],
                    Email = x.Key
                };
            }).OrderBy(x => x.Id).ToList();
            var ids = us.Select(x => x.Id).Distinct().ToList();
            var user = await db.User.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
            us.ForEach(x =>
            {
                var u = user.GetValueOrDefault(x.Id);
                x.CopyPropFrom(u, nameof(u.Recover), nameof(u.Email));
            });
            return us;
        }

        [HttpPost("api/[Controller]/KickOut")]
        public async Task<IActionResult> GetUserActive([FromBody] string token)
        {
            var task = new TaskNotification()
            {
                Title = $"Kick",
                Description = $"Kick",
                EntityId = _entitySvc.GetEntity(nameof(User)).Id,
                RecordId = null,
                Attachment = "fal fa-paper-plane",
                StatusId = ((int)TaskStateEnum.UnreadStatus).ToString(),
                RemindBefore = 540,
                Deadline = DateTimeOffset.Now,
            };
            SetAuditInfo(task);
            await _taskService.SendMessageSocket(token, task);
            return Ok(task);
        }

        public override async Task<ActionResult<TaskNotification>> CreateAsync([FromBody] TaskNotification entity)
        {
            var res = await base.CreateAsync(entity);
            await _taskService.NotifyAsync(new List<TaskNotification> { entity });
            return res;
        }

        public override async Task<ActionResult<TaskNotification>> UpdateAsync([FromBody] TaskNotification entity, string reasonOfChange = "")
        {
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
            entities = entities.Where(x => x.Id == null).ToList();
            var roleIds = entities.Where(x => x.AssignedId is null && x.RoleId != null).Select(x => x.RoleId);
            var userRoles = await db.UserRole.Where(x => roleIds.Contains(x.RoleId)).ToListAsync();
            var assignedTasks = entities
                .Where(x => x.AssignedId is null && x.RoleId != null)
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
            var allAssignedTasks = assignedTasks.Union(entities.Where(x => x.AssignedId != null)).ToList();
            var updatedTasks = await base.BulkUpdateAsync(allAssignedTasks, reasonOfChange);
            await _taskService.NotifyAsync(updatedTasks);
            return updatedTasks;
        }
    }
}
