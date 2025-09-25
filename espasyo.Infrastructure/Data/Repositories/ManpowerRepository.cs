using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories;

public class ManpowerRepository : IManpowerRepository
{
    private readonly ApplicationDbContext _context;

    public ManpowerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Manpower>> GetAllManpowerAsync()
    {
        return await _context.Manpowers
            .Include(m => m.Precinct)
            .OrderBy(m => m.Precinct.Name)
            .ToListAsync();
    }

    public async Task<Manpower?> GetByPrecinctIdAsync(Guid precinctId)
    {
        return await _context.Manpowers
            .Include(m => m.Precinct)
            .FirstOrDefaultAsync(m => m.PrecinctId == precinctId);
    }

    public async Task<Manpower?> GetByIdAsync(Guid id)
    {
        return await _context.Manpowers
            .Include(m => m.Precinct)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Manpower> CreateAsync(Manpower manpower)
    {
        _context.Manpowers.Add(manpower);
        await _context.SaveChangesAsync();
        return manpower;
    }

    public async Task<Manpower?> UpdateAsync(Manpower manpower)
    {
        var existing = await _context.Manpowers.FindAsync(manpower.Id);
        if (existing == null) return null;

        _context.Entry(existing).CurrentValues.SetValues(manpower);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var manpower = await _context.Manpowers.FindAsync(id);
        if (manpower == null) return false;

        _context.Manpowers.Remove(manpower);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByPrecinctIdAsync(Guid precinctId)
    {
        return await _context.Manpowers
            .AnyAsync(m => m.PrecinctId == precinctId);
    }

    public async Task<Dictionary<Guid, int>> GetTotalManpowerByPrecinctAsync()
    {
        return await _context.Manpowers
            .GroupBy(m => m.PrecinctId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Sum(m => m.HeadCount)
            );
    }

    public async Task<IEnumerable<Manpower>> GetManpowerWithShortageAsync(Dictionary<Guid, int> requiredManpower)
    {
        var allManpower = await GetAllManpowerAsync();
        
        return allManpower.Where(m => 
            requiredManpower.ContainsKey(m.PrecinctId) && 
            m.HasShortage(requiredManpower[m.PrecinctId])
        );
    }
}