using Core.Enums;
using Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Management.Sdk.Differencing;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.Websocket;

namespace TMS.API.Services
{
    public class TaskService
    {
        private readonly TMSContext db;
        private readonly UserService _userService;
        private readonly RealtimeService _fcmSvc;
        protected readonly EntityService _entitySvc;

        public TaskService(UserService userService, TMSContext db, RealtimeService notificationService, EntityService entityService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            _fcmSvc = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _entitySvc = entityService;
        }

        public async Task NotifyAsync(IEnumerable<TaskNotification> entities)
        {
            await entities
                .Where(x => x.AssignedId.HasValue)
                .Select(x => new WebSocketResponse<TaskNotification>
                {
                    EntityId = _entitySvc.GetEntity(nameof(TaskNotification))?.Id ?? 0,
                    TypeId = 3,
                    Data = x
                })
            .ForEachAsync(SendMessageToUser);
        }

        public async Task NotifyAndCountBadgeAsync(IEnumerable<TaskNotification> entities)
        {
            await entities
                .Where(x => x.AssignedId.HasValue)
                .Select(x => new WebSocketResponse<TaskNotification>
                {
                    EntityId = x.EntityId.Value,
                    TypeId = 4,
                    Data = x
                })
            .ForEachAsync(SendMessageToUser);
        }

        public async Task SendChatToUser(WebSocketResponse<Chat> task)
        {
            await _fcmSvc.SendMessageToUsersAsync(new List<int>() { task.Data.ToId.Value }, JsonConvert.SerializeObject(task, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            }), null);
        }

        public async Task ChatGptSendToUser(WebSocketResponse<Chat> task)
        {
            await _fcmSvc.SendMessageToUsersAsync(new List<int>() { task.Data.FromId.Value }, JsonConvert.SerializeObject(task, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            }), null);
        }

        private async Task SendMessageToUser(WebSocketResponse<TaskNotification> task)
        {
            var tenantCode = _userService.TenantCode;
            var fcm = new FCMWrapper
            {
                To = $"/topics/{tenantCode}U{task.Data.AssignedId:0000000}",
                Data = new FCMData
                {
                    Title = task.Data.Title,
                    Body = task.Data.Description,
                },
                Notification = new FCMNotification
                {
                    Title = task.Data.Title,
                    Body = task.Data.Description,
                    ClickAction = "com.softek.tms.push.background.MESSAGING_EVENT"
                }
            };
            await _fcmSvc.SendMessageToUsersAsync(new List<int>() { task.Data.AssignedId.Value }, JsonConvert.SerializeObject(task, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            }), fcm.ToJson());
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
        {
            return _fcmSvc.GetAll();
        }

        public async Task SendMessageSocket(string socket, TaskNotification task)
        {
            var entity = new WebSocketResponse<TaskNotification>
            {
                EntityId = _entitySvc.GetEntity(nameof(User))?.Id ?? 0,
                TypeId = 3,
                Data = task
            };
            await _fcmSvc.SendMessageToSocketAsync(socket, entity.ToJson());
        }

        public async Task SendMessageAllUser(object task)
        {
            await _fcmSvc.SendMessageToAll(JsonConvert.SerializeObject(task, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            }));
        }

        public async Task SendMessageAllUserOtherMe(object task, int UserId)
        {
            await _fcmSvc.SendMessageToAllOtherMe(JsonConvert.SerializeObject(task, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            }), UserId);
        }
    }
}
