using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;

namespace ThePantry.Application.Commands;

public record MoveItemsToCategoryCommand(List<int> ItemIds, string Category) : IRequest<bool>;

public class MoveItemsToCategoryHandler : IRequestHandler<MoveItemsToCategoryCommand, bool>
{
    private readonly ApplicationDbContext _context;

    public MoveItemsToCategoryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(MoveItemsToCategoryCommand request, CancellationToken cancellationToken)
    {
        if (!request.ItemIds.Any()) return false;

        var items = await _context.InventoryItems
            .Where(i => request.ItemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.Category = request.Category;
            item.LastModifiedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
