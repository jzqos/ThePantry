using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Queries;

public record GetLabelQueueQuery : IRequest<List<LabelQueueItemDto>>;

public class LabelQueueItemDto
{
    public int Id { get; set; }
    public string Upc { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public string? Category { get; set; }
    public LabelScanStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ResultName { get; set; }
    public string? ResultSpecies { get; set; }
    public string? ResultWeight { get; set; }
    public string? ErrorMessage { get; set; }
    public int? LinkedInventoryItemId { get; set; }
}

public class GetLabelQueueHandler : IRequestHandler<GetLabelQueueQuery, List<LabelQueueItemDto>>
{
    private readonly ApplicationDbContext _context;

    public GetLabelQueueHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LabelQueueItemDto>> Handle(GetLabelQueueQuery request, CancellationToken cancellationToken)
    {
        return await _context.LabelQueueItems
            .OrderByDescending(q => q.Timestamp)
            .Take(100)
            .Select(q => new LabelQueueItemDto
            {
                Id = q.Id,
                Upc = q.Upc,
                ImagePath = q.ImagePath,
                Category = q.Category,
                Status = q.Status,
                Timestamp = q.Timestamp,
                ResultName = q.ResultName,
                ResultSpecies = q.ResultSpecies,
                ResultWeight = q.ResultWeight,
                ErrorMessage = q.ErrorMessage,
                LinkedInventoryItemId = q.LinkedInventoryItemId
            })
            .ToListAsync(cancellationToken);
    }
}
