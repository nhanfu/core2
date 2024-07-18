using Core.Extensions;
using Core.Services;
using Microsoft.Net.Http.Headers;
using System.Net.WebSockets;

namespace Core.Middlewares;

public class LoadBalaceMiddleware
{
    private const int DefaultBufferSize = 4096;

    private readonly RequestDelegate _next;
    private readonly IConfiguration _conf;
    private readonly HttpClient _httpClient;
    private readonly ProxyOptions _defaultOptions;
    private static Clusters Balancer => Clusters.Data;
    private static readonly string[] NotForwardedWebSocketHeaders = ["Connection", "Host", "Upgrade", "Sec-WebSocket-Key", "Sec-WebSocket-Version"];

    public LoadBalaceMiddleware(RequestDelegate next, IConfiguration conf)
    {
        _next = next;
        _conf = conf;
        _defaultOptions = new ProxyOptions()
        {
            SendChunked = false
        };
        Clusters.Data = new Clusters
        {
            Nodes = _conf.GetSection("Proxy:Destination").Get<List<Node>>()
        };
        _httpClient = new HttpClient(_defaultOptions.BackChannelMessageHandler ?? new HttpClientHandler());
    }

    private static void SetPortAndSchema(ProxyOptions options)
    {
        if (!options.Port.HasValue)
        {
            if (string.Equals(options.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                options.Port = 443;
            }
            else
            {
                options.Port = 80;
            }
        }
        if (string.IsNullOrEmpty(options.Scheme))
        {
            options.Scheme = "http";
        }
    }

    public async Task Invoke(HttpContext context)
    {
        var role = _conf.GetSection("Role").Get<string>();
        if (role != Utils.Balancer 
            || context.Request.Headers.TryGetValue(HeaderNames.Connection, out var con) && con == "hub")
        {
            await _next(context);
            return;
        }
        var options = _defaultOptions;
        int maxRetry = 5;
    Start:
        var node = ResolveNode(); // try to resolve node from cluster table in the db, not only from config
        options.Port = node.Port;
        SetPortAndSchema(options);
        try
        {
            await Dispatch(context, options, node);
            node.Alive = true;
            node.LastResponse = DateTime.Now;
        }
        catch (HttpRequestException)
        {
            node.Alive = false;
            if (maxRetry == 0)
            {
                throw;
            }
            maxRetry--;
            goto Start;
        }
    }

    public int RecoveryMinutes = 10;
    private Node ResolveNode()
    {
        var nodes = new List<Node>();
        for (var i = 0; i < Balancer.Nodes.Count; i++)
        {
            var node = Balancer.Nodes[i];
            if (node.Alive || node.LastResponse < DateTime.Now.AddMinutes(-RecoveryMinutes))
                nodes.Add(node);
        }
        Balancer.AvailableNodes = nodes;
        var result = Balancer.Index >= 0 && Balancer.Index < Balancer.AvailableNodes.Count
            ? Balancer.AvailableNodes[Balancer.Index] : Balancer.AvailableNodes[0];
        Balancer.Index++;
        Balancer.Index %= Balancer.AvailableNodes.Count;
        return result;
    }

    private async Task Dispatch(HttpContext context, ProxyOptions options, Node destination)
    {
        context.Request.Headers["X-Forwarded-For"] = context.Connection.RemoteIpAddress.ToString();
        context.Request.Headers["X-Forwarded-Proto"] = context.Request.Protocol.ToString();
        int port = context.Request.Host.Port ?? (context.Request.IsHttps ? 443 : 80);
        context.Request.Headers["X-Forwarded-Port"] = port.ToString();
        context.Request.Headers["X-Forwarded-To-Port"] = destination.Port.ToString();

        var chost = (destination == null) ? options.Host : destination.Host;
        var cport = (destination == null) ? options.Port : destination.Port;
        var scheme = (destination == null) ? options.Scheme : destination.Scheme;

        if (context.WebSockets.IsWebSocketRequest)
        {
            await HandleWebSocketRequest(context, options, destination, chost, cport.Value);
        }
        else
        {
            await HandleHttpRequest(context, options, chost, cport.Value, scheme);
        }
    }

    private static async Task HandleWebSocketRequest(HttpContext context, ProxyOptions _options, Node destination, string host, int port)
    {
        using var client = new ClientWebSocket();
        foreach (var headerEntry in context.Request.Headers)
        {
            if (!NotForwardedWebSocketHeaders.Contains(headerEntry.Key, StringComparer.OrdinalIgnoreCase))
            {
                client.Options.SetRequestHeader(headerEntry.Key, headerEntry.Value);
            }
        }

        var wsScheme = string.Equals(destination.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
        string url = UserServiceHelpers.GetUri(host, port, wsScheme, $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}");

        if (_options.WebSocketKeepAliveInterval.HasValue)
        {
            client.Options.KeepAliveInterval = _options.WebSocketKeepAliveInterval.Value;
        }

        try
        {
            await client.ConnectAsync(new Uri(url), context.RequestAborted);
        }
        catch (WebSocketException)
        {
            context.Response.StatusCode = 400;
            return;
        }

        using var server = await context.WebSockets.AcceptWebSocketAsync(client.SubProtocol);
        await Task.WhenAll(PumpWebSocket(context, client, server, _options, context.RequestAborted), PumpWebSocket(context, server, client, _options, context.RequestAborted));
    }

    private static async Task PumpWebSocket(HttpContext context, WebSocket source, WebSocket destination, ProxyOptions _options, CancellationToken cancellationToken)
    {
        var buffer = new byte[_options.BufferSize ?? DefaultBufferSize];
        while (true)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, null, cancellationToken);
                return;
            }
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await destination.CloseOutputAsync(source.CloseStatus.Value, source.CloseStatusDescription, cancellationToken);
                return;
            }

            await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken);
        }
    }

    private async Task HandleHttpRequest(HttpContext context, ProxyOptions _options, string host, int port, string scheme)
    {
        var requestMessage = new HttpRequestMessage();
        var requestMethod = context.Request.Method;

        if (!HttpMethods.IsGet(requestMethod) && !HttpMethods.IsHead(requestMethod) && !HttpMethods.IsDelete(requestMethod) && !HttpMethods.IsTrace(requestMethod))
        {
            var streamContent = new StreamContent(context.Request.Body);
            requestMessage.Content = streamContent;
        }

        foreach (var header in context.Request.Headers)
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        requestMessage.Headers.Host = host;
        string uriString = UserServiceHelpers.GetUri(host, port, scheme, $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}");
        requestMessage.RequestUri = new Uri(uriString);
        requestMessage.Method = new HttpMethod(context.Request.Method);
        using var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        context.Response.StatusCode = (int)responseMessage.StatusCode;
        foreach (var header in responseMessage.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        foreach (var header in responseMessage.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        if (!_options.SendChunked)
        {
            context.Response.Headers.Remove("transfer-encoding");
            await responseMessage.Content.CopyToAsync(context.Response.Body);
        }
        else
        {
            var buffer = new byte[_options.BufferSize ?? DefaultBufferSize];
            using var responseStream = await responseMessage.Content.ReadAsStreamAsync();
            int len = 0;
            int full = 0;
            while ((len = await responseStream.ReadAsync(buffer)) > 0)
            {
                await context.Response.Body.WriteAsync(buffer);
                full += buffer.Length;
            }
            context.Response.Headers.Remove("transfer-encoding");
        }
    }
}

public class ProxyOptions
{
    private int? _bufferSize;
    public long Score { get; set; }
    public string Scheme { get; set; }
    public string Host { get; set; }
    public int? Port { get; set; }
    public string UrlHost { get; set; }
    public HttpMessageHandler BackChannelMessageHandler { get; set; }
    public TimeSpan? WebSocketKeepAliveInterval { get; set; }
    public bool SendChunked { get; set; }
    public int? BufferSize
    {
        get
        {
            return _bufferSize;
        }
        set
        {
            if (value.HasValue && value.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            _bufferSize = value;
        }
    }
}

public class Clusters
{
    public List<Node> AvailableNodes { get; set; }
    public List<Node> Nodes { get; set; }
    public int Index { get; set; }
    public string Policy { get; set; }
    public Dictionary<int, long> Score { get; set; }
    public static Clusters Data { get; set; }
}

public class Node
{
    public bool Alive { get; set; } = true;
    public DateTime LastResponse { get; set; } = DateTime.Now;
    public string Id { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public string Scheme { get; set; }
    public string Role { get; set; }
}