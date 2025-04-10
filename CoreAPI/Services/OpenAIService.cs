using OpenAI.Chat;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class OpenAIHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIHttpClientService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"];
    }

    public async IAsyncEnumerable<string> GetChatGPTResponseStreamWithHistoryAsync(List<ChatMessage> messages)
    {
        var formattedMessages = new List<object>();

        foreach (var msg in messages)
        {
            if (!string.IsNullOrWhiteSpace(msg.Images))
            {
                formattedMessages.Add(new
                {
                    role = msg.Role,
                    content = new object[]
                    {
                    new { type = "text", text = msg.Content },
                    new { type = "image_url", image_url = new { url = msg.Images } }
                    }
                });
            }
            else
            {
                formattedMessages.Add(new
                {
                    role = msg.Role,
                    content = msg.Content
                });
            }
        }

        var requestBody = new
        {
            model = "gpt-4o", // dùng model đúng
            messages = formattedMessages,
            stream = true,
            temperature = 0.7,
            max_tokens = 10000
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenAI API stream failed: {response.StatusCode}");

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Streamed lines begin with "data: "
            if (line.StartsWith("data: "))
            {
                line = line.Substring("data: ".Length);

                if (line == "[DONE]")
                    break;

                yield return line;
            }
        }
    }

    public class ChatSessionViewModel
    {
        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = new();

        [JsonPropertyName("image_base64")]
        public string ImageBase64 { get; set; }
    }

    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("images")]
        public string Images { get; set; } // base64 string (nếu có)
    }
}