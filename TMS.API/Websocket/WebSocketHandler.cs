using Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TMS.API.Websocket
{
    public class FCMWrapper
    {
        public string Condition { get; set; }
        public string To { get; set; }
        public FCMData Data { get; set; }
        public FCMNotification Notification { get; set; }
    }

    public class FCMData
    {
        public int Badge { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string EntityId { get; set; }
        public string RecordId { get; set; }
    }

    public class FCMNotification
    {
        public int Badge { get; set; }
        public string Sound { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string ClickAction { get; set; }
    }

    public abstract class WebSocketHandler
    {
        protected ConnectionManager WebSocketConnectionManager { get; set; }
        private readonly string FCM_API_KEY;
        private readonly string FCM_SENDER_ID;
        private ILogger<WebSocketHandler> _logger;

        public WebSocketHandler(ConnectionManager webSocketConnectionManager, IConfiguration configuration, ILogger<WebSocketHandler> logger)
        {
            WebSocketConnectionManager = webSocketConnectionManager;
            _logger = logger;
            FCM_API_KEY = configuration["FCM_API_KEY"];
            FCM_SENDER_ID = configuration["FCM_SENDER_ID"];
            _logger.LogDebug(FCM_API_KEY);
            _logger.LogDebug(FCM_SENDER_ID);
        }

        public virtual Task OnConnected(WebSocket socket, int userId, List<int> roleIds, string ip)
        {
            WebSocketConnectionManager.AddSocket(socket, userId, roleIds, ip);
            return Task.CompletedTask;
        }

        public virtual async Task OnDisconnected(WebSocket socket)
        {
            await WebSocketConnectionManager.RemoveSocket(WebSocketConnectionManager.GetId(socket));
        }

        public async Task SendMessageAsync(WebSocket socket, string message)
        {
            if (socket.State != WebSocketState.Open)
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(buffer: new ArraySegment<byte>(
                    array: bytes,
                    offset: 0,
                    count: bytes.Length),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None);
        }

        public async Task SendFCMNotfication(string message)
        {
            if (message is null)
            {
                return;
            }

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://fcm.googleapis.com/fcm/send"));
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + FCM_API_KEY);
            request.Headers.TryAddWithoutValidation("Sender", "id=" + FCM_SENDER_ID);
            request.Content = new StringContent(message, Encoding.UTF8, "application/json");
            var res = await client.SendAsync(request);
            await res.Content.ReadAsStringAsync();
        }

        public async Task SendMessageToAll(string message)
        {
            var users = WebSocketConnectionManager.GetAll();
            foreach (var pair in users)
            {
                if (pair.Value.State == WebSocketState.Open)
                {
                    await SendMessageAsync(pair.Value, message);
                }
            }
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
        {
            return WebSocketConnectionManager.GetAll();
        }

        public Task SendMessageToUserAsync(int userId, string message, string fcm = null)
        {
            var userGroup = WebSocketConnectionManager.GetAll()
                .Where(x => userId == x.Key.Split("/").FirstOrDefault().TryParseInt());
            return NotifyUserGroup(message, userGroup, fcm);
        }

        public Task SendMessageToUsersAsync(List<int> userIds, string message, string fcm = null)
        {
            var userGroup = WebSocketConnectionManager.GetAll()
                .Where(x => userIds.Contains(x.Key.Split("/").FirstOrDefault().TryParseInt() ?? 0));
            return NotifyUserGroup(message, userGroup, fcm);
        }

        public async Task SendMessageToSocketAsync(string token, string message, string fcm = null)
        {
            var pair = WebSocketConnectionManager.GetAll()
                .FirstOrDefault(x => x.Key == token);
            var fcmTask = SendFCMNotfication(fcm);
            if (pair.Value.State != WebSocketState.Open)
            {
                return;
            }
            await SendMessageAsync(pair.Value, message);
        }

        public Task SendMessageToUsersAsync(int userId, string message, string fcm = null)
        {
            return SendMessageToUsersAsync(new List<int> { userId }, message, fcm);
        }

        private async Task NotifyUserGroup(string message, IEnumerable<KeyValuePair<string, WebSocket>> userGroup, string fcm = null)
        {
            var fcmTask = SendFCMNotfication(fcm);
            var realtimeTasks = userGroup.Select(pair =>
            {
                if (pair.Value.State != WebSocketState.Open)
                {
                    return Task.CompletedTask;
                }

                return SendMessageAsync(pair.Value, message);
            }).ToList();
            realtimeTasks.Add(fcmTask);
            await Task.WhenAll(realtimeTasks);
        }

        public async Task RealtimeMessageToRoleAsync(int roleId, string message, string fcm = null)
        {
            if (roleId == 0 || message.IsNullOrWhiteSpace())
            {
                return;
            }

            var userGroup = WebSocketConnectionManager.GetAll()
                .Where(x =>
                {
                    var keys = x.Key.Split("/");
                    if (keys.Length < 2)
                    {
                        return false;
                    }

                    var roleIds = keys[1].Split(",").Select(x => x.TryParseInt()).ToList();
                    return roleIds.Contains(roleId);
                });
            await NotifyUserGroup(message, userGroup, fcm);
        }

        public abstract Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
    }
}
