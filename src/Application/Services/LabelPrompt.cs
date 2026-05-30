using System.Text.Json;

namespace ThePantry.Application.Services;

internal static class LabelPrompt
{
    internal const string Text =
        "This is a product label photo, likely from a reduced/clearance meat or food item. " +
        "Extract the following and reply ONLY with valid JSON (no markdown, no explanation):\n" +
        "{\"name\": \"product name\", \"species\": \"animal species or null\", \"weight\": \"weight with unit or null\"}\n" +
        "Leave species null if not applicable.";

    internal static LabelRecognitionResult? ParseResult(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Strip markdown code fences if model included them despite instructions
        var json = text.Trim();
        if (json.StartsWith("```"))
        {
            var newline = json.IndexOf('\n');
            json = newline >= 0 ? json[(newline + 1)..] : json[3..];
        }
        if (json.EndsWith("```")) json = json[..json.LastIndexOf("```")];

        // Extract the first {...} block in case the model added surrounding prose
        var start = json.IndexOf('{');
        var end   = json.LastIndexOf('}');
        if (start >= 0 && end > start) json = json[start..(end + 1)];

        json = json.Trim();
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new LabelRecognitionResult
            {
                Name    = root.TryGetProperty("name",    out var n) ? n.GetString() ?? "" : "",
                Species = root.TryGetProperty("species", out var s) && s.ValueKind != JsonValueKind.Null ? s.GetString() : null,
                Weight  = root.TryGetProperty("weight",  out var w) && w.ValueKind != JsonValueKind.Null ? w.GetString() : null,
            };
        }
        catch
        {
            return null; // caller logs the raw text
        }
    }
}
