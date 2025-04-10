using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using static OpenAIHttpClientService;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DeepSeekController : ControllerBase
{
    private readonly IDeepSeekService _deepSeekService;
    private readonly OpenAIHttpClientService _gptService;

    public DeepSeekController(IDeepSeekService deepSeekService, OpenAIHttpClientService gptService)
    {
        _deepSeekService = deepSeekService;
        _gptService = gptService;
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

    [HttpPost("chat-stream-gpt")]
    public async Task ChatWithHistory([FromBody] ChatSessionViewModel request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var chunk in _gptService.GetChatGPTResponseStreamWithHistoryAsync(request.Messages))
        {
            await Response.WriteAsync(chunk);
            await Response.Body.FlushAsync();
        }
    }
}