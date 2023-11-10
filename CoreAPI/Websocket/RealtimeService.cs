using Newtonsoft.Json;
using System.Net.WebSockets;
using Core.Models;

namespace Core.Websocket
{
    public class RealtimeService : WebSocketHandler
    {
        public RealtimeService(ConnectionManager webSocketConnectionManager, IConfiguration configuration, ILogger<RealtimeService> logger)
            : base(webSocketConnectionManager, configuration, logger)
        {
        }

        public override async Task OnConnected(WebSocket socket, string userId, List<string> roleIds, string ip)
        {
            await base.OnConnected(socket, userId, roleIds, ip);
        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var task = new TaskNotification
            {
                InsertedDate = DateTime.Now,
                Title = "Chuyến xe mới",
                Description = "Cát Lái - Bình Dương"
            };
            await SendMessageToAll(JsonConvert.SerializeObject(task));
        }
    }
}
