using espasyo.Domain.Enums;
using Microsoft.ML.Data;

namespace espasyo.Application.Common.Models.ML;

public record TrainerModel
{
   public string? CaseId { get; init; }
   public int CrimeType { get; init; }
   public string? TimeStamp { get; init; }
   public long TimeStampUnix { get; init; }
   public string? Address { get; init; }
   public double Latitude { get; init; }
   public double Longitude { get; init; }
   public int Severity { get; init; } 
   public int PoliceDistrict { get; init; } 
   public int Weather { get; init; } 
   public int Motive { get; init; }
}

public record ClusteredModel
{
   public string CaseId { get; set; }

   // This maps the ML.NET output column "PredictedLabel" to this property.
   [ColumnName("PredictedLabel")]
   public uint ClusterId { get; set; }

   public double Latitude { get; set; }
   public double Longitude { get; set; }
}

public record ClusterItem
{
   public string CaseId { get; set; }
   public double Latitude { get; set; }
   public double Longitude { get; set; }
    public int Month { get;  set; }
    public int Day { get;  set; }
    public int Year { get;  set; }
    public string TimeOfDay { get; set; }
    public Barangay Precinct { get; set; }
    public CrimeTypeEnum CrimeType { get; set; }
    public uint ClusterId { get; set; }
}

public record ClusterGroup
{
    public uint ClusterId { get; set; }
    public float[] Centroids => ClusterItems.Count != 0
       ?
         [
             (float)ClusterItems.Average(item => item.Latitude),
             (float)ClusterItems.Average(item => item.Longitude)
         ]
       : [0f, 0f];
    public List<ClusterItem> ClusterItems { get; set; } = [];
    public int ClusterCount => ClusterItems.Count;
}

public record ClusterQualityMetrics
{
    public int OptimalK { get; init; }
    public int SelectedK { get; init; }
    public Dictionary<int, double> SilhouetteScores { get; init; } = [];
    public Dictionary<int, double> DaviesBouldinScores { get; init; } = [];
    public Dictionary<int, double> CalinskiHarabaszScores { get; init; } = [];
}

public record GroupedClusterResponse
{
   public IEnumerable<ClusterGroup> ClusterGroups { get; init; } = [];
   public IEnumerable<string> Filters { get; init; } = [];
   public ClusterQualityMetrics? QualityMetrics { get; init; }
}

// Statistical Forecasting Models
public record ForecastParameters
{
    public int Horizon { get; init; } = 6; // months ahead
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ModelType { get; init; } = "Linear"; // SSA, Linear, Seasonal
    public bool IncludeSeasonality { get; init; } = true;
    public bool WeightRecentData { get; init; } = true;
    public bool IncludeTimeOfDay { get; init; } = false;
    public bool IncludeMonthOfYear { get; init; } = false;
    public bool IncludeTrend { get; init; } = true;
    public string[]? CrimeTypeFilter { get; init; }
    public string[]? SeverityFilter { get; init; }
    public DynamicThresholds? CustomThresholds { get; init; }
    /// <summary>
    /// Optional configuration for CompositeRiskScore computation.
    /// When null, all built-in defaults are used.
    /// </summary>
    public RiskScoringConfig? RiskScoringConfig { get; init; }
}

public record ForecastPoint
{
    public DateTime Timestamp { get; init; }
    public double Forecast { get; init; }
    public double LowerBound { get; init; }
    public double UpperBound { get; init; }
    public double Confidence { get; init; }
    public string Trend { get; init; } = "stable"; // increasing, decreasing, stable
    public string RiskLevel { get; init; } = "medium"; // low, medium, high, critical
}

public record ForecastSeries
{
    public int Precinct { get; init; }
    public int CrimeType { get; init; }
    public uint ClusterId { get; init; }
    public List<ForecastPoint> Forecasts { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record ForecastRow
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string PredictionType { get; init; } = "temporal";
    public int Precinct { get; init; }
    public int? CrimeType { get; init; }
    public uint? ClusterId { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public DateTime Timestamp { get; init; }
    public double Forecast { get; init; }
    public double LowerBound { get; init; }
    public double UpperBound { get; init; }
    public double Confidence { get; init; }
    public string Trend { get; init; } = "stable";
    public string RiskLevel { get; init; } = "medium";
    public string? TimeOfDay { get; init; }
    /// <summary>
    /// Composite risk score derived from crime type severity × precinct geographic risk × heinous multiplier.
    /// Separate from the forecast-engine RiskLevel (which is based on volume-vs-historical-average ratios).
    /// Populated by GenerateStatisticalForecastCommandHandler after the temporal forecast is flattened.
    /// </summary>
    public double CompositeRiskScore { get; init; }
}

public record SeasonalPredictionRow
{
    public int Precinct { get; init; }
    public int CrimeType { get; init; }
    public List<double> Trend { get; init; } = new();
    public List<double> Seasonal { get; init; } = new();
    public List<double> Residual { get; init; } = new();
    public Dictionary<string, double> Strength { get; init; } = new();
    public int PeakMonth { get; init; }
    public int TroughMonth { get; init; }
}

public record TemporalPatterns
{
    public string? PeakTimeOfDay { get; init; }
    public int? PeakMonth { get; init; }
    public int? TroughMonth { get; init; }
    public double? WeekendEffect { get; init; }
}

public record SpatialForecastRow
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public int Precinct { get; init; }
    public uint ClusterId { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public DateTime Timestamp { get; init; }
    public double Forecast { get; init; }
    public double LowerBound { get; init; }
    public double UpperBound { get; init; }
    public double Confidence { get; init; }
    public string Trend { get; init; } = "stable";
    public string RiskLevel { get; init; } = "medium";
}

public record ForecastResponse
{
    public List<ForecastSeries> Series { get; init; } = new();
    public List<ForecastRow> Forecasts { get; init; } = new();
    public List<SpatialForecastRow> Spatial { get; init; } = new();
    public ForecastMetrics Metrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public string ModelUsed { get; init; } = string.Empty;
    public ForecastSummary Summary { get; init; } = new();
    public ForecastExplanation Explanation { get; init; } = new();
    public ThresholdCalculationResult DynamicThresholds { get; init; } = new();
    public TemporalPatterns? TemporalPatterns { get; init; }
    public List<SeasonalPredictionRow> SeasonalPredictions { get; init; } = new();
}

public record ForecastMetrics
{
    public double MeanAbsoluteError { get; init; }
    public double RootMeanSquareError { get; init; }
    public double MeanAbsolutePercentageError { get; init; }
    public double ModelAccuracy { get; init; }
}

public record ForecastValidationResult
{
    public ForecastMetrics Metrics { get; init; } = new();
    public bool IsReliable { get; init; }
    public List<string> Warnings { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}

public record DataQualityAssessment
{
    public bool IsValid { get; init; }
    public int DataPoints { get; init; }
    public int OutlierCount { get; init; }
    public double OutlierPercentage { get; init; }
    public List<string> Issues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}

// Time Series Data for ML.NET
public class TimeSeriesData
{
    [LoadColumn(0)]
    public DateTime Date { get; set; }
    
    [LoadColumn(1)]
    public float Value { get; set; }
}

public class ForecastOutput
{
    public float[] ForecastedValues { get; set; } = Array.Empty<float>();
    public float[] LowerBoundValues { get; set; } = Array.Empty<float>();
    public float[] UpperBoundValues { get; set; } = Array.Empty<float>();
}

public record ForecastSummary
{
    public int TotalForecasts { get; init; }
    public int HighRiskPredictions { get; init; }
    public int CriticalRiskPredictions { get; init; }
    public string OverallTrend { get; init; } = "stable"; // increasing, decreasing, stable
    public string DominantRiskLevel { get; init; } = "medium";
    public double AverageConfidence { get; init; }
    public string KeyInsight { get; init; } = string.Empty;
    public List<string> RecommendedActions { get; init; } = new();
    /// <summary>
    /// Average CompositeRiskScore across all forecast rows.
    /// </summary>
    public double AverageCompositeRiskScore { get; init; }
    /// <summary>
    /// Maximum CompositeRiskScore across all forecast rows.
    /// </summary>
    public double MaxCompositeRiskScore { get; init; }
}

public record ForecastExplanation
{
    public string ModelDescription { get; init; } = string.Empty;
    public string DataQualityNotes { get; init; } = string.Empty;
    public string ConfidenceExplanation { get; init; } = string.Empty;
    public string TrendAnalysis { get; init; } = string.Empty;
    public string RiskAssessmentLogic { get; init; } = string.Empty;
    public string LimitationsAndCaveats { get; init; } = string.Empty;
    public string HowToInterpret { get; init; } = string.Empty;
}

// Dynamic Risk Threshold Models
public record DynamicThresholds
{
    public double LowMax { get; init; } = 0.8;      // Low risk threshold (80% of average)
    public double MediumMax { get; init; } = 1.2;   // Medium risk threshold (120% of average)
    public double HighMax { get; init; } = 1.5;     // High risk threshold (150% of average)
    // Critical is anything above HighMax

    public double TrendIncreaseThreshold { get; init; } = 1.1;   // forecast > avg * this → "increasing"
    public double TrendDecreaseThreshold { get; init; } = 0.9;   // forecast < avg * this → "decreasing"
}

public record ThresholdCalculationResult
{
    public DynamicThresholds GlobalThresholds { get; init; } = new();
    public Dictionary<int, DynamicThresholds> PrecinctSpecificThresholds { get; init; } = new();
    public int TotalDataPointsUsed { get; init; }
    public Dictionary<int, int> DataPointsPerPrecinct { get; init; } = new();
    public string CalculationMethod { get; init; } = "percentile-based";
    public Dictionary<string, double> GlobalStatistics { get; init; } = new();
    public Dictionary<int, Dictionary<string, double>> PrecinctStatistics { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Configurable parameters for the CompositeRiskScore formula.
/// All fields have defaults for backward compatibility; the front-end may supply overrides.
/// Formula per (precinct, crimeType): score = (severityScore / 10.0) * crimeRiskFactor * heinousMultiplier
/// </summary>
public record RiskScoringConfig
{
    /// <summary>
    /// Multiplier applied when the crime type is heinous AND the filter/set includes heinous crime types.
    /// Default: 1.5 (50 % boost).
    /// </summary>
    public double HeinousBoostFactor { get; init; } = 1.5;

    /// <summary>
    /// Multiplier applied when the crime type is NOT heinous but the filter/set includes heinous crimes.
    /// Default: 1.2 (20 % boost — heinous presence elevates all crime types in the precinct).
    /// </summary>
    public double HeinousPresenceFactor { get; init; } = 1.2;

    /// <summary>
    /// Severity score per crime type ID (int value of CrimeTypeEnum, 0–19).
    /// When null, uses the built-in default mapping derived from DataDrivenComplexityService.
    /// </summary>
    public Dictionary<int, double>? CrimeTypeSeverityScores { get; init; }

    /// <summary>
    /// Geographic risk factor per precinct ID (int value of Barangay, 0–8).
    /// When null, uses the built-in default mapping from MuntinlupaBarangayData.
    /// </summary>
    public Dictionary<int, double>? PrecinctCrimeRiskFactors { get; init; }

    /// <summary>
    /// Crime type IDs considered "heinous" (int values of CrimeTypeEnum).
    /// When null, default is [7, 11, 12, 14, 15, 16] — DrugTrafficking, HumanTrafficking,
    /// Homicide, Kidnapping, Murder, Rape (the types mapped to SeverityEnum.High).
    /// </summary>
    public List<int>? HeinousCrimeTypeIds { get; init; }

    /// <summary>
    /// Returns the effective heinous crime type IDs (override or built-in default).
    /// </summary>
    public List<int> GetHeinousCrimeTypeIds() =>
        HeinousCrimeTypeIds ?? DefaultHeinousCrimeTypeIds;

    /// <summary>
    /// Returns the effective severity score for the given crime type ID (override or built-in).
    /// </summary>
    public double GetSeverityScore(int crimeTypeId) =>
        CrimeTypeSeverityScores?.TryGetValue(crimeTypeId, out var s) == true ? s : GetDefaultSeverityScore(crimeTypeId);

    /// <summary>
    /// Returns the effective geographic risk factor for the given precinct ID (override or built-in).
    /// </summary>
    public double GetPrecinctRiskFactor(int precinctId) =>
        PrecinctCrimeRiskFactors?.TryGetValue(precinctId, out var r) == true ? r : GetDefaultRiskFactor(precinctId);

    // ── Built-in defaults ──────────────────────────────────────────────────────

    private static readonly List<int> DefaultHeinousCrimeTypeIds =
        [7, 11, 12, 14, 15, 16]; // DrugTrafficking, HumanTrafficking, Homicide, Kidnapping, Murder, Rape

    private static double GetDefaultSeverityScore(int crimeTypeId) => crimeTypeId switch
    {
        0 => 6.0,   // Arson
        1 => 3.0,   // Assault
        2 => 3.0,   // Burglary
        3 => 7.0,   // Corruption
        4 => 4.0,   // Counterfeiting
        5 => 4.0,   // CyberCrime
        6 => 4.0,   // DomesticViolence
        7 => 6.0,   // DrugTrafficking
        8 => 5.0,   // Embezzlement
        9 => 5.0,   // Extortion
        10 => 4.0,  // Fraud
        11 => 9.0,  // HumanTrafficking
        12 => 9.0,  // Homicide
        13 => 4.0,  // IllegalPossessionOfFirearms
        14 => 8.0,  // Kidnapping
        15 => 10.0, // Murder
        16 => 8.0,  // Rape
        17 => 5.0,  // Robbery
        18 => 2.0,  // Theft
        19 => 1.0,  // Vandalism
        _ => 2.0
    };

    private static double GetDefaultRiskFactor(int precinctId) => precinctId switch
    {
        0 => 1.8,  // Alabang
        1 => 0.7,  // Bayanan
        2 => 0.6,  // Buli
        3 => 0.6,  // Cupang
        4 => 0.9,  // Poblacion
        5 => 0.7,  // Putatan
        6 => 0.8,  // Tunasan
        7 => 0.5,  // Ayala_Alabang
        8 => 1.2,  // Sucat
        _ => 1.0
    };
}

// Request models for API endpoints
public record StatisticalForecastRequest
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = new List<ClusterGroup>();
    public int Horizon { get; init; } = 6;
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ModelType { get; init; } = "Linear";
    public bool IncludeSeasonality { get; init; } = true;
    public bool WeightRecentData { get; init; } = true;
    public bool IncludeTimeOfDay { get; init; } = false;
    public bool IncludeMonthOfYear { get; init; } = false;
    public bool IncludeTrend { get; init; } = true;
    
    // Data filters
    public string[]? CrimeTypeFilter { get; init; }
    public string[]? SeverityFilter { get; init; }
    public DynamicThresholds? CustomThresholds { get; init; }
    /// <summary>
    /// Optional risk scoring overrides. When null, all built-in defaults apply.
    /// </summary>
    public RiskScoringConfig? RiskScoringConfig { get; init; }
}

public record TemporalForecastRequest
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = new List<ClusterGroup>();
    public int Horizon { get; init; } = 6;
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ModelType { get; init; } = "Linear";
    public bool IncludeSeasonality { get; init; } = true;
    public bool WeightRecentData { get; init; } = true;
    public bool IncludeTimeOfDay { get; init; } = false;
    public bool IncludeMonthOfYear { get; init; } = false;
    public bool IncludeTrend { get; init; } = true;
    public string[]? CrimeTypeFilter { get; init; }
    public string[]? SeverityFilter { get; init; }
    public DynamicThresholds? CustomThresholds { get; init; }
}

public record SpatialForecastRequest
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = new List<ClusterGroup>();
    public int Horizon { get; init; } = 6;
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ModelType { get; init; } = "Linear";
    public bool IncludeTrend { get; init; } = true;
}

public record SeasonalForecastRequest
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = new List<ClusterGroup>();
}
