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
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // Nếu dùng nginx thì cái này giúp không bị buffer

        try
        {
            await foreach (var chunk in _gptService.GetChatGPTResponseStreamWithHistoryAsync(request.Messages))
            {
                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    // Gửi đúng format SSE để client xử lý tốt
                    await Response.WriteAsync($"data: {chunk}\n\n");
                    await Response.Body.FlushAsync();
                }
            }

            // Gửi kết thúc stream
            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            // Log hoặc handle error theo cách của bạn
            await Response.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n");
            await Response.Body.FlushAsync();
        }
    }

}