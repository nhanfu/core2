using Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using Core.Models;
using Core.Websocket;
using Core.ViewModels;
using System.Text;

namespace Core.Services
{
    public class TaskService
    {
        private readonly UserService _userSvc;
        private readonly WebSocketService _socket;
        private static readonly JsonSerializerSettings _jsonSetting = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public TaskService(UserService userService, WebSocketService socket)
        {
            _userSvc = userService ?? throw new ArgumentNullException(nameof(userService));
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
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
            await _socket.SendMessageToUsersAsync([task.Message.ToId], JsonConvert.SerializeObject(task, _jsonSetting), null);
        }

        public async Task ChatGptSendToUser(MQEvent task)
        {
            await _socket.SendMessageToUsersAsync([task.Message.FromId], JsonConvert.SerializeObject(task, _jsonSetting), null);
        }

        private async Task SendMessageToUser(MQEvent task)
        {
            var tenantCode = _userSvc.TenantCode;
            var env = _userSvc.Env;
            var fcm = new FCMWrapper
            {
                To = $"/topics/{tenantCode}/{env}/U{task.Message.AssignedId:0000000}",
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

        private static async Task<Chat> GetChatGPTResponse(Chat entity)
        {
            var languageRules = new[]
            {
            new { Language = "javascript", Regex = @"```javascript([\s\S]+?)```", Replacement = "<pre><code class=\"language-javascript\">$1</code></pre>" },
            new { Language = "html", Regex = @"```html([\s\S]+?)```", Replacement = "<pre><code class=\"language-html\">$1</code></pre>" },
            new { Language = "csharp", Regex = @"```csharp([\s\S]+?)```", Replacement = "<pre><code class=\"language-csharp\">$1</code></pre>" },
            new { Language = "code", Regex = @"```([\s\S]+?)```", Replacement = "<pre><code>$1</code></pre>" }
        };

            var apiKey = "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";
            var endpoint = "https://api.openai.com/v1/chat/completions";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            var requestData = new ChatGptVM
            {
                model = "gpt-3.5-turbo",
                messages =
                    [
                        new ChatGptMessVM
                        {
                            role = "user",
                            content = entity.Context,
                            name = entity.FromId.ToString(),
                        }
                    ]
            };
            var jsonRequestData = JsonConvert.SerializeObject(requestData);
            var response = await httpClient.PostAsync(endpoint, new StringContent(jsonRequestData, Encoding.UTF8, "application/json"));
            var jsonResponseData = await response.Content.ReadAsStringAsync();
            var rs = JsonConvert.DeserializeObject<RsChatGpt>(jsonResponseData);
            var text = rs.choices.FirstOrDefault().message.content;
            foreach (var rule in languageRules)
            {
                var language = rule.Language;
                var regex = new System.Text.RegularExpressions.Regex(rule.Regex);
                var replacement = rule.Replacement;
                text = regex.Replace(text, replacement);
            }

            return new Chat()
            {
                FromId = entity.ToId,
                ToId = entity.FromId,
                Context = text,
                ConversationId = entity.ConversationId,
                IsSeft = true,
            };
        }

        internal async Task<Chat> Chat(Chat entity)
        {
            var patchMV = entity.MapToPatch();
            await _userSvc.SavePatch(patchMV);
            if (entity.ToId == 552.ToString())
            {
                var rs1 = await GetChatGPTResponse(entity);
                await _userSvc.SavePatch(rs1.MapToPatch());
                var chat = new MQEvent
                {
                    QueueName = entity.QueueName,
                    Message = rs1,
                    Id = Id.NewGuid(),
                };
                await SendChatToUser(chat);
            }
            else
            {
                var chat = new MQEvent
                {
                    QueueName = entity.QueueName,
                    Message = entity,
                    Id = Id.NewGuid(),
                };
                await SendChatToUser(chat);
            }
            return entity;
        }

        internal IEnumerable<User> GetUserActive()
        {
            var online = GetAll().Keys;
            var us = online.Select(x =>
            {
                var split = x.Split("/");
                return new User
                {
                    Id = split[0],
                    Recover = split[3],
                    Email = x
                };
            }).OrderBy(x => x.Id);
            return us;
        }
    }
}
