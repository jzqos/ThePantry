using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record IncrementInventoryByScannerCommand(
    string Upc,
    int QuantityToAdd = 1,
    string? Category = null
) : IRequest<InventoryItem>;

public class IncrementInventoryByScannerHandler : IRequestHandler<IncrementInventoryByScannerCommand, InventoryItem>
{
    private readonly ApplicationDbContext _context;
    private readonly IProductLookupService _productLookupService;
    
    public IncrementInventoryByScannerHandler(ApplicationDbContext context, IProductLookupService productLookupService)
    {
        _context = context;
        _productLookupService = productLookupService;
    }
    
    public async Task<InventoryItem> Handle(IncrementInventoryByScannerCommand request, CancellationToken cancellationToken)
    {
        if (!IsValidBarcode(request.Upc))
            throw new ArgumentException($"Invalid barcode: {request.Upc}");

        var item = await _context.InventoryItems
            .Include(i => i.Skus)
            .FirstOrDefaultAsync(i => i.Skus.Any(s => s.Sku == request.Upc), cancellationToken);
        
        if (item == null)
        {
            var lookupResult = await _productLookupService.LookupAsync(request.Upc, cancellationToken);
            
            item = new InventoryItem
            {
                Name = lookupResult?.Name ?? $"Unknown Product ({request.Upc})",
                Description = lookupResult?.Description ?? lookupResult?.Brand,
                ImageUrl = lookupResult?.ImageUrl,
                Category = request.Category ?? "Pantry",
                MinimumThreshold = 1,
                CreatedDate = DateTime.UtcNow
            };
            
            item.Skus.Add(new ProductSku { Sku = request.Upc });
            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // If a specific category was provided and the item is still at the default, update it
        if (!string.IsNullOrWhiteSpace(request.Category) && item.Category is "Pantry" or "Uncategorized")
        {
            item.Category = request.Category;
        }

        for (int i = 0; i < request.QuantityToAdd; i++)
        {
            _context.StockEntries.Add(new StockEntry { InventoryItemId = item.Id });
        }
        
        item.LastModifiedDate = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);

        return item;
    }

    private static bool IsValidBarcode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length < 4) return false;
        foreach (var c in code)
            if (c < 0x20 || c > 0x7E) return false;
        return code.Count(char.IsLetterOrDigit) >= code.Length / 2;
    }
}