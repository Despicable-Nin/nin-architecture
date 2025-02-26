using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories;

public class StreetRepository(ApplicationDbContext context) : IStreetRepository
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