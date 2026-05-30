using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record MarkAsOpenedByScannerCommand(string Upc) : IRequest<InventoryItem?>;

public class MarkAsOpenedByScannerHandler : IRequestHandler<MarkAsOpenedByScannerCommand, InventoryItem?>
{
    private readonly ApplicationDbContext _context;
    private readonly IProductLookupService _productLookupService;

    public MarkAsOpenedByScannerHandler(ApplicationDbContext context, IProductLookupService productLookupService)
    {
        _context = context;
        _productLookupService = productLookupService;
    }

    public async Task<InventoryItem?> Handle(MarkAsOpenedByScannerCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Skus)
            .Include(i => i.StockEntries)
            .FirstOrDefaultAsync(i => i.Skus.Any(s => s.Sku == request.Upc), cancellationToken);

        if (item == null)
        {
            var lookupResult = await _productLookupService.LookupAsync(request.Upc, cancellationToken);
            
            item = new InventoryItem
            {
                Name = lookupResult?.Name ?? $"Unknown Product ({request.Upc})",
                Description = lookupResult?.Description ?? lookupResult?.Brand,
                ImageUrl = lookupResult?.ImageUrl,
                Category = "Uncategorized",
                MinimumThreshold = 1,
                CreatedDate = DateTime.UtcNow
            };
            
            item.Skus.Add(new ProductSku { Sku = request.Upc });
            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync(cancellationToken);

            // Add initial stock entry
            var entry = new StockEntry { InventoryItemId = item.Id, IsOpened = true, OpenedDate = DateTime.UtcNow };
            _context.StockEntries.Add(entry);
        }
        else
        {
            // Remove the oldest unopened unit and add a fresh opened one.
            // This keeps the rest of the stack as sealed/unopened.
            var entryToConsume = item.StockEntries
                .Where(s => !s.IsOpened)
                .OrderBy(s => s.AddedDate)
                .FirstOrDefault()
                ?? item.StockEntries.OrderBy(s => s.AddedDate).FirstOrDefault();

            if (entryToConsume != null)
                _context.StockEntries.Remove(entryToConsume);

            // Add the new opened unit, inheriting shelf-life from the parent item
            _context.StockEntries.Add(new StockEntry
            {
                InventoryItemId = item.Id,
                IsOpened = true,
                OpenedDate = DateTime.UtcNow
            });
        }

        item.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return item;
    }
}