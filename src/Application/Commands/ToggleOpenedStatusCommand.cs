using Microsoft.EntityFrameworkCore;
using MediatR;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record ToggleOpenedStatusCommand(int InventoryItemId) : IRequest<InventoryItem?>;

public class ToggleOpenedStatusHandler : IRequestHandler<ToggleOpenedStatusCommand, InventoryItem?>
{
    private readonly ApplicationDbContext _context;

    public ToggleOpenedStatusHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryItem?> Handle(ToggleOpenedStatusCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.StockEntries)
            .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, cancellationToken);

        if (item == null || !item.StockEntries.Any())
            return null;

        if (item.StockEntries.Any(s => s.IsOpened))
        {
            // Close: remove the most recently opened unit entirely (it's been consumed)
            var toClose = item.StockEntries
                .Where(s => s.IsOpened)
                .OrderByDescending(s => s.OpenedDate)
                .First();
            _context.StockEntries.Remove(toClose);
        }
        else
        {
            // Open: remove the oldest sealed unit and replace it with an opened one
            var toOpen = item.StockEntries
                .Where(s => !s.IsOpened)
                .OrderBy(s => s.AddedDate)
                .First();
            _context.StockEntries.Remove(toOpen);
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