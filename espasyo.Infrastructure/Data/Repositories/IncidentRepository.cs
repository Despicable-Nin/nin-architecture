using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

    public async Task<IEnumerable<Incident>> GetFilteredIncidentsAsync(KeyValuePair<DateOnly, DateOnly>? dateRange = null, string[]? crimeTypes = null, string[]? motives = null, string[]? weathers = null, string[]? policeDistricts = null, string[]? severities = null)
    {
        dateRange ??= new KeyValuePair<DateOnly, DateOnly>(DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now));

        var query = context.Incidents.AsNoTracking();

        var startDate = dateRange.Value.Key.ToDateTime(TimeOnly.MinValue);
        var endDate = dateRange.Value.Value.ToDateTime(TimeOnly.MaxValue);

        var dateRangeQuery = query.Where(i => i.TimeStamp >= startDate && i.TimeStamp <= endDate);

        var crimeFilteredQuery = FilterByEnum<CrimeTypeEnum>(dateRangeQuery, crimeTypes, "CrimeType");
        var motiveFilteredQuery = FilterByEnum<MotiveEnum>(dateRangeQuery, motives, "Motive");
        var weatherFilteredQuery = FilterByEnum<WeatherConditionEnum>(dateRangeQuery, weathers, "Weather");
        var precinctFilteredQuery = FilterByEnum<Barangay>(dateRangeQuery, policeDistricts, "PoliceDistrict");
        var severityFilteredQuery =  FilterByEnum<SeverityEnum>(dateRangeQuery, severities, "Severity");

        IEnumerable<Incident> result = [];

        if (crimeFilteredQuery != null)
            result = result.Union(crimeFilteredQuery);
        if (motiveFilteredQuery != null)
            result = result.Union(motiveFilteredQuery);
        if (weatherFilteredQuery != null)
            result = result.Union(weatherFilteredQuery);
        if (precinctFilteredQuery != null)
            result = result.Union(precinctFilteredQuery);
        if (severityFilteredQuery != null)
            result = result.Union(severityFilteredQuery);
        
        result = result.Distinct();

        var filteredIncidentsAsync = result as Incident[] ?? result.ToArray();
        if (filteredIncidentsAsync.Count() == 0)
        {
            return await dateRangeQuery.ToArrayAsync();
        }

        return filteredIncidentsAsync;
    }

    private static IEnumerable<Incident>? FilterByEnum<TEnum>(IQueryable<Incident> incidents, string[]? filterEnums, string? nameOfEnum = "")
      where TEnum : Enum
    {
        if (filterEnums != null && filterEnums.Length > 0)
        {
            var temp = filterEnums.Select(x => (TEnum)Enum.Parse(typeof(TEnum), x)).ToArray();
            var param = Expression.Parameter(typeof(Incident), "x");
            if (nameOfEnum != null)
            {
                var property = Expression.Property(param, nameOfEnum);
                var containsMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(TEnum));
                var containsExpression = Expression.Call(containsMethod, Expression.Constant(temp), property);
                var lambda = Expression.Lambda<Func<Incident, bool>>(containsExpression, param);
                incidents = incidents.Where(lambda);
                return incidents;
            }
        }

        return null;
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
        return Enum.GetValues<Barangay>()
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
        return await context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
    }
}