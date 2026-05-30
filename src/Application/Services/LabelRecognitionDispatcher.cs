namespace ThePantry.Application.Services;

public class LabelRecognitionDispatcher : ILabelRecognitionService
{
    private readonly AppSettingsService _appSettings;
    private readonly AnthropicLabelRecognitionService _anthropic;
    private readonly OpenAiCompatibleLabelRecognitionService _openAiCompatible;
    private readonly ILogger<LabelRecognitionDispatcher> _logger;

    public bool IsConfigured => true; // lazily checked inside RecognizeAsync

    public LabelRecognitionDispatcher(
        AppSettingsService appSettings,
        AnthropicLabelRecognitionService anthropic,
        OpenAiCompatibleLabelRecognitionService openAiCompatible,
        ILogger<LabelRecognitionDispatcher> logger)
    {
        _appSettings = appSettings;
        _anthropic = anthropic;
        _openAiCompatible = openAiCompatible;
        _logger = logger;
    }

    public async Task<LabelRecognitionResult?> RecognizeAsync(string base64ImageData, string? base64ImageData2 = null, CancellationToken ct = default)
    {
        var provider = (await _appSettings.GetAsync("LLM_PROVIDER", ct) ?? "anthropic").ToLowerInvariant();
        var apiKey   = await _appSettings.GetAsync("LLM_API_KEY", ct);
        var model    = await _appSettings.GetAsync("LLM_MODEL", ct);
        var endpoint = await _appSettings.GetAsync("LLM_ENDPOINT", ct);

        var (imageData, mediaType)   = StripDataUrl(base64ImageData);
        var (imageData2, _) = base64ImageData2 != null ? StripDataUrl(base64ImageData2) : (null, "image/jpeg");

        try
        {
            return provider switch
            {
                "anthropic" => await _anthropic.RecognizeAsync(
                    apiKey ?? "",
                    model ?? "claude-haiku-4-5-20251001",
                    imageData, mediaType, imageData2, ct),

                "openai" => await _openAiCompatible.RecognizeAsync(
                    endpoint ?? "https://api.openai.com",
                    apiKey,
                    model ?? "gpt-4o-mini",
                    imageData, mediaType, imageData2, ct),

                "openrouter" => await _openAiCompatible.RecognizeAsync(
                    endpoint ?? "https://openrouter.ai/api",
                    apiKey,
                    model ?? "meta-llama/llama-3.2-11b-vision-instruct:free",
                    imageData, mediaType, imageData2, ct),

                "ollama" => await _openAiCompatible.RecognizeAsync(
                    endpoint ?? "http://localhost:11434",
                    null,
                    model ?? "llava",
                    imageData, mediaType, imageData2, ct),

                "llamacpp" => await _openAiCompatible.RecognizeAsync(
                    endpoint ?? "http://localhost:8080",
                    apiKey,
                    model ?? "",
                    imageData, mediaType, imageData2, ct),

                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Label recognition failed for provider {Provider}", provider);
            return null;
        }
    }

    private static (string data, string mediaType) StripDataUrl(string raw)
    {
        if (!raw.StartsWith("data:")) return (raw, "image/jpeg");
        var comma = raw.IndexOf(',');
        var prefix = raw[..comma];
        var mediaType = prefix.Contains("image/png") ? "image/png" : "image/jpeg";
        return (raw[(comma + 1)..], mediaType);
    }
}
