using System.ComponentModel.DataAnnotations;
using espasyo.Application.Configuration;

namespace espasyo.Application.Services;

/// <summary>
/// Validates ML configuration settings to ensure they are within acceptable ranges
/// and that all required settings are properly configured
/// </summary>
public class MLConfigurationValidator
{
    /// <summary>
    /// Validates the ML settings configuration
    /// </summary>
    /// <param name="settings">The ML settings to validate</param>
    /// <returns>A list of validation errors, empty if all settings are valid</returns>
    public static List<string> ValidateConfiguration(MLSettings settings)
    {
        var errors = new List<string>();

        if (settings == null)
        {
            errors.Add("MLSettings configuration is required but was not found.");
            return errors;
        }

        // Validate Training settings
        ValidateTrainingSettings(settings.Training, errors);
        
        // Validate Historical Data settings
        ValidateHistoricalDataSettings(settings.HistoricalData, errors);
        
        // Validate Feature Engineering settings
        ValidateFeatureEngineeringSettings(settings.FeatureEngineering, errors);
        
        // Validate Complexity settings
        ValidateComplexitySettings(settings.Complexity, errors);
        
        // Validate Workload settings
        ValidateWorkloadSettings(settings.Workload, errors);
        
        // Validate Optimization settings
        ValidateOptimizationSettings(settings.Optimization, errors);
        
        // Validate Default settings
        ValidateDefaultSettings(settings.Defaults, errors);

        // Cross-validation checks
        ValidateCrossSettingConsistency(settings, errors);

        return errors;
    }

    private static void ValidateTrainingSettings(TrainingSettings training, List<string> errors)
    {
        if (training == null)
        {
            errors.Add("Training settings are required.");
            return;
        }

        ValidatePropertyRange(training, nameof(training.RandomSeed), errors);
        ValidatePropertyRange(training, nameof(training.MinimumTrainingDataPoints), errors);
        ValidatePropertyRange(training, nameof(training.ValidationSplitRatio), errors);
    }

    private static void ValidateHistoricalDataSettings(HistoricalDataSettings historicalData, List<string> errors)
    {
        if (historicalData == null)
        {
            errors.Add("Historical data settings are required.");
            return;
        }

        ValidatePropertyRange(historicalData, nameof(historicalData.HistoricalYears), errors);
        ValidatePropertyRange(historicalData, nameof(historicalData.MinimumIncidentsPerPeriod), errors);
    }

    private static void ValidateFeatureEngineeringSettings(FeatureEngineeringSettings featureEngineering, List<string> errors)
    {
        if (featureEngineering == null)
        {
            errors.Add("Feature engineering settings are required.");
            return;
        }

        ValidatePropertyRange(featureEngineering, nameof(featureEngineering.PopulationDensityScaling), errors);
        ValidatePropertyRange(featureEngineering, nameof(featureEngineering.SeasonalVariationAmplitude), errors);
        ValidatePropertyRange(featureEngineering, nameof(featureEngineering.StandardMonthlyHours), errors);
    }

    private static void ValidateComplexitySettings(ComplexitySettings complexity, List<string> errors)
    {
        if (complexity == null)
        {
            errors.Add("Complexity settings are required.");
            return;
        }

        // Note: ComplexCrimeTypes and GeographicComplexityFactors are now calculated dynamically
        // by DataDrivenComplexityService, so we only validate they are initialized (can be empty)
        if (complexity.ComplexCrimeTypes == null)
        {
            errors.Add("ComplexCrimeTypes must be initialized (can be empty for data-driven calculation).");
        }

        if (complexity.GeographicComplexityFactors == null)
        {
            errors.Add("GeographicComplexityFactors must be initialized (can be empty for data-driven calculation).");
        }
        else
        {
            // Only validate manually configured factors (if any)
            foreach (var kvp in complexity.GeographicComplexityFactors)
            {
                if (kvp.Value < 0.1f || kvp.Value > 5.0f)
                {
                    errors.Add($"Manual GeographicComplexityFactor for '{kvp.Key}' must be between 0.1 and 5.0 (found: {kvp.Value}).");
                }
            }
        }

        ValidatePropertyRange(complexity, nameof(complexity.DefaultGeographicComplexity), errors);
        ValidatePropertyRange(complexity, nameof(complexity.MaxSeasonalComplexityVariation), errors);
    }

    private static void ValidateWorkloadSettings(WorkloadSettings workload, List<string> errors)
    {
        if (workload == null)
        {
            errors.Add("Workload settings are required.");
            return;
        }

        ValidatePropertyRange(workload, nameof(workload.BaseHoursPerCrime), errors);
        ValidatePropertyRange(workload, nameof(workload.ComplexityWorkloadMultiplier), errors);
        ValidatePropertyRange(workload, nameof(workload.MaxCrimesPerOfficerPerMonth), errors);
    }

    private static void ValidateOptimizationSettings(OptimizationSettings optimization, List<string> errors)
    {
        if (optimization == null)
        {
            errors.Add("Optimization settings are required.");
            return;
        }

        ValidatePropertyRange(optimization, nameof(optimization.MinimumStaffingLevel), errors);
        ValidatePropertyRange(optimization, nameof(optimization.MaximumStaffingLevel), errors);
        ValidatePropertyRange(optimization, nameof(optimization.StaffingOptimizationStep), errors);
        ValidatePropertyRange(optimization, nameof(optimization.TargetPerformanceScore), errors);

        if (optimization.MinimumStaffingLevel >= optimization.MaximumStaffingLevel)
        {
            errors.Add("MinimumStaffingLevel must be less than MaximumStaffingLevel.");
        }

        ValidatePerformanceSettings(optimization.Performance, errors);
    }

    private static void ValidatePerformanceSettings(PerformanceCalculationSettings performance, List<string> errors)
    {
        if (performance == null)
        {
            errors.Add("Performance calculation settings are required.");
            return;
        }

        ValidatePropertyRange(performance, nameof(performance.ClearanceRateWeight), errors);
        ValidatePropertyRange(performance, nameof(performance.ResponseTimeWeight), errors);
        ValidatePropertyRange(performance, nameof(performance.CommunitySatisfactionWeight), errors);
        ValidatePropertyRange(performance, nameof(performance.TargetResponseTimeMinutes), errors);

        var totalWeight = performance.ClearanceRateWeight + performance.ResponseTimeWeight + performance.CommunitySatisfactionWeight;
        if (Math.Abs(totalWeight - 1.0f) > 0.01f)
        {
            errors.Add($"Performance weights must sum to 1.0 (current sum: {totalWeight:F3}).");
        }
    }

    private static void ValidateDefaultSettings(DefaultPredictionSettings defaults, List<string> errors)
    {
        if (defaults == null)
        {
            errors.Add("Default prediction settings are required.");
            return;
        }

        ValidatePropertyRange(defaults, nameof(defaults.DefaultComplexityScore), errors);
        ValidatePropertyRange(defaults, nameof(defaults.DefaultWorkloadHours), errors);
        ValidatePropertyRange(defaults, nameof(defaults.DefaultConfidence), errors);

        ValidateFallbackSettings(defaults.Fallback, errors);
    }

    private static void ValidateFallbackSettings(FallbackCalculationSettings fallback, List<string> errors)
    {
        if (fallback == null)
        {
            errors.Add("Fallback calculation settings are required.");
            return;
        }

        ValidatePropertyRange(fallback, nameof(fallback.OfficersPerThousandPopulation), errors);
        ValidatePropertyRange(fallback, nameof(fallback.BaseOfficersPerPrecinct), errors);
        ValidatePropertyRange(fallback, nameof(fallback.AdditionalOfficersPer100Crimes), errors);
    }

    private static void ValidateCrossSettingConsistency(MLSettings settings, List<string> errors)
    {
        // Ensure historical years makes sense with training requirements
        var estimatedDataPointsPerYear = 12 * 7; // 12 months * 7 precincts (approximate)
        var estimatedTotalDataPoints = settings.HistoricalData.HistoricalYears * estimatedDataPointsPerYear;
        
        if (estimatedTotalDataPoints < settings.Training.MinimumTrainingDataPoints)
        {
            errors.Add($"Historical data period ({settings.HistoricalData.HistoricalYears} years) may not provide enough data points for training requirements ({settings.Training.MinimumTrainingDataPoints} minimum).");
        }

        // Ensure workload calculations are reasonable
        var maxHoursPerMonth = settings.FeatureEngineering.StandardMonthlyHours;
        var maxHoursPerCrime = settings.Workload.BaseHoursPerCrime * settings.Workload.ComplexityWorkloadMultiplier;
        var maxCrimesPerOfficer = maxHoursPerMonth / maxHoursPerCrime;
        
        if (maxCrimesPerOfficer < 1)
        {
            errors.Add("Workload configuration may result in unrealistic crime capacity per officer (less than 1 crime per officer per month).");
        }
    }

    private static void ValidatePropertyRange(object obj, string propertyName, List<string> errors)
    {
        var property = obj.GetType().GetProperty(propertyName);
        if (property == null) return;

        var rangeAttribute = property.GetCustomAttributes(typeof(RangeAttribute), false).FirstOrDefault() as RangeAttribute;
        if (rangeAttribute == null) return;

        var value = property.GetValue(obj);
        if (value == null) return;

        var isValid = rangeAttribute.IsValid(value);
        if (!isValid)
        {
            errors.Add($"{propertyName} must be between {rangeAttribute.Minimum} and {rangeAttribute.Maximum} (found: {value}).");
        }
    }
}