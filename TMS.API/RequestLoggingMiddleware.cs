using Core.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TMS.API.Models;

namespace TMS.API
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;
            var requestBody = await ReadRequestBodyAsync(request);
            if (!requestBody.IsNullOrWhiteSpace() && (request.Method == HttpMethods.Put || request.Method == HttpMethods.Delete || request.Method == HttpMethods.Patch))
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var _context = scope.ServiceProvider.GetRequiredService<LOGContext>();
                    string token = request.Headers.Authorization.FirstOrDefault().Replace("Bearer ", "");
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    var logEntry = new RequestLog
                    {
                        HttpMethod = request.Method,
                        Path = request.Path,
                        InsertedDate = DateTime.Now,
                        Active = true,
                    };
                    await _next(context);
                    logEntry.StatusCode = context.Response.StatusCode.ToString();
                    logEntry.UpdatedDate = DateTime.Now;
                    logEntry.RequestBody = requestBody;
                    var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
                    logEntry.InsertedBy = userId;
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
    }
}
