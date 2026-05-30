namespace ThePantry.Application.Services;

public class LabelRecognitionResult
{
    public string Name { get; set; } = string.Empty;
    public string? Species { get; set; }
    public string? Weight { get; set; }
}

public interface ILabelRecognitionService
{
    bool IsConfigured { get; }
    /// <summary>Recognizes a label from one or two images (e.g. barcode side + front label).</summary>
    Task<LabelRecognitionResult?> RecognizeAsync(string base64ImageData, string? base64ImageData2 = null, CancellationToken ct = default);
}
