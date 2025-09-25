using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories.Sqlite;

public class SqliteManpowerRepository : IManpowerRepository
{
    private readonly SqliteApplicationDbContext _context;

    public SqliteManpowerRepository(SqliteApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Manpower>> GetAllManpowerAsync()
    {
        return await _context.Manpowers
            .Include(m => m.Precinct)
            .OrderBy(m => m.Precinct.Code)
            .ToListAsync();
    }

    public async Task<Manpower?> GetByPrecinctIdAsync(Guid precinctId)
    {
        return await _context.Manpowers
            .Include(m => m.Precinct)
            .FirstOrDefaultAsync(m => m.PrecinctId == precinctId);
    }

    public async Task<IEnumerable<Manpower>> GetByPrecinctIdAllShiftsAsync(Guid precinctId)
    {
        return await _context.Manpowers
            .Include(m => m.Precinct)
            .Where(m => m.PrecinctId == precinctId)
            .OrderBy(m => m.Shift)
            .ToListAsync();
    }

    public async Task<Manpower?> GetByPrecinctIdAndShiftAsync(Guid precinctId, ShiftEnum shift)
    {
        return await _context.Manpowers
            .Include(m => m.Precinct)
            .FirstOrDefaultAsync(m => m.PrecinctId == precinctId && m.Shift == shift);
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

    public async Task<Manpower> UpsertAsync(Guid precinctId, ShiftEnum shift, int headCount)
    {
        var existing = await GetByPrecinctIdAndShiftAsync(precinctId, shift);
        
        if (existing != null)
        {
            // Update existing record
            existing.UpdateHeadCount(headCount);
            await _context.SaveChangesAsync();
            return existing;
        }
        else
        {
            // Create new record
            var newManpower = new Manpower(precinctId, shift, headCount);
            _context.Manpowers.Add(newManpower);
            await _context.SaveChangesAsync();
            return newManpower;
        }
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

    public async Task<bool> ExistsByPrecinctIdAndShiftAsync(Guid precinctId, ShiftEnum shift)
    {
        return await _context.Manpowers
            .AnyAsync(m => m.PrecinctId == precinctId && m.Shift == shift);
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