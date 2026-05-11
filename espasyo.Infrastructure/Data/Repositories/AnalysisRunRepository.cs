using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories;

public class AnalysisRunRepository : IAnalysisRunRepository
{
    private readonly ApplicationDbContext _context;

    public AnalysisRunRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisRun> SaveAsync(AnalysisRun run)
    {
        _context.AnalysisRuns.Add(run);
        await _context.SaveChangesAsync();
        return run;
    }

    public async Task<AnalysisRun?> GetByIdAsync(Guid id)
    {
        return await _context.AnalysisRuns
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<AnalysisRun>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _context.AnalysisRuns
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var run = await _context.AnalysisRuns.FindAsync(id);
        if (run == null) return false;

        _context.AnalysisRuns.Remove(run);
        await _context.SaveChangesAsync();
        return true;
    }
}
