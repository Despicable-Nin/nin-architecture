using espasyo.Application.Common.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories;

public class IncidentRepository(ApplicationDbContext context) : IIncidentRepository
{
    public async Task<IEnumerable<Incident>> GetAllIncidentsAsync(KeyValuePair<DateOnly, DateOnly>? dateRange = null)
    {
        dateRange ??= new KeyValuePair<DateOnly, DateOnly>(DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now));
        
        var query = context.Incidents.AsQueryable();
        
        var startDate = dateRange.Value.Key.ToDateTime(TimeOnly.MinValue);
        var endDate = dateRange.Value.Value.ToDateTime(TimeOnly.MaxValue);

        query = query.Where(i => i.TimeStamp >= startDate && i.TimeStamp <= endDate);

        return await query.ToArrayAsync();
    }

    public async Task<(IEnumerable<Incident>, int count)> GetPaginatedIncidentsAsync(int pageNumber, int pageSize)
    {
        var count = context.Incidents.Count();
        return (await context.Incidents.OrderByDescending(x => x.TimeStamp).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArrayAsync(), count);
    }

    public async Task<Incident?> GetIncidentByCaseIdAsync(string caseId)
    {
        return await context.Incidents.FirstOrDefaultAsync(x => x.CaseId == caseId);
    }

    public Task<Incident?> GetIncidentByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Incident> CreateIncidentAsync(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        context.Incidents.Add(incident);

        await context.SaveChangesAsync(CancellationToken.None);
        return incident;
    }
    
    public async Task<Incident?> UpdateIncidentAsync(Incident incident)
    {
         context.Incidents.Update(incident);
         await context.SaveChangesAsync(CancellationToken.None);
         return incident;
    }

    public Dictionary<int, string> GetCrimeTypes()
    {
        return Enum.GetValues<CrimeTypeEnum>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }

    public Dictionary<int, string> GetSeverityEnums()
    {
        return Enum.GetValues<SeverityEnum>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }

    public Dictionary<int, string> GetMotiveEnums()
    {
        return Enum.GetValues<MotiveEnum>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }

    public Dictionary<int, string> PoliceDistrictEnums()
    {
        return Enum.GetValues<MuntinlupaPoliceDistrictEnum>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }

    public Dictionary<int, string> GetWeatherEnums()
    {
        return Enum.GetValues<WeatherConditionEnum>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }

    public void Dispose()
    {
        context.Dispose();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync(default);
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
    }
}