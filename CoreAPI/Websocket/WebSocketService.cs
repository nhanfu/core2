using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Core.Websocket
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

    public class WebSocketService(ConnectionManager connectionManager, IConfiguration configuration)
    {
        protected ConnectionManager ConnectionManager { get; set; } = connectionManager;
        private readonly string FCM_API_KEY = configuration["FCM_API_KEY"];
        private readonly string FCM_SENDER_ID = configuration["FCM_SENDER_ID"];

        public virtual Task OnConnected(WebSocket socket, string userId, List<string> roleIds, string ip)
        {
            ConnectionManager.AddSocket(socket, userId, roleIds, ip);
            return Task.CompletedTask;
        }

        public virtual async Task OnDisconnected(WebSocket socket)
        {
            await ConnectionManager.RemoveSocket(ConnectionManager.GetId(socket));
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
            var users = ConnectionManager.GetAll();
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
            var users = ConnectionManager.GetAll().Where(x => !x.Key.Contains(queueName));
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
            return ConnectionManager.GetAll();
        }

        public Task SendMessageToUsersAsync(List<string> userIds, string message, string fcm = null)
        {
            var userGroup = ConnectionManager.GetAll()
                .Where(x => userIds.Contains(x.Key.Split("/").FirstOrDefault()));
            return NotifyUserGroup(message, userGroup, fcm);
        }

        public async Task SendMessageToSocketAsync(string token, string message, string fcm = null)
        {
            var pair = ConnectionManager.GetAll()
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            await socket.SendAsync(Encoding.ASCII.GetBytes("connected"), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
