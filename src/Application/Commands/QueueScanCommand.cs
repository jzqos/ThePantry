using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record QueueScanCommand(
    string Upc,
    string? RawData = null,
    string? ImageData = null,
    string? Category = null
) : IRequest<ScanQueueItem>;

public class QueueScanHandler : IRequestHandler<QueueScanCommand, ScanQueueItem>
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    
    public QueueScanHandler(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    
    public async Task<ScanQueueItem> Handle(QueueScanCommand request, CancellationToken cancellationToken)
    {
        // Reject garbage barcode reads (control characters, too short)
        if (!IsValidBarcode(request.Upc))
            return new ScanQueueItem { Upc = request.Upc, Status = ScanStatus.Failed, Timestamp = DateTime.UtcNow };

        // Check for recent duplicate scans (within last 5 seconds) to prevent double-queuing
        var recentScan = await _context.ScanQueueItems
            .Where(s => s.Upc == request.Upc && s.Timestamp > DateTime.UtcNow.AddSeconds(-1))
            .FirstOrDefaultAsync(cancellationToken);

        if (recentScan != null)
        {
            return recentScan;
        }

        string? imagePath = null;
        if (!string.IsNullOrEmpty(request.ImageData))
        {
            try
            {
                var base64Data = request.ImageData.Contains(',')
                    ? request.ImageData.Split(',')[1]
                    : request.ImageData;

                var bytes = Convert.FromBase64String(base64Data);
                var fileName = $"scan_{Guid.NewGuid():N}.jpg";
                var storagePath = _configuration["ScanStoragePath"] ?? "/uploads/scans";

                if (!Directory.Exists(storagePath))
                    Directory.CreateDirectory(storagePath);

                await File.WriteAllBytesAsync(Path.Combine(storagePath, fileName), bytes, cancellationToken);

                // Always store a URL-style path so it can be served and resolved consistently
                imagePath = $"/uploads/scans/{fileName}";
            }
            catch (Exception)
            {
                // image capture is best-effort; continue without it
            }
        }

        // Check if we already have this UPC in inventory
        var existingItem = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Skus.Any(s => s.Sku == request.Upc), cancellationToken);
        
        var scanItem = new ScanQueueItem
        {
            Upc = request.Upc,
            RawData = request.RawData,
            Status = ScanStatus.Pending,
            Timestamp = DateTime.UtcNow,
            LinkedInventoryItemId = existingItem?.Id,
            ImagePath = imagePath,
            Category = request.Category
        };
        
        _context.ScanQueueItems.Add(scanItem);
        await _context.SaveChangesAsync(cancellationToken);
        
        return scanItem;
    }

    // Reject clearly invalid reads: too short, or contains non-printable ASCII
    private static bool IsValidBarcode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length < 4) return false;
        foreach (var c in code)
            if (c < 0x20 || c > 0x7E) return false; // non-printable or non-ASCII
        // Reject if more than half the characters are non-alphanumeric (e.g. "*;/& &R")
        var alphanumCount = code.Count(char.IsLetterOrDigit);
        return alphanumCount >= code.Length / 2;
    }
}