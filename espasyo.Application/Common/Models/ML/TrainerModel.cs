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
    public int Year { get;  set; }
    public string TimeOfDay { get; set; }
    public Barangay Precinct { get; set; }
    public CrimeTypeEnum CrimeType { get; set; }
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

public record GroupedClusterResponse
{
   public IEnumerable<ClusterGroup> ClusterGroups { get; init; } = [];
   public IEnumerable<string> Filters { get; init; } = [];
}

// Statistical Forecasting Models
public record ForecastParameters
{
    public int Horizon { get; init; } = 6; // months ahead
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ModelType { get; init; } = "SSA"; // SSA, Linear, Seasonal
    public bool IncludeSeasonality { get; init; } = true;
    public bool WeightRecentData { get; init; } = true;
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
    public List<ForecastPoint> Forecasts { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record ForecastResponse
{
    public List<ForecastSeries> Series { get; init; } = new();
    public ForecastMetrics Metrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public string ModelUsed { get; init; } = string.Empty;
    public ForecastSummary Summary { get; init; } = new();
    public ForecastExplanation Explanation { get; init; } = new();
    public ThresholdCalculationResult DynamicThresholds { get; init; } = new();
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

// Request models for API endpoints
public record StatisticalForecastRequest
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = new List<ClusterGroup>();
    public int Horizon { get; init; } = 6;
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ModelType { get; init; } = "SSA";
    public bool IncludeSeasonality { get; init; } = true;
    public bool WeightRecentData { get; init; } = true;
    
    // Support for existing UI filters
    public int[]? PrecinctFilter { get; init; }
    public int[]? CrimeTypeFilter { get; init; }
    public string[]? RiskLevelFilter { get; init; }
    public DateTime? DateRangeStart { get; init; }
    public DateTime? DateRangeEnd { get; init; }
}
