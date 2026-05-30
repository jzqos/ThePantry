using MediatR;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record QueueLabelScanCommand(
    string Upc,
    string? ImageData,   // base64 data URL
    string? Category,
    string StoragePath
) : IRequest<int>;

public class QueueLabelScanHandler : IRequestHandler<QueueLabelScanCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QueueLabelScanHandler> _logger;

    public QueueLabelScanHandler(ApplicationDbContext context, ILogger<QueueLabelScanHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> Handle(QueueLabelScanCommand request, CancellationToken cancellationToken)
    {
        string? imagePath = null;

        if (!string.IsNullOrWhiteSpace(request.ImageData))
        {
            try
            {
                if (!Directory.Exists(request.StoragePath))
                    Directory.CreateDirectory(request.StoragePath);

                var fileName = $"label_{Guid.NewGuid():N}.jpg";
                var fullPath = Path.Combine(request.StoragePath, fileName);

                var base64 = request.ImageData;
                if (base64.Contains(',')) base64 = base64[(base64.IndexOf(',') + 1)..];
                await File.WriteAllBytesAsync(fullPath, Convert.FromBase64String(base64), cancellationToken);

                imagePath = $"/uploads/scans/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save label image for UPC {Upc}", request.Upc);
            }
        }

        var item = new LabelQueueItem
        {
            Upc = request.Upc,
            ImagePath = imagePath,
            Category = request.Category,
            Status = LabelScanStatus.Pending,
            Timestamp = DateTime.UtcNow
        };

        _context.LabelQueueItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item.Id;
    }
}
