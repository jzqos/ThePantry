using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;

namespace ThePantry.Application.Queries;

public record GetCategoriesQuery : IRequest<List<string>>;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, List<string>>
{
    private readonly ApplicationDbContext _context;

    public GetCategoriesHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var stored = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);

        // Merge with any categories already in use that aren't in the stored list
        var inUse = await _context.InventoryItems
            .Select(i => i.Category)
            .Distinct()
            .ToListAsync(cancellationToken);

        return stored.Union(inUse).OrderBy(c => c).ToList();
    }
}
