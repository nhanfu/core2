using Core.Enums;
using Core.Extensions;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Exceptions;
using Core.Models;

namespace Core
{
    public class HttpStatusCodeExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpStatusCodeExceptionMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public HttpStatusCodeExceptionMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = loggerFactory?.CreateLogger<HttpStatusCodeExceptionMiddleware>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();

            using StreamReader reader = new(request.Body, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return requestBody;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                var request = context.Request;
                var sideEffectMethods = request.Method == HttpMethods.Put || request.Method == HttpMethods.Delete || request.Method == HttpMethods.Patch;
                if (!sideEffectMethods)
                {
                    await _next(context);
                    return;
                }
                await RunSideEffectContext(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                if (context.Response.HasStarted)
                {
                    return;
                }
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(message: BuildExpMessage(ex));
                }
                await WriteContextMessage(context, ex, HttpStatusCode.Unauthorized);
                return;
            }
            catch (ApiException ex)
            {
                if (context.Response.HasStarted)
                {
                    return;
                }
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(BuildExpMessage(ex));
                }
                await WriteContextMessage(context, ex, ex.StatusCode, ex.ContentType);
                return;
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    return;
                }
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(BuildExpMessage(ex));
                }
                await WriteContextMessage(context, ex, HttpStatusCode.InternalServerError);
                return;
            }
        }

        private async Task RunSideEffectContext(HttpContext context)
        {
            var request = context.Request;
            var requestBody = await ReadRequestBodyAsync(request);
            if (!requestBody.IsNullOrWhiteSpace())
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<LOGContext>();
                string token = request.Headers.Authorization.FirstOrDefault().Replace("Bearer ", "");
                if (token == null)
                {
                    await _next(context);
                    return;
                }
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var logEntry = new RequestLog
                {
                    Id = Guid.NewGuid().ToString(),
                    HttpMethod = request.Method,
                    Path = request.Path,
                    InsertedDate = DateTimeOffset.Now,
                    Active = true,
                    TenantCode = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimaryGroupSid)?.Value
                };
                await _next(context);
                logEntry.StatusCode = context.Response.StatusCode;
                logEntry.UpdatedDate = DateTimeOffset.Now;
                logEntry.RequestBody = requestBody;
                var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
                logEntry.InsertedBy = userId;
                _context.RequestLog.Add(logEntry);
                await _context.SaveChangesAsync();
            }
            else
            {
                await _next(context);
            }
        }

        private static string BuildExpMessage(Exception ex)
        {
            return string.Concat("Error occurs at: ", DateTime.Now.ToString(), ex.Message, ex.InnerException?.Message, ex.StackTrace);
        }

        private static async Task WriteContextMessage(HttpContext context, Exception ex, HttpStatusCode statusCode, string contentType = null)
        {
            contentType ??= "application/json; charset=utf-8";
            context.Response.Clear();
            context.Response.StatusCode = (int)statusCode;
            context.Response.Headers.Add("Content-type", contentType);
            context.Response.ContentType = contentType;
            var response = string.Empty;
            var message = ex.Message;
#if !DEBUG
            response = JsonConvert.SerializeObject(new { ex.Message, StatusCode = (int)statusCode });
#else
            response = JsonConvert.SerializeObject(new { Message = ex.Message + ex.InnerException?.Message, ex.StackTrace, StatusCode = (int)statusCode });
#endif
            await context.Response.WriteAsync(response, Encoding.UTF8);
        }
    }
}
