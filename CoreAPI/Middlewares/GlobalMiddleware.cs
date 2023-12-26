using Core.Enums;
using Newtonsoft.Json;
using System.Text;
using Core.Exceptions;

namespace CoreAPI.Middlewares
{
    public class GlobalMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

        public async Task Invoke(HttpContext context)
        {
            try
            {
                UserServiceHelpers.Port = UserServiceHelpers.ParsePort(context.Request);
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                if (context.Response.HasStarted)
                {
                    return;
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
                await WriteContextMessage(context, ex, ex.StatusCode, ex.ContentType);
                return;
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    return;
                }
                await WriteContextMessage(context, ex, HttpStatusCode.InternalServerError);
                return;
            }
        }

        private static async Task WriteContextMessage(HttpContext context, Exception ex, HttpStatusCode statusCode, string contentType = null)
        {
            contentType ??= "application/json; charset=utf-8";
            context.Response.Clear();
            context.Response.StatusCode = (int)statusCode;
            context.Response.Headers.TryAdd("Content-type", contentType);
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
