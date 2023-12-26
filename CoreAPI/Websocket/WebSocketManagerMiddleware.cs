using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
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
        if (context.Request.Path.Value.Contains("task"))
        {
            await ReceiveFromDevice(context, socket, token, configuration);
        }
        else if (context.Request.PathBase.Value.Contains("clusters"))
        {
            await ReceiveFromCluster(context, socket);
        }
    }

    private async Task ReceiveFromCluster(HttpContext context, WebSocket socket)
    {
        var deviceKey = WebSocketHandler.OnClusterConnected(socket, context.Request.Host.Value, UserServiceHelpers.Port);
        Console.WriteLine("Receive from cluster - Device key: {0}", deviceKey);
        await socket.SendAsync(Encoding.ASCII.GetBytes(deviceKey), WebSocketMessageType.Text, true, CancellationToken.None);
        await Receive(socket, deviceKey, async (deviceKey, result, buffer) =>
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                await WebSocketHandler.ReceiveAsync(deviceKey, socket, buffer);
                return;
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await WebSocketHandler.OnDisconnected(socket);
                return;
            }
        });
    }

    private async Task ReceiveFromDevice(HttpContext context, WebSocket socket, string token, IConfiguration configuration)
    {
        var principal = Utils.GetPrincipalFromAccessToken(token, configuration);
        var userId = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        var roleIds = principal.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
        var ip = UserService.GetRemoteIpAddress(context);
        var deviceKey = WebSocketHandler.OnClientConnected(socket, userId, roleIds, context.Connection.RemoteIpAddress.ToString());
        await socket.SendAsync(Encoding.ASCII.GetBytes(deviceKey), WebSocketMessageType.Text, true, CancellationToken.None);
        await Receive(socket, deviceKey, async (deviceKey, result, buffer) =>
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                await WebSocketHandler.ReceiveAsync(deviceKey, socket, buffer);
                return;
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await WebSocketHandler.OnDisconnected(socket);
                return;
            }
        });
    }

    private static async Task Receive(WebSocket socket, string deviceKey, Action<string, WebSocketReceiveResult, byte[]> handleMessage)
    {
        var buffer = new byte[1024 * 4];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            handleMessage(deviceKey, result, buffer);
        }
    }
}