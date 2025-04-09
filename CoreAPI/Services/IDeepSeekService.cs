using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public interface IDeepSeekService
{
    IAsyncEnumerable<string> ChatCompletionStreamAsync(ChatCompletionRequest request);
}

public class DeepSeekService : IDeepSeekService
{
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;

    public DeepSeekService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _apiKey = configuration["DeepSeekConfig:ApiKey"];
        _baseUrl = configuration["DeepSeekConfig:BaseUrl"];
        _httpClient = httpClientFactory.CreateClient();
    }

    public async IAsyncEnumerable<string> ChatCompletionStreamAsync(ChatCompletionRequest request)
    {
        var url = $"{_baseUrl}/chat/completions";

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var json = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {errorContent}");
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line;
            }
        }
    }
}