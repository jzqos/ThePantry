using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record AppendLabelPhotoCommand(
    int QueueItemId,
    string ImageData,
    string StoragePath
) : IRequest<bool>;

public class AppendLabelPhotoHandler : IRequestHandler<AppendLabelPhotoCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppendLabelPhotoHandler> _logger;

    public AppendLabelPhotoHandler(ApplicationDbContext context, ILogger<AppendLabelPhotoHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(AppendLabelPhotoCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.LabelQueueItems
            .FirstOrDefaultAsync(q => q.Id == request.QueueItemId, cancellationToken);

        if (item == null) return false;

        // Save the image file
        try
        {
            if (!Directory.Exists(request.StoragePath))
                Directory.CreateDirectory(request.StoragePath);

            var fileName = $"label2_{Guid.NewGuid():N}.jpg";
            var fullPath = Path.Combine(request.StoragePath, fileName);

            var base64 = request.ImageData;
            if (base64.Contains(',')) base64 = base64[(base64.IndexOf(',') + 1)..];
            await File.WriteAllBytesAsync(fullPath, Convert.FromBase64String(base64), cancellationToken);

            // Delete old ImagePath2 file if one was already attached
            if (!string.IsNullOrWhiteSpace(item.ImagePath2))
            {
                var oldFile = Path.Combine(request.StoragePath, Path.GetFileName(item.ImagePath2));
                try { if (File.Exists(oldFile)) File.Delete(oldFile); } catch { /* best effort */ }
            }

            item.ImagePath2 = $"/uploads/scans/{fileName}";

            // If item already failed (recognition had no image2 yet), reset it to Pending so it gets retried
            if (item.Status == LabelScanStatus.Failed || item.Status == LabelScanStatus.Complete)
                item.Status = LabelScanStatus.Pending;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save front-label image for queue item {Id}", request.QueueItemId);
            return false;
        }
    }
}
