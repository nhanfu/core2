using Core.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Core.Exceptions
{
    public class HttpStatusCodeExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpStatusCodeExceptionMiddleware> _logger;

        public HttpStatusCodeExceptionMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = loggerFactory?.CreateLogger<HttpStatusCodeExceptionMiddleware>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                if (context.Response.HasStarted)
                {
                    throw;
                }
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(BuildExpMessage(ex));
                }
                await WriteContextMessage(context, ex, HttpStatusCode.Unauthorized);
                return;
            }
            catch (ApiException ex)
            {
                if (context.Response.HasStarted)
                {
                    throw;
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
                    throw;
                }
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(BuildExpMessage(ex));
                }
                await WriteContextMessage(context, ex, HttpStatusCode.InternalServerError);
                return;
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

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class HttpStatusCodeExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpStatusCodeExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpStatusCodeExceptionMiddleware>();
        }
    }

}
