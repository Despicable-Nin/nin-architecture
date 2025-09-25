using System.ComponentModel.DataAnnotations;

namespace espasyo.Application.Configuration;

/// <summary>
/// Configuration settings for ML-based manpower allocation service
/// Contains all configurable parameters to eliminate hard-coded values
/// </summary>
public class MLSettings
{
    /// <summary>
    /// Training parameters for ML models
    /// </summary>
    public TrainingSettings Training { get; set; } = new();

    /// <summary>
    /// Historical data configuration
    /// </summary>
    public HistoricalDataSettings HistoricalData { get; set; } = new();

    /// <summary>
    /// Feature engineering settings
    /// </summary>
    public FeatureEngineeringSettings FeatureEngineering { get; set; } = new();

    /// <summary>
    /// Complexity analysis configuration
    /// </summary>
    public ComplexitySettings Complexity { get; set; } = new();

    /// <summary>
    /// Workload prediction settings
    /// </summary>
    public WorkloadSettings Workload { get; set; } = new();

    /// <summary>
    /// Optimization parameters
    /// </summary>
    public OptimizationSettings Optimization { get; set; } = new();

    /// <summary>
    /// Default prediction values when models are not trained
    /// </summary>
    public DefaultPredictionSettings Defaults { get; set; } = new();
}

public class TrainingSettings
{
    /// <summary>
    /// Random seed for reproducible ML training results
    /// </summary>
    [Range(0, int.MaxValue)]
    public int RandomSeed { get; set; } = 42;

    /// <summary>
    /// Minimum number of historical data points required for training
    /// </summary>
    [Range(10, 10000)]
    public int MinimumTrainingDataPoints { get; set; } = 100;

    /// <summary>
    /// Validation split ratio for model evaluation
    /// </summary>
    [Range(0.1, 0.5)]
    public double ValidationSplitRatio { get; set; } = 0.2;
}

public class HistoricalDataSettings
{
    /// <summary>
    /// Number of years of historical data to use for training
    /// </summary>
    [Range(1, 10)]
    public int HistoricalYears { get; set; } = 3;

    /// <summary>
    /// Whether to include current year data in training
    /// </summary>
    public bool IncludeCurrentYear { get; set; } = true;

    /// <summary>
    /// Minimum incidents per precinct-month for reliable training
    /// </summary>
    [Range(1, 100)]
    public int MinimumIncidentsPerPeriod { get; set; } = 5;
}

public class FeatureEngineeringSettings
{
    /// <summary>
    /// Whether to apply normalization to numerical features
    /// </summary>
    public bool ApplyNormalization { get; set; } = true;

    /// <summary>
    /// Population density scaling factor
    /// </summary>
    [Range(1, 10000)]
    public float PopulationDensityScaling { get; set; } = 1000.0f;

    /// <summary>
    /// Seasonal variation amplitude for monthly factors
    /// </summary>
    [Range(0.0, 1.0)]
    public float SeasonalVariationAmplitude { get; set; } = 0.1f;

    /// <summary>
    /// Hours per officer per month for workload calculations
    /// </summary>
    [Range(80, 200)]
    public float StandardMonthlyHours { get; set; } = 160.0f;
}

public class ComplexitySettings
{
    /// <summary>
    /// Crime types considered highly complex - calculated dynamically from data
    /// Use DataDrivenComplexityService.CalculateComplexCrimeTypesAsync() instead
    /// </summary>
    [Obsolete("Use DataDrivenComplexityService.CalculateComplexCrimeTypesAsync() for data-driven complexity calculation")]
    public HashSet<string> ComplexCrimeTypes { get; set; } = new();

    /// <summary>
    /// Geographic complexity factors by precinct - calculated dynamically from data
    /// Use DataDrivenComplexityService.CalculateGeographicComplexityFactorsAsync() instead
    /// </summary>
    [Obsolete("Use DataDrivenComplexityService.CalculateGeographicComplexityFactorsAsync() for data-driven complexity calculation")]
    public Dictionary<string, float> GeographicComplexityFactors { get; set; } = new();

