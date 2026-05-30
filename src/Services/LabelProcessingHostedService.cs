using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Services;

public class LabelProcessingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LabelProcessingHostedService> _logger;
    private readonly string _scanStoragePath;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(8);

    public LabelProcessingHostedService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<LabelProcessingHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _scanStoragePath = configuration["ScanStoragePath"] ?? "/uploads/scans";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Label Processing Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingItems(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing label queue");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingItems(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var recognitionService = scope.ServiceProvider.GetRequiredService<ILabelRecognitionService>();
        var appSettings = scope.ServiceProvider.GetRequiredService<AppSettingsService>();

        // Only run if a provider is configured
        var provider = await appSettings.GetAsync("LLM_PROVIDER", ct);
        if (string.IsNullOrWhiteSpace(provider)) return;

        var pending = await context.LabelQueueItems
            .Where(q => q.Status == LabelScanStatus.Pending)
            .OrderBy(q => q.Timestamp)
            .Take(5)
            .ToListAsync(ct);

        foreach (var queueItem in pending)
        {
            try
            {
                queueItem.Status = LabelScanStatus.Processing;
                await context.SaveChangesAsync(ct);

                LabelRecognitionResult? result = null;

                var dataUrl1 = LoadAndDeleteImage(queueItem.ImagePath, out var used1);
                var dataUrl2 = LoadAndDeleteImage(queueItem.ImagePath2, out var used2);

                _logger.LogInformation("Label queue item {Id}: image1={HasImg1}, image2={HasImg2}, storagePath={Path}",
                    queueItem.Id, dataUrl1 != null, dataUrl2 != null, _scanStoragePath);

                if (dataUrl1 != null || dataUrl2 != null)
                {
                    result = await recognitionService.RecognizeAsync(dataUrl1 ?? dataUrl2!, dataUrl1 != null ? dataUrl2 : null, ct);
                }
                else
                {
                    _logger.LogWarning("Label queue item {Id}: no images available at {Path1} / {Path2}",
                        queueItem.Id, queueItem.ImagePath, queueItem.ImagePath2);
                }

                if (used1) queueItem.ImagePath  = null;
                if (used2) queueItem.ImagePath2 = null;

                if (result != null && !string.IsNullOrWhiteSpace(result.Name))
                {
                    queueItem.ResultName = result.Name;
                    queueItem.ResultSpecies = result.Species;
                    queueItem.ResultWeight = result.Weight;

                    var displayName = string.IsNullOrWhiteSpace(result.Species)
                        ? result.Name
                        : $"{result.Name} ({result.Species})";
                    var description = result.Weight != null ? $"Weight: {result.Weight}" : null;

                    // Check if this UPC already has an inventory item (e.g. added via another path)
                    var existing = await context.InventoryItems
                        .Include(i => i.Skus)
                        .FirstOrDefaultAsync(i => i.Skus.Any(s => s.Sku == queueItem.Upc), ct);

                    if (existing != null)
                    {
                        context.StockEntries.Add(new StockEntry { InventoryItemId = existing.Id });
                        existing.LastModifiedDate = DateTime.UtcNow;
                        queueItem.LinkedInventoryItemId = existing.Id;
                        _logger.LogInformation("UPC {Upc} already in inventory — incremented stock for '{Name}'", queueItem.Upc, existing.Name);
                    }
                    else
                    {
                        var item = new InventoryItem
                        {
                            Name = displayName,
                            Description = description,
                            Category = queueItem.Category ?? "Refrigerator",
                            MinimumThreshold = 1,
                            ShelfLifeDays = 90,
                            UseWithinDays = 2,
                            CreatedDate = DateTime.UtcNow
                        };
                        item.Skus.Add(new ProductSku { Sku = queueItem.Upc });
                        context.InventoryItems.Add(item);
                        await context.SaveChangesAsync(ct);

                        context.StockEntries.Add(new StockEntry { InventoryItemId = item.Id });
                        item.LastModifiedDate = DateTime.UtcNow;
                        queueItem.LinkedInventoryItemId = item.Id;
                        _logger.LogInformation("Label recognition complete for UPC {Upc}: '{Name}'", queueItem.Upc, displayName);
                    }

                    queueItem.Status = LabelScanStatus.Complete;
                }
                else
                {
                    queueItem.Status = LabelScanStatus.Failed;
                    queueItem.ErrorMessage = "Could not extract product details from label image.";
                    _logger.LogWarning("Label recognition failed for UPC {Upc}", queueItem.Upc);
                }

                await context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing label queue item {Id}", queueItem.Id);
                queueItem.Status = LabelScanStatus.Failed;
                queueItem.ErrorMessage = ex.Message;
                await context.SaveChangesAsync(ct);
            }
        }
    }

    private string? LoadAndDeleteImage(string? imagePath, out bool used)
    {
        used = false;
        if (string.IsNullOrWhiteSpace(imagePath)) return null;
        // imagePath is stored as "/uploads/scans/filename.jpg"; resolve to filesystem path
        var fileName = Path.GetFileName(imagePath);
        var fsPath = Path.Combine(_scanStoragePath, fileName);
        if (!File.Exists(fsPath))
        {
            _logger.LogDebug("Image file not found at {Path}", fsPath);
            return null;
        }
        try
        {
            var bytes = File.ReadAllBytes(fsPath);
            used = true;
            try { File.Delete(fsPath); } catch { /* best effort */ }
            return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load image {Path}", fsPath);
            return null;
        }
    }
}
