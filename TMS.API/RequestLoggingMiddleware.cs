using Core.Extensions;
using System.Security.Claims;
using TMS.API.Models;

namespace TMS.API
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IServiceScopeFactory serviceScopeFactory, IHttpContextAccessor httpContextAccessor)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;
            var requestBody = await ReadRequestBodyAsync(request);
            if (!requestBody.IsNullOrWhiteSpace() && (request.Method == HttpMethods.Post || request.Method == HttpMethods.Put || request.Method == HttpMethods.Delete))
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var _context = scope.ServiceProvider.GetRequiredService<LOGContext>();

                    // Ghi nhật ký chỉ cho các yêu cầu POST, PUT và DELETE

                    var logEntry = new RequestLog
                    {
                        HttpMethod = request.Method,
                        Path = request.Path,
                        InsertedDate = DateTime.UtcNow,
                    };
                    logEntry.RequestBody = requestBody;
                    var userId = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value?.TryParseInt() ?? 0;
                    logEntry.InsertedBy = userId;
                    // Ghi nhận phản hồi và lưu vào log entry
                    var originalBodyStream = context.Response.Body;
                    using (var responseBody = new MemoryStream())
                    {
                        context.Response.Body = responseBody;

                        await _next(context);

                        var response = await FormatResponse(context.Response);

                        logEntry.StatusCode = context.Response.StatusCode;
                        logEntry.ResponseBody = response;

                        await responseBody.CopyToAsync(originalBodyStream);
                    }

                    // Lưu mục nhập nhật ký vào cơ sở dữ liệu
                    _context.RequestLog.Add(logEntry);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();

            using (var reader = new StreamReader(request.Body, leaveOpen: true))
            {
                var requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                return requestBody;
            }
        }


        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return body;
        }
    }
}
