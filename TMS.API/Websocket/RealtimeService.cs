using Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace TMS.API.Websocket
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
