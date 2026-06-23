using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories.Sqlite;

public class SqlitePrecinctRepository : IPrecinctRepository
{
    private readonly SqliteApplicationDbContext _context;

    public SqlitePrecinctRepository(SqliteApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Precinct>> GetAllAsync()
    {
        return await _context.Precincts
            .Where(p => p.IsActive)
            .OrderBy(p => p.Code)
            .ToListAsync();
    }

    public async Task<Precinct?> GetByIdAsync(Guid id)
    {
        return await _context.Precincts
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Precinct?> GetByBarangayAsync(Barangay barangay)
    {
        return await _context.Precincts
            .FirstOrDefaultAsync(p => p.Barangay == barangay);
    }
}
