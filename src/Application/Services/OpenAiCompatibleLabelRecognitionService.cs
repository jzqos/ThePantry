using System.Text;
using System.Text.Json;

namespace ThePantry.Application.Services;

/// <summary>
/// Handles OpenAI-compatible /v1/chat/completions endpoints:
/// OpenAI, OpenRouter, Ollama (/v1/), llama.cpp server
/// </summary>
public class OpenAiCompatibleLabelRecognitionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiCompatibleLabelRecognitionService> _logger;

    public OpenAiCompatibleLabelRecognitionService(IHttpClientFactory httpClientFactory, ILogger<OpenAiCompatibleLabelRecognitionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<LabelRecognitionResult?> RecognizeAsync(
        string baseUrl, string? apiKey, string model, string base64Image, string mediaType, string? base64Image2, CancellationToken ct)
    {
        var contentBlocks = new List<object>
        {
            new { type = "image_url", image_url = new { url = $"data:{mediaType};base64,{base64Image}" } }
        };
        if (!string.IsNullOrWhiteSpace(base64Image2))
            contentBlocks.Add(new { type = "image_url", image_url = new { url = $"data:{mediaType};base64,{base64Image2}" } });
        contentBlocks.Add(new { type = "text", text = LabelPrompt.Text });

        var requestBody = new
        {
            model,
            max_completion_tokens = 1024,
            messages = new[]
            {
                new { role = "user", content = contentBlocks.ToArray() }
            }
        };

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await client.PostAsync("v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("LLM API error {Status} at {Url}: {Body}", response.StatusCode, baseUrl, body);
            return null;
        }

        using var doc = JsonDocument.Parse(body);
        var choice  = doc.RootElement.GetProperty("choices")[0];
        var message = choice.GetProperty("message");

        var finishReason = choice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : "?";
        var text         = message.TryGetProperty("content",  out var contentEl) && contentEl.ValueKind != JsonValueKind.Null ? contentEl.GetString() : null;
        var refusal      = message.TryGetProperty("refusal",  out var refusalEl) && refusalEl.ValueKind != JsonValueKind.Null ? refusalEl.GetString() : null;

        _logger.LogInformation("LLM response: finish={Finish} content_len={Len} refusal={Refusal} text={Text}",
            finishReason, text?.Length ?? 0, refusal ?? "(none)", text ?? "(null)");

        if (!string.IsNullOrWhiteSpace(refusal))
        {
            _logger.LogWarning("LLM refused: {Refusal}", refusal);
            return null;
        }

        return LabelPrompt.ParseResult(text);
    }
}
