using espasyo.Application.Common.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using espasyo.Domain.Events;
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

    public Task<Incident> CreateIncidentAsync(Incident incident)
    {
        if(incident == null) throw new ArgumentNullException(nameof(incident));
        incident.AddDomainEvent(new IncidentCreatedEvent(incident.CaseId, incident.Address));
        return Task.FromResult(context.Incidents.Add(incident).Entity);
    }
    
    public async Task<Incident?> UpdateIncidentAsync(Incident incident)
    {
        return context.Incidents.Update(incident).Entity;
    }

    public Dictionary<int, string> GetCrimeTypes()
    {
        return Enum.GetValues(typeof(CrimeTypeEnum))
            .Cast<CrimeTypeEnum>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }

    public Dictionary<int, string> GetSeverityEnums()
    {
        return Enum.GetValues(typeof(SeverityEnum))
            .Cast<SeverityEnum>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }

    public Dictionary<int, string> GetMotiveEnums()
    {
        return Enum.GetValues(typeof(MotiveEnum))
            .Cast<MotiveEnum>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }

    public Dictionary<int, string> PoliceDistrictEnums()
    {
        return Enum.GetValues(typeof(MuntinlupaPoliceDistrictEnum))
            .Cast<MuntinlupaPoliceDistrictEnum>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }

    public Dictionary<int, string> GetWeatherEnums()
    {
        return Enum.GetValues(typeof(WeatherConditionEnum))
            .Cast<WeatherConditionEnum>()
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