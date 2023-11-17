using Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using Core.Models;
using Core.Websocket;

namespace Core.Services
{
    public class TaskService
    {
        private readonly CoreContext db;
        private readonly UserService _userService;
        private readonly WebSocketService _socket;
        private JsonSerializerSettings _jsonSetting;

        public TaskService(UserService userService, CoreContext db, WebSocketService _socket)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            this._socket = _socket ?? throw new ArgumentNullException(nameof(_socket));
            _jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };
        }

        public async Task NotifyAsync(IEnumerable<TaskNotification> entities, string queueName)
        {
            await entities
                .Where(x => x.AssignedId.HasAnyChar())
                .Select(x => new MQEvent
                {
                    QueueName = queueName,
                    Id = Id.NewGuid(),
                    Message = x
                })
            .ForEachAsync(SendMessageToUser);
        }

        public async Task SendChatToUser(MQEvent task)
        {
            await _socket.SendMessageToUsersAsync(new List<string>() { task.Message.ToId }, JsonConvert.SerializeObject(task, _jsonSetting), null);
        }

        public async Task ChatGptSendToUser(MQEvent task)
        {
            await _socket.SendMessageToUsersAsync(new List<string>() { task.Message.FromId }, JsonConvert.SerializeObject(task, _jsonSetting), null);
        }

        private async Task SendMessageToUser(MQEvent task)
        {
            var system = _userService.System;
            var tenantCode = _userService.TenantCode;
            var env = _userService.Env;
            var fcm = new FCMWrapper
            {
                To = $"/topics/{system}/{tenantCode}/{env}/U{task.Message.AssignedId:0000000}",
                Data = new FCMData
                {
                    Title = task.Message.Title,
                    Body = task.Message.Description,
                },
                Notification = new FCMNotification
                {
                    Title = task.Message.Title,
                    Body = task.Message.Description,
                    ClickAction = "com.softek.tms.push.background.MESSAGING_EVENT"
                }
            };
            await _socket.SendMessageToUsersAsync(new List<string>() { task.Message.AssignedId }, JsonConvert.SerializeObject(task, _jsonSetting), fcm.ToJson());
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
        {
            return _socket.GetAll();
        }

        public async Task SendMessageSocket(string socket, TaskNotification task, string queueName)
        {
            var entity = new MQEvent
            {
                QueueName = queueName,
                Id = Id.NewGuid(),
                Message = task
            };
            await _socket.SendMessageToSocketAsync(socket, entity.ToJson());
        }

        public async Task SendMessageToSubscribers(object task, string queueName)
        {
            await _socket.SendMessageToSubscribers(JsonConvert.SerializeObject(task, _jsonSetting), queueName);
        }
    }
}
