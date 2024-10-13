using System.Text;
using System.Text.Json;

public class OpenAIHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIHttpClientService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"];
    }

    public async Task<string> GetChatGPTResponse(string prompt)
    {
        var requestUri = "https://api.openai.com/v1/chat/completions";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = prompt }
            },
            max_tokens = 150
        };

        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"OpenAI API request failed with status code {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }
}