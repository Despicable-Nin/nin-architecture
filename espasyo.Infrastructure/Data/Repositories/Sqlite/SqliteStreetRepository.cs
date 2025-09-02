using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories.Sqlite;

public class SqliteStreetRepository(SqliteApplicationDbContext context) : IStreetRepository
{
    public async Task<IEnumerable<Street>> GetAllStreetsAsync()
    {
        return await context.Streets.ToListAsync();
    }

    public bool CreateStreets(IEnumerable<Street> streets)
    {
        context.AddRange(streets);
        return context.SaveChanges() > 0;
    }
}
