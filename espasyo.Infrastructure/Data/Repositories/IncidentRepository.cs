using espasyo.Application.Common.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
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

    public Task<Incident?> GetIncidentByCaseIdAsync(string caseId)
    {
        throw new NotImplementedException();
    }

    public Task<Incident?> GetIncidentByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Guid?> CreateIncidentAsync(Incident incident)
    {
        Guid? incidentId = null;
        try
        {
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();
           incidentId = incident.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return incidentId;
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
}