using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;

namespace ThePantry.Application.Commands;

public record DeleteCategoryCommand(string Name) : IRequest<bool>;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly ApplicationDbContext _context;

    public DeleteCategoryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var cat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == request.Name, cancellationToken);
        if (cat == null) return false;
        _context.Categories.Remove(cat);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
