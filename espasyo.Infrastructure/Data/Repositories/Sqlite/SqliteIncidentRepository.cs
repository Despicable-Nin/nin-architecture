using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories.Sqlite;

public class SqliteIncidentRepository(SqliteApplicationDbContext context) : IIncidentRepository
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

    public async Task<IEnumerable<Incident>> GetFilteredIncidentsAsync(KeyValuePair<DateOnly, DateOnly>? dateRange = null,
        string[]? crimeTypes = null,
        string[]? motives = null,
        string[]? weathers = null,
        string[]? policeDistricts = null,
        string[]? severities = null)
    {
        dateRange ??= new (DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now));

        var query = context.Incidents.AsNoTracking();

        var startDate = dateRange.Value.Key.ToDateTime(TimeOnly.MinValue);
        var endDate = dateRange.Value.Value.ToDateTime(TimeOnly.MaxValue);

        query = query.Where(i => i.TimeStamp >= startDate && i.TimeStamp <= endDate);

        if (crimeTypes is { Length: > 0 })
        {
            var crimeTypesEnum = ConvertToEnumArray<CrimeTypeEnum>(crimeTypes);
            query = query.Where(i => crimeTypesEnum.Contains(i.CrimeType));
        }

        if (motives is { Length: > 0 })
        {
            var motiveTypesEnum = ConvertToEnumArray<MotiveEnum>(motives);
            query = query.Where(i => motiveTypesEnum.Contains(i.Motive));
        }

        if (weathers is { Length: > 0 })
        {
            var weatherTypesEmum = ConvertToEnumArray<WeatherConditionEnum>(weathers);
            query = query.Where(i => weatherTypesEmum.Contains(i.Weather));
        }

        if (policeDistricts is { Length: > 0 })
        {
            var precincts = ConvertToEnumArray<Barangay>(policeDistricts);
            query = query.Where(i => precincts.Contains(i.PoliceDistrict));
        }

        if (severities is { Length: > 0 })
        {
            var severityTypesEnum = ConvertToEnumArray<SeverityEnum>(severities);
            query = query.Where(i => severityTypesEnum.Contains(i.Severity));
        }

        return await query.ToArrayAsync() ?? [];
    }

    public async Task<(IEnumerable<Incident>, int count)> GetPaginatedIncidentsAsync(string search, int pageNumber, int pageSize)
    {
        var query = context.Incidents.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(i => i.CaseId!.Contains(search) || search.Contains(i.CaseId));
        }

        int count = await query.CountAsync();

        var incidents = await query.OrderByDescending(x => x.TimeStamp)
                                   .Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToArrayAsync();

        return (incidents, count);
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

        // Check if CaseId already exists to prevent constraint violation
        if (!string.IsNullOrEmpty(incident.CaseId))
        {
            var existingIncident = await context.Incidents
                .Where(i => i.CaseId == incident.CaseId)
                .FirstOrDefaultAsync();
                
            if (existingIncident != null)
            {
                throw new InvalidOperationException($"An incident with CaseId '{incident.CaseId}' already exists.");
            }
        }

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

    public async Task<bool> RemoveAllIncidentsAsync()
    {
        try
        {
            var incidents = context.Incidents;
            context.Incidents.RemoveRange(incidents);
            await context.SaveChangesAsync(CancellationToken.None);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
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
        return Enum.GetValues<Barangay>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }

    public Dictionary<int, string> GetWeatherEnums()
    {
        return Enum.GetValues<WeatherConditionEnum>()
            .ToDictionary(e => (int)e, e => e.ToString());
    }
    
    private static TEnum[] ConvertToEnumArray<TEnum>(IEnumerable<string> values) where TEnum : struct, Enum
    {
        return values.Select(x => (TEnum)Enum.Parse(typeof(TEnum), x)).ToArray();
    }
}
