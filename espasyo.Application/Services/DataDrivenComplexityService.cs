using espasyo.Application.Interfaces;
using espasyo.Domain.Enums;

namespace espasyo.Application.Services;

/// <summary>
/// Data-driven complexity calculation service that replaces hard-coded values
/// with calculations based on actual database data
/// </summary>
public class DataDrivenComplexityService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IManpowerRepository _manpowerRepository;

    public DataDrivenComplexityService(
        IIncidentRepository incidentRepository,
        IManpowerRepository manpowerRepository)
    {
        _incidentRepository = incidentRepository;
        _manpowerRepository = manpowerRepository;
    }

    /// <summary>
    /// Calculate geographic complexity factors for all precincts based on actual data
    /// </summary>
    public async Task<Dictionary<string, float>> CalculateGeographicComplexityFactorsAsync()
    {
        var complexityFactors = new Dictionary<string, float>();
        
        // Get all precincts with incident data
        var precinctIncidentStats = await GetPrecinctIncidentStatisticsAsync();
        
        if (!precinctIncidentStats.Any())
        {
            return complexityFactors; // Return empty if no data
        }

        // Calculate baseline metrics across all precincts
        var avgIncidentsPerKm2 = precinctIncidentStats.Average(p => p.IncidentDensity);
        var avgIncidentsPerOfficer = precinctIncidentStats.Average(p => p.IncidentsPerOfficer);
        
        foreach (var stat in precinctIncidentStats)
        {
            // Calculate complexity based on multiple data-driven factors
            var densityFactor = stat.IncidentDensity / Math.Max(avgIncidentsPerKm2, 1);
            var workloadFactor = stat.IncidentsPerOfficer / Math.Max(avgIncidentsPerOfficer, 1);
            var crimeTypeFactor = await GetCrimeTypeComplexityFactor(stat.PrecinctId);
            
            // Weighted combination of factors
            var complexityFactor = (float)(
                (densityFactor * 0.4) +      // 40% weight to crime density
                (workloadFactor * 0.4) +     // 40% weight to workload per officer
                (crimeTypeFactor * 0.2)      // 20% weight to crime type complexity
            );
            
            // Normalize to reasonable range (0.5 to 2.0)
            complexityFactor = Math.Max(0.5f, Math.Min(2.0f, complexityFactor));
            
            complexityFactors[stat.PrecinctName] = complexityFactor;
        }

        return complexityFactors;
    }

    /// <summary>
    /// Calculate which crime types are complex based on actual incident data
    /// </summary>
    public async Task<HashSet<string>> CalculateComplexCrimeTypesAsync()
    {
        var complexCrimeTypes = new HashSet<string>();
        
        // Get crime statistics by type across all precincts
        var crimeTypeStats = await GetCrimeTypeStatisticsAsync();
        
        if (!crimeTypeStats.Any())
        {
            return complexCrimeTypes; // Return empty if no data
        }

        // Calculate complexity thresholds based on data distribution
        var avgFrequency = crimeTypeStats.Average(c => c.TotalCount);
        var avgSeverityScore = crimeTypeStats.Average(c => c.SeverityScore);
        
        // Determine complex crimes using statistical analysis
        foreach (var crimeType in crimeTypeStats)
        {
            // A crime type is considered complex if:
            // 1. It's relatively rare (below average frequency) AND has high severity
            // 2. OR it has very high severity regardless of frequency
            var isRareButSevere = crimeType.TotalCount < avgFrequency && 
                                 crimeType.SeverityScore > avgSeverityScore;
            var isHighSeverity = crimeType.SeverityScore > (avgSeverityScore * 1.5);
            
            if (isRareButSevere || isHighSeverity)
            {
                complexCrimeTypes.Add(crimeType.CrimeType.ToString());
            }
        }

        return complexCrimeTypes;
    }

    /// <summary>
    /// Get default complexity factor for precincts not in the calculated list
    /// </summary>
    public async Task<float> CalculateDefaultComplexityFactorAsync()
    {
        var allFactors = await CalculateGeographicComplexityFactorsAsync();
        
        if (!allFactors.Any())
        {
            return 1.0f; // Neutral default
        }

        // Use median of all calculated factors as default
        var sortedFactors = allFactors.Values.OrderBy(f => f).ToList();
        var medianIndex = sortedFactors.Count / 2;
        
        return sortedFactors.Count % 2 == 0
            ? (sortedFactors[medianIndex - 1] + sortedFactors[medianIndex]) / 2
            : sortedFactors[medianIndex];
    }

    private async Task<List<PrecinctIncidentStatistics>> GetPrecinctIncidentStatisticsAsync()
    {
        var statistics = new List<PrecinctIncidentStatistics>();
        
        try
        {
            // Get all incidents with precinct information
            var allIncidents = await _incidentRepository.GetAllIncidentsAsync();
            var allManpower = await _manpowerRepository.GetAllManpowerAsync();
            
            // Group by precinct
            var precinctGroups = allIncidents
                .Where(i => i.Precinct != null)
                .GroupBy(i => i.PrecinctId);

            foreach (var precinctGroup in precinctGroups)
            {
                var precinct = precinctGroup.First().Precinct;
                if (precinct == null) continue;

                var incidentCount = precinctGroup.Count();
                var areaKm2 = precinct.AreaKm2 ?? 1; // Default to 1 if null
                var incidentDensity = (double)incidentCount / (double)areaKm2;

                // Get manpower for this precinct
                var precinctManpower = allManpower
                    .Where(m => m.PrecinctId == precinct.Id)
                    .Sum(m => m.HeadCount);

                var incidentsPerOfficer = precinctManpower > 0 
                    ? (double)incidentCount / precinctManpower 
                    : incidentCount; // If no manpower data, use incident count

                statistics.Add(new PrecinctIncidentStatistics
                {
                    PrecinctId = precinct.Id,
                    PrecinctName = GetPrecinctName(precinct.Barangay),
                    TotalIncidents = incidentCount,
                    AreaKm2 = (double)areaKm2,
                    IncidentDensity = incidentDensity,
                    TotalManpower = precinctManpower,
                    IncidentsPerOfficer = incidentsPerOfficer
                });
            }
        }
        catch (Exception)
        {
            // Log exception and return empty list
            // In production, use proper logging
        }

        return statistics;
    }

    private async Task<double> GetCrimeTypeComplexityFactor(Guid precinctId)
    {
        try
        {
            // Get incidents for this precinct - filter from all incidents
            var allIncidents = await _incidentRepository.GetAllIncidentsAsync();
            var precinctIncidents = allIncidents.Where(i => i.PrecinctId == precinctId);
            
            if (!precinctIncidents.Any())
                return 1.0; // Default complexity

            // Calculate complexity based on crime type severity
            var complexitySum = 0.0;
            foreach (var incident in precinctIncidents)
            {
                complexitySum += GetCrimeTypeSeverityScore(incident.CrimeType);
            }

            return complexitySum / precinctIncidents.Count();
        }
        catch (Exception)
        {
            return 1.0; // Default complexity on error
        }
    }

    private async Task<List<CrimeTypeStatistics>> GetCrimeTypeStatisticsAsync()
    {
        var statistics = new List<CrimeTypeStatistics>();
        
        try
        {
            var allIncidents = await _incidentRepository.GetAllIncidentsAsync();
            
            var crimeTypeGroups = allIncidents.GroupBy(i => i.CrimeType);
            
            foreach (var group in crimeTypeGroups)
            {
                var severityScore = GetCrimeTypeSeverityScore(group.Key);
                
                statistics.Add(new CrimeTypeStatistics
                {
                    CrimeType = group.Key,
                    TotalCount = group.Count(),
                    SeverityScore = severityScore
                });
            }
        }
        catch (Exception)
        {
            // Log exception and return empty list
        }

        return statistics;
    }

    private double GetCrimeTypeSeverityScore(CrimeTypeEnum crimeType)
    {
        // Calculate severity based on the enum value and logical severity
        // This replaces hard-coded crime type complexity
        return crimeType switch
        {
            CrimeTypeEnum.Murder => 10.0,
            CrimeTypeEnum.Homicide => 9.0,
            CrimeTypeEnum.HumanTrafficking => 9.0,
            CrimeTypeEnum.Kidnapping => 8.0,
            CrimeTypeEnum.Corruption => 7.0,
            CrimeTypeEnum.Rape => 8.0,
            CrimeTypeEnum.Arson => 6.0,
            CrimeTypeEnum.DrugTrafficking => 6.0,
            CrimeTypeEnum.Embezzlement => 5.0,
            CrimeTypeEnum.Extortion => 5.0,
            CrimeTypeEnum.Counterfeiting => 4.0,
            CrimeTypeEnum.Fraud => 4.0,
            CrimeTypeEnum.CyberCrime => 4.0,
            CrimeTypeEnum.Robbery => 5.0,
            CrimeTypeEnum.IllegalPossessionOfFirearms => 4.0,
            CrimeTypeEnum.Burglary => 3.0,
            CrimeTypeEnum.DomesticViolence => 4.0,
            CrimeTypeEnum.Assault => 3.0,
            CrimeTypeEnum.Theft => 2.0,
            CrimeTypeEnum.Vandalism => 1.0,
            _ => 2.0 // Default for unknown crime types
        };
    }

    private string GetPrecinctName(Barangay barangay)
    {
        return barangay.ToString().Replace("_", " ");
    }
}

// Supporting data models
public class PrecinctIncidentStatistics
{
    public Guid PrecinctId { get; set; }
    public string PrecinctName { get; set; } = string.Empty;
    public int TotalIncidents { get; set; }
    public double AreaKm2 { get; set; }
    public double IncidentDensity { get; set; } // Incidents per km²
    public int TotalManpower { get; set; }
    public double IncidentsPerOfficer { get; set; }
}

public class CrimeTypeStatistics
{
    public CrimeTypeEnum CrimeType { get; set; }
    public int TotalCount { get; set; }
    public double SeverityScore { get; set; }
}