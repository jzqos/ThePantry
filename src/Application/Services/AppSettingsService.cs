using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Services;

public class AppSettingsService
{
    private readonly ApplicationDbContext _context;

    public AppSettingsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        return setting?.Value;
    }

    public async Task SetAsync(string key, string? value, CancellationToken ct = default)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting == null)
        {
            _context.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }
        await _context.SaveChangesAsync(ct);
    }
}
