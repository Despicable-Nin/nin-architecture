using espasyo.Application.Common.Interfaces;
using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories;

public class IncidentRepository(ApplicationDbContext context) : IIncidentRepository
{
    public Task<List<Incident>> GetAllIncidentsAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<(IEnumerable<Incident>, int count)> GetPaginatedIncidentsAsync(int pageNumber, int pageSize)
    {
        var count = context.Incidents.Count();
        return (await context.Incidents.OrderBy(x => x.TimeStamp).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArrayAsync(), count);
    }

    public Task<Incident?> GetIncidentByCaseIdAsync(string caseId)
    {
        throw new NotImplementedException();
    }

    public Task<Incident?> GetIncidentByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Guid> CreateIncidentAsync(Incident incident)
    {
         context.Incidents.Add(incident);
         await context.SaveChangesAsync();
         return incident.Id;
    }
}