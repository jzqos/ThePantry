using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record AddCategoryCommand(string Name) : IRequest<bool>;

public class AddCategoryHandler : IRequestHandler<AddCategoryCommand, bool>
{
    private readonly ApplicationDbContext _context;

    public AddCategoryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return false;

        var exists = await _context.Categories.AnyAsync(c => c.Name == name, cancellationToken);
        if (exists) return false;

        _context.Categories.Add(new StoredCategory { Name = name });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
