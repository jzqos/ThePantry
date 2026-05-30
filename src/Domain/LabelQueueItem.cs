namespace ThePantry.Domain;

public enum LabelScanStatus { Pending, Processing, Complete, Failed }

public class LabelQueueItem
{
    public int Id { get; set; }
    public string Upc { get; set; } = string.Empty;
    public string? ImagePath { get; set; }   // barcode-side photo
    public string? ImagePath2 { get; set; }  // front-label photo (optional, added by user)
    public string? Category { get; set; }
    public LabelScanStatus Status { get; set; } = LabelScanStatus.Pending;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Results from LLM
    public string? ResultName { get; set; }
    public string? ResultSpecies { get; set; }
    public string? ResultWeight { get; set; }
    public string? ErrorMessage { get; set; }

    public int? LinkedInventoryItemId { get; set; }
    public InventoryItem? LinkedInventoryItem { get; set; }
}
