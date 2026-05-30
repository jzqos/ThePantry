using MediatR;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record AddItemFromLabelCommand(
    string Upc,
    string Name,
    string? Species,
    string? Weight,
    string? Category,
    int ShelfLifeDays = 30
) : IRequest<InventoryItem>;

public class AddItemFromLabelHandler : IRequestHandler<AddItemFromLabelCommand, InventoryItem>
{
    private readonly ApplicationDbContext _context;

    public AddItemFromLabelHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryItem> Handle(AddItemFromLabelCommand request, CancellationToken cancellationToken)
    {
        var displayName = string.IsNullOrWhiteSpace(request.Species)
            ? request.Name
            : $"{request.Name} ({request.Species})";

        var description = request.Weight != null ? $"Weight: {request.Weight}" : null;

        var item = new InventoryItem
        {
            Name = displayName,
            Description = description,
            Category = request.Category ?? "Refrigerator",
            MinimumThreshold = 1,
            ShelfLifeDays = request.ShelfLifeDays,
            UseWithinDays = 2,
            CreatedDate = DateTime.UtcNow
        };

        item.Skus.Add(new ProductSku { Sku = request.Upc });
        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        _context.StockEntries.Add(new StockEntry { InventoryItemId = item.Id });
        item.LastModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return item;
    }
}
