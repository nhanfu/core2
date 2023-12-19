using System.Net.WebSockets;

namespace CoreAPI.Middlewares;

public class LoadBalaceMiddleware
{
    private const int DefaultBufferSize = 4096;

    private readonly RequestDelegate _next;
    private readonly IConfiguration _conf;
    private readonly HttpClient _httpClient;
    private readonly ProxyOptions _defaultOptions;
    private readonly BalancerOptions _balancer;

    private static readonly string[] NotForwardedWebSocketHeaders = ["Connection", "Host", "Upgrade", "Sec-WebSocket-Key", "Sec-WebSocket-Version"];

    public LoadBalaceMiddleware(RequestDelegate next, IConfiguration conf)
    {
        _next = next;
        _conf = conf;
        _defaultOptions = new ProxyOptions()
        {
            SendChunked = false
        };
        _balancer = new BalancerOptions
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
        if (_conf.GetSection("Role").Get<string>() != "Balancer")
        {
            await _next(context);
            return;
        }
        var options = _defaultOptions;
        int maxRetry = 5;
    Start:
        var node = ResolveNode();
        options.Port = node.Port;
        SetPortAndSchema(options);
        try
        {
            await Dispatch(context, options, node);
            node.Alive = true;
            node.LastResponse = DateTimeOffset.Now;
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
        for (var i = 0; i < _balancer.Nodes.Count; i++)
        {
            var node = _balancer.Nodes[i];
            if (node.Alive || node.LastResponse < DateTimeOffset.Now.AddMinutes(-RecoveryMinutes))
                nodes.Add(node);
        }
        _balancer.AvailableNodes = nodes;
        var result = _balancer.Index >= 0 && _balancer.Index < _balancer.AvailableNodes.Count
            ? _balancer.AvailableNodes[_balancer.Index] : _balancer.AvailableNodes[0];
        _balancer.Index++;
        _balancer.Index %= _balancer.AvailableNodes.Count;
        return result;
    }

    private async Task Dispatch(HttpContext context, ProxyOptions options, Node destination)
    {
        context.Request.Headers["X-Forwarded-For"] = context.Connection.RemoteIpAddress.ToString();
        context.Request.Headers["X-Forwarded-Proto"] = context.Request.Protocol.ToString();
        int port = context.Request.Host.Port ?? (context.Request.IsHttps ? 443 : 80);
        context.Request.Headers["X-Forwarded-Port"] = port.ToString();

        var chost = (destination == null) ? options.Host : destination.Host;
        var cport = (destination == null) ? options.Port : destination.Port;
        var scheme = (destination == null) ? options.Scheme : destination.Scheme;

        if (context.WebSockets.IsWebSocketRequest)
        {
            await HandleWebSocketRequest(context, options, destination, chost, cport.Value, scheme);
        }
        else
        {
            await HandleHttpRequest(context, options, destination, chost, cport.Value, scheme);
        }
    }

    private static async Task HandleWebSocketRequest(HttpContext context, ProxyOptions _options, Node destination, string host, int port, string scheme)
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
        string url = GetUri(context, host, port, scheme);

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

    private async Task HandleHttpRequest(HttpContext context, ProxyOptions _options, Node destination, string host, int port, string scheme)
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
        string uriString = GetUri(context, host, port, scheme);
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

    private static string GetUri(HttpContext context, string host, int? port, string scheme)
    {
        var urlPort = "";
        if (port.HasValue
            && !(port.Value == 443 && "https".Equals(scheme, StringComparison.InvariantCultureIgnoreCase))
            && !(port.Value == 80 && "http".Equals(scheme, StringComparison.InvariantCultureIgnoreCase))
            )
        {
            urlPort = ":" + port.Value;
        }
        return $"{scheme}://{host}{urlPort}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
    }

    public bool Terminate(HttpContext httpContext)
    {
        return true;
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

public class BalancerOptions
{
    public List<Node> AvailableNodes { get; set; }
    public List<Node> Nodes { get; set; }
    public int Index { get; set; }
    public string Policy { get; set; }
    public Dictionary<int, long> Score { get; set; }
}

public class Node
{
    public bool Alive { get; set; } = true;
    public DateTimeOffset LastResponse { get; set; } = DateTimeOffset.Now;
    public string Host { get; set; }
    public int Port { get; set; }
    public string Scheme { get; set; }
}