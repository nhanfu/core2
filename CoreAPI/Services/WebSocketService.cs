using Core.Extensions;
using Core.ViewModels;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Core.Services
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
        public string Badge { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string EntityId { get; set; }
        public string RecordId { get; set; }
    }

    public class FCMNotification
    {
        public string Badge { get; set; }
        public string Sound { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string ClickAction { get; set; }
    }

    public class WebSocketService(ConnectionManager connManager, IConfiguration configuration, IDistributedCache cache)
    {
        private readonly string FCM_API_KEY = configuration["FCM_API_KEY"];
        private readonly string FCM_SENDER_ID = configuration["FCM_SENDER_ID"];

        public virtual string OnDeviceConnected(WebSocket socket, string userId, List<string> roleIds, string ip, string companyName)
        {
            return connManager.AddDeviceSocket(socket, userId, roleIds, ip, companyName);
        }

        public virtual async Task OnDisconnected(WebSocket socket, bool cluster = false)
        {
            await connManager.RemoveSocket(connManager.GetId(socket, cluster));
        }

        public virtual string OnClusterConnected(WebSocket socket, string deviceKey)
        {
            return connManager.AddClusterSocket(socket, $"{deviceKey}/{Uuid7.Guid().ToString()}");
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

        public async Task SendMessageToAll(string message, string TenantCode)
        {
            var users = connManager.GetDeviceSockets(TenantCode);
            foreach (var pair in users)
            {
                if (pair.Value.State == WebSocketState.Open)
                {
                    await SendMessageAsync(pair.Value, message);
                }
            }
        }

        public async Task SendMessageToSubscribers(string message, string queueName)
        {
            var connections = connManager.GetSocketByQueue(queueName);
            foreach (var socket in connections)
            {
                if (socket.State == WebSocketState.Open)
                {
                    await SendMessageAsync(socket, message);
                }
            }
        }

        public ConcurrentDictionary<string, WebSocket> GetAll(string TenantCode)
        {
            return connManager.GetDeviceSockets(TenantCode);
        }

        public Task SendMessageToUsersAsync(List<string> userIds, string message, string fcm, string TenantCode)
        {
            var userGroup = connManager.GetDeviceSockets(TenantCode)
                .Where(x => userIds.Contains(x.Key.Split("/").FirstOrDefault()));
            return NotifyUserGroup(message, userGroup, fcm);
        }

        public async Task SendMessageToSocketAsync(string token, string message, string fcm, string TenantCode)
        {
            var pair = connManager.GetDeviceSockets(TenantCode)
                .FirstOrDefault(x => x.Key == token);
            var fcmTask = SendFCMNotfication(fcm);
            if (pair.Value.State != WebSocketState.Open)
            {
                return;
            }
            await SendMessageAsync(pair.Value, message);
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

        public async Task ReceiveAsync(string deviceKey, WebSocket socket, byte[] buffer)
        {
            var text = Encoding.UTF8.GetString(buffer);
            var mq = text.TryParse<MQEvent>();
            if (mq is null)
            {
                await socket.SendAsync(Encoding.ASCII.GetBytes(deviceKey), WebSocketMessageType.Text, true, CancellationToken.None);
                return;
            }
            mq.DeviceKey = deviceKey;
            await MQAction(mq);
        }

        public async Task MQAction(MQEvent mq)
        {
            switch (mq.Action)
            {
                case "ClearCache":
                    await cache.RemoveAsync(mq.Message);
                    break;
                case "Subscribe":
                    connManager.SubScribeQueue(mq.DeviceKey, mq.QueueName);
                    break;
                case "Unsubscribe":
                    connManager.UnsubScribeQueue(mq.DeviceKey, mq.QueueName);
                    break;
            }
        }
    }
}