    /// <summary>
    /// Default geographic complexity factor for precincts not specified
    /// </summary>
    [Range(0.1, 5.0)]
    public float DefaultGeographicComplexity { get; set; } = 1.0f;

    /// <summary>
    /// Maximum seasonal complexity variation
    /// </summary>
    [Range(0.0, 2.0)]
    public float MaxSeasonalComplexityVariation { get; set; } = 1.0f;
}

public class WorkloadSettings
{
    /// <summary>
    /// Base hours per crime for workload estimation
    /// </summary>
    [Range(1, 40)]
    public float BaseHoursPerCrime { get; set; } = 8.0f;

    /// <summary>
    /// Complexity multiplier for workload calculations
    /// </summary>
    [Range(1.0, 10.0)]
    public float ComplexityWorkloadMultiplier { get; set; } = 1.5f;

    /// <summary>
    /// Maximum crimes per officer capacity per month
    /// </summary>
    [Range(5, 100)]
    public int MaxCrimesPerOfficerPerMonth { get; set; } = 50;
}

public class OptimizationSettings
{
    /// <summary>
    /// Minimum staffing level to consider in optimization
    /// </summary>
    [Range(1, 10)]
    public int MinimumStaffingLevel { get; set; } = 2;

    /// <summary>
    /// Maximum staffing level to consider in optimization
    /// </summary>
    [Range(10, 200)]
    public int MaximumStaffingLevel { get; set; } = 50;

    /// <summary>
    /// Step size for staffing level optimization search
    /// </summary>
    [Range(1, 10)]
    public int StaffingOptimizationStep { get; set; } = 1;

    /// <summary>
    /// Target performance score threshold
    /// </summary>
    [Range(0.0, 1.0)]
    public float TargetPerformanceScore { get; set; } = 0.8f;

    /// <summary>
    /// Performance calculation parameters
    /// </summary>
    public PerformanceCalculationSettings Performance { get; set; } = new();
}

public class PerformanceCalculationSettings
{
    /// <summary>
    /// Weight for crime clearance rate in performance calculation
    /// </summary>
    [Range(0.0, 1.0)]
    public float ClearanceRateWeight { get; set; } = 0.4f;

    /// <summary>
    /// Weight for response time in performance calculation
    /// </summary>
    [Range(0.0, 1.0)]
    public float ResponseTimeWeight { get; set; } = 0.3f;

    /// <summary>
    /// Weight for community satisfaction in performance calculation
    /// </summary>
    [Range(0.0, 1.0)]
    public float CommunitySatisfactionWeight { get; set; } = 0.3f;

    /// <summary>
    /// Target response time in minutes
    /// </summary>
    [Range(1, 60)]
    public float TargetResponseTimeMinutes { get; set; } = 15.0f;
}

public class DefaultPredictionSettings
{
    /// <summary>
    /// Default complexity score when model is not available
    /// </summary>
    [Range(0.1, 5.0)]
    public float DefaultComplexityScore { get; set; } = 1.0f;

    /// <summary>
    /// Default workload hours when model is not available
    /// </summary>
    [Range(40, 300)]
    public float DefaultWorkloadHours { get; set; } = 160.0f;

    /// <summary>
    /// Default confidence level for fallback predictions
    /// </summary>
    [Range(0.0, 1.0)]
    public float DefaultConfidence { get; set; } = 0.5f;

    /// <summary>
    /// Fallback manpower calculation method parameters
    /// </summary>
    public FallbackCalculationSettings Fallback { get; set; } = new();
}

public class FallbackCalculationSettings
{
    /// <summary>
    /// Officers per 1000 population ratio for fallback calculation
    /// </summary>
    [Range(0.5, 10.0)]
    public float OfficersPerThousandPopulation { get; set; } = 2.0f;

    /// <summary>
    /// Base officers per precinct minimum
    /// </summary>
    [Range(2, 20)]
    public int BaseOfficersPerPrecinct { get; set; } = 4;

    /// <summary>
    /// Additional officers per 100 crimes per month
    /// </summary>
    [Range(0.1, 5.0)]
    public float AdditionalOfficersPer100Crimes { get; set; } = 1.0f;
}