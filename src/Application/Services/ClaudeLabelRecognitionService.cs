using System.Text;
using System.Text.Json;

namespace ThePantry.Application.Services;

public class AnthropicLabelRecognitionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnthropicLabelRecognitionService> _logger;

    public AnthropicLabelRecognitionService(IHttpClientFactory httpClientFactory, ILogger<AnthropicLabelRecognitionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<LabelRecognitionResult?> RecognizeAsync(string apiKey, string model, string base64Image, string mediaType, string? base64Image2, CancellationToken ct)
    {
        var imageBlocks = new List<object>
        {
            new { type = "image", source = new { type = "base64", media_type = mediaType, data = base64Image } }
        };
        if (!string.IsNullOrWhiteSpace(base64Image2))
            imageBlocks.Add(new { type = "image", source = new { type = "base64", media_type = mediaType, data = base64Image2 } });
        imageBlocks.Add(new { type = "text", text = LabelPrompt.Text });

        var requestBody = new
        {
            model,
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "user", content = imageBlocks.ToArray() }
            }
        };

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.anthropic.com/");
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var response = await client.PostAsync("/v1/messages",
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Anthropic API error {Status}: {Body}", response.StatusCode, body);
            return null;
        }

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
        return LabelPrompt.ParseResult(text);
    }
}
