using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
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
            .OrderBy(m => m.Year)
            .ThenBy(m => m.PrecinctEnum)
            .ToListAsync();
    }

    public async Task<IEnumerable<Manpower>> GetByYearAsync(int year)
    {
        return await _context.Manpowers
            .Include(m => m.Precinct)
            .Where(m => m.Year == year)
            .OrderBy(m => m.PrecinctEnum)
            .ToListAsync();
    }

    public async Task<Manpower?> GetByPrecinctAndYearAsync(Barangay precinct, int year)
    {
        return await _context.Manpowers
            .Include(m => m.Precinct)
            .FirstOrDefaultAsync(m => m.PrecinctEnum == precinct && m.Year == year);
    }

    public async Task<IEnumerable<Manpower>> GetByPrecinctAsync(Barangay precinct)
    {
        return await _context.Manpowers
            .Include(m => m.Precinct)
            .Where(m => m.PrecinctEnum == precinct)
            .OrderBy(m => m.Year)
            .ToListAsync();
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

    public async Task<bool> ExistsAsync(Barangay precinct, int year)
    {
        return await _context.Manpowers
            .AnyAsync(m => m.PrecinctEnum == precinct && m.Year == year);
    }

    public async Task<Dictionary<Barangay, int>> GetTotalManpowerByPrecinctAsync(int year)
    {
        return await _context.Manpowers
            .Where(m => m.Year == year)
            .GroupBy(m => m.PrecinctEnum)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Sum(m => m.AllocatedCount)
            );
    }

    public async Task<IEnumerable<Manpower>> GetManpowerRequiringAdjustmentAsync(int year, Dictionary<Barangay, int> predictedCaseCounts)
    {
        var manpowerData = await GetByYearAsync(year);
        
        return manpowerData.Where(m => 
            predictedCaseCounts.ContainsKey(m.PrecinctEnum) && 
            m.RequiresManpowerAdjustment(predictedCaseCounts[m.PrecinctEnum])
        );
    }
}