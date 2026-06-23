using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories;

public class ForecastRepository : IForecastRepository
{
    private readonly ApplicationDbContext _context;

    public ForecastRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ForecastRun> SaveForecastRunAsync(ForecastRun forecastRun)
    {
        _context.ForecastRuns.Add(forecastRun);
        await _context.SaveChangesAsync();
        return forecastRun;
    }

    public async Task SaveForecastResultsAsync(IEnumerable<ForecastResult> results)
    {
        _context.ForecastResults.AddRange(results);
        await _context.SaveChangesAsync();
    }

    public async Task<ForecastRun?> GetForecastRunByIdAsync(Guid id)
    {
        return await _context.ForecastRuns
            .Include(f => f.Precinct)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<ForecastRun>> GetForecastRunsAsync(int page = 1, int pageSize = 20)
    {
        return await _context.ForecastRuns
            .Include(f => f.Precinct)
            .OrderByDescending(f => f.RunAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<ForecastResult>> GetForecastResultsAsync(Guid forecastRunId)
    {
        return await _context.ForecastResults
            .Where(r => r.ForecastRunId == forecastRunId)
            .OrderBy(r => r.Year)
            .ThenBy(r => r.Month)
            .ThenBy(r => r.Precinct)
            .ToListAsync();
    }

    public async Task<bool> DeleteForecastRunAsync(Guid id)
    {
        var run = await _context.ForecastRuns.FindAsync(id);
        if (run == null) return false;

        // Cascade delete will handle ForecastResults
        _context.ForecastRuns.Remove(run);
        await _context.SaveChangesAsync();
        return true;
    }
}
