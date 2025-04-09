using CoreAPI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DeepSeekController : ControllerBase
{
    private readonly IDeepSeekService _deepSeekService;

    public DeepSeekController(IDeepSeekService deepSeekService)
    {
        _deepSeekService = deepSeekService;
    }

    [HttpPost("chat-stream")]
    public async Task ChatCompletionStream([FromBody] ChatCompletionRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var chunk in _deepSeekService.ChatCompletionStreamAsync(request))
        {
            await Response.WriteAsync(chunk);
            await Response.Body.FlushAsync();
        }
    }
}