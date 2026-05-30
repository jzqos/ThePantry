using MediatR;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

/// <summary>
/// Creates a LabelQueueItem pre-loaded with both the barcode-side and front-label images.
/// Used from AddItems when the user manually captures the front of a product.
/// Any previous pending LabelQueueItem for the same UPC is superseded (reset to Pending).
/// </summary>
public record QueueLabelScanWithFrontPhotoCommand(
    string Upc,
    string? BarcodeSideImageData,
    string FrontImageData,
    string StoragePath
) : IRequest<int>;

public class QueueLabelScanWithFrontPhotoHandler : IRequestHandler<QueueLabelScanWithFrontPhotoCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QueueLabelScanWithFrontPhotoHandler> _logger;

    public QueueLabelScanWithFrontPhotoHandler(ApplicationDbContext context, ILogger<QueueLabelScanWithFrontPhotoHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> Handle(QueueLabelScanWithFrontPhotoCommand request, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(request.StoragePath))
            Directory.CreateDirectory(request.StoragePath);

        string? imagePath1 = SaveImage(request.BarcodeSideImageData, request.StoragePath, "label1");
        string? imagePath2 = SaveImage(request.FrontImageData, request.StoragePath, "label2");

        // If a pending LabelQueueItem already exists for this UPC (e.g. created by ScanProcessingHostedService),
        // attach the front photo to it and reset for reprocessing.
        var existing = _context.LabelQueueItems
            .Where(q => q.Upc == request.Upc && q.Status == LabelScanStatus.Pending)
            .OrderByDescending(q => q.Timestamp)
            .FirstOrDefault();

        if (existing != null)
        {
            if (imagePath2 != null) existing.ImagePath2 = imagePath2;
            // If existing has no barcode image but we have one, set it
            if (existing.ImagePath == null && imagePath1 != null) existing.ImagePath = imagePath1;
            existing.Status = LabelScanStatus.Pending; // ensure it gets picked up
            await _context.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        // Otherwise create a fresh entry
        var item = new LabelQueueItem
        {
            Upc = request.Upc,
            ImagePath  = imagePath1,
            ImagePath2 = imagePath2,
            Category = null, // unknown from this context
            Status = LabelScanStatus.Pending,
            Timestamp = DateTime.UtcNow
        };
        _context.LabelQueueItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    private static string? SaveImage(string? dataUrl, string storagePath, string prefix)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;
        try
        {
            var base64 = dataUrl.Contains(',') ? dataUrl[(dataUrl.IndexOf(',') + 1)..] : dataUrl;
            var fileName = $"{prefix}_{Guid.NewGuid():N}.jpg";
            File.WriteAllBytes(Path.Combine(storagePath, fileName), Convert.FromBase64String(base64));
            return $"/uploads/scans/{fileName}";
        }
        catch { return null; }
    }
}
