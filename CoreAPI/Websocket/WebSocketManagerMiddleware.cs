using System.Net.WebSockets;
using System.Security.Claims;
using Core.Extensions;
using Core.Services;

namespace Core.Websocket;

public class WebSocketManagerMiddleware(RequestDelegate next, WebSocketService webSocketHandler)
{
    private WebSocketService WebSocketHandler { get; set; } = webSocketHandler;

    public async Task Invoke(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            await next(context);
            return;
        }

        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var token = context.Request.Query["access_token"].ToString();
        var configuration = context.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
        var principal = Utils.GetPrincipalFromAccessToken(token, configuration);
        var userId = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        var roleIds = principal.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
        var ip = UserService.GetRemoteIpAddress(context);
        await WebSocketHandler.OnConnected(socket, userId, roleIds, context.Connection.RemoteIpAddress.ToString());
        await Receive(socket, userId, roleIds, ip, async (userId, roleIds, ip, result, buffer) =>
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                await WebSocketHandler.ReceiveAsync(userId, roleIds, ip, socket, result, buffer);
                return;
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await WebSocketHandler.OnDisconnected(socket);
                return;
            }
        });
    }

    private static async Task Receive(WebSocket socket, string userId, List<string> roleIds, string ip, Action<string, List<string>, string, WebSocketReceiveResult, byte[]> handleMessage)
    {
        var buffer = new byte[1024 * 4];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            handleMessage(userId, roleIds, ip, result, buffer);
        }
    }
}