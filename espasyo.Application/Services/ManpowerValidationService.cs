using espasyo.Domain.Enums;

namespace espasyo.Application.Services;

/// <summary>
/// Service to validate manpower recommendations against established standards and benchmarks
/// </summary>
public class ManpowerValidationService
{
    // International police-to-population ratios (officers per 1,000 residents)
    private static readonly Dictionary<string, double> InternationalBenchmarks = new()
    {
        { "Philippines_National_Average", 1.6 },    // PNP national average
        { "ASEAN_Average", 2.2 },                  // Southeast Asia average
        { "UN_Recommended_Minimum", 2.25 },        // UN recommended minimum
        { "Developed_Countries_Average", 3.5 },     // Developed countries average
        { "High_Crime_Areas_Target", 4.0 }         // High crime areas target
    };

    // Workload capacity standards (cases per officer per month by case complexity)
    private static readonly Dictionary<string, double> WorkloadCapacityStandards = new()
    {
        { "Simple_Cases_Only", 25.0 },           // Traffic, minor theft, etc.
        { "Mixed_Complexity", 15.0 },            // Average mix of case types
        { "Complex_Cases_Focus", 8.0 },          // Major crimes, investigations
        { "Specialized_Units", 5.0 }             // Cyber crime, organized crime
    };

    /// <summary>
    /// Validate manpower recommendation against multiple benchmarks
    /// </summary>
    public ValidationResult ValidateRecommendation(
        Barangay precinct, 
        int recommendedManpower, 
        int population, 
        Dictionary<CrimeTypeEnum, int> predictedCrimes)
    {
        var validation = new ValidationResult
        {
            Precinct = precinct,
            RecommendedManpower = recommendedManpower,
            Population = population,
            PredictedCrimes = predictedCrimes
        };

        // Test 1: Police-to-population ratio
        validation.PopulationRatioValidation = ValidatePopulationRatio(recommendedManpower, population);

        // Test 2: Workload capacity analysis  
        validation.WorkloadCapacityValidation = ValidateWorkloadCapacity(recommendedManpower, predictedCrimes);

        // Test 3: International benchmarking
        validation.InternationalBenchmarkValidation = ValidateAgainstInternationalStandards(recommendedManpower, population);

        // Test 4: Operational feasibility
        validation.OperationalFeasibilityValidation = ValidateOperationalFeasibility(recommendedManpower);

        // Calculate overall validation score
        validation.OverallValidationScore = CalculateOverallValidationScore(validation);
        
        // Generate recommendations for improvement
        validation.ImprovementRecommendations = GenerateImprovementRecommendations(validation);

        return validation;
    }

    private PopulationRatioValidation ValidatePopulationRatio(int recommendedManpower, int population)
    {
        var officersPerThousand = (recommendedManpower / (double)population) * 1000;
        
        var result = new PopulationRatioValidation
        {
            OfficersPerThousandPopulation = officersPerThousand,
            PhilippinesNationalAverage = InternationalBenchmarks["Philippines_National_Average"],
            ASEANAverage = InternationalBenchmarks["ASEAN_Average"],
            UNRecommendedMinimum = InternationalBenchmarks["UN_Recommended_Minimum"]
        };

        // Determine compliance level
        if (officersPerThousand >= result.UNRecommendedMinimum)
            result.ComplianceLevel = "Exceeds international standards";
        else if (officersPerThousand >= result.ASEANAverage)
            result.ComplianceLevel = "Meets regional standards";
        else if (officersPerThousand >= result.PhilippinesNationalAverage)
            result.ComplianceLevel = "Meets national standards";
        else
            result.ComplianceLevel = "Below national standards";

        result.IsValid = officersPerThousand >= result.PhilippinesNationalAverage * 0.8; // 80% of national average

        return result;
    }

    private WorkloadCapacityValidation ValidateWorkloadCapacity(int recommendedManpower, Dictionary<CrimeTypeEnum, int> predictedCrimes)
    {
        var totalCases = predictedCrimes.Values.Sum();
        var casesPerOfficer = recommendedManpower > 0 ? (double)totalCases / recommendedManpower : 0;

        // Calculate complexity-weighted caseload
        var complexityWeight = CalculateCaseComplexityWeight(predictedCrimes);
        var adjustedCasesPerOfficer = casesPerOfficer * complexityWeight;

        var result = new WorkloadCapacityValidation
        {
            CasesPerOfficerPerMonth = casesPerOfficer,
            ComplexityAdjustedCasesPerOfficer = adjustedCasesPerOfficer,
            StandardCapacityMixed = WorkloadCapacityStandards["Mixed_Complexity"],
            StandardCapacitySimple = WorkloadCapacityStandards["Simple_Cases_Only"],
            StandardCapacityComplex = WorkloadCapacityStandards["Complex_Cases_Focus"]
        };

        // Determine workload level
        if (adjustedCasesPerOfficer <= result.StandardCapacityComplex)
            result.WorkloadLevel = "Light workload - excellent service quality";
        else if (adjustedCasesPerOfficer <= result.StandardCapacityMixed)
            result.WorkloadLevel = "Optimal workload - good service quality";
        else if (adjustedCasesPerOfficer <= result.StandardCapacitySimple)
            result.WorkloadLevel = "High workload - adequate service quality";
        else
            result.WorkloadLevel = "Excessive workload - compromised service quality";

        result.IsValid = adjustedCasesPerOfficer <= result.StandardCapacitySimple * 1.2; // 120% of simple capacity max

        return result;
    }

    private InternationalBenchmarkValidation ValidateAgainstInternationalStandards(int recommendedManpower, int population)
    {
        var officersPerThousand = (recommendedManpower / (double)population) * 1000;
        
        var result = new InternationalBenchmarkValidation();
        
        foreach (var benchmark in InternationalBenchmarks)
        {
            var comparison = new BenchmarkComparison
            {
                BenchmarkName = benchmark.Key,
                BenchmarkValue = benchmark.Value,
                ActualValue = officersPerThousand,
                CompliancePercentage = (officersPerThousand / benchmark.Value) * 100,
                MeetsStandard = officersPerThousand >= benchmark.Value * 0.9 // 90% compliance threshold
            };
            
            result.BenchmarkComparisons.Add(comparison);
        }

        result.OverallCompliance = result.BenchmarkComparisons.Average(c => c.CompliancePercentage);
        result.IsValid = result.BenchmarkComparisons.Any(c => c.MeetsStandard);

        return result;
    }

    private OperationalFeasibilityValidation ValidateOperationalFeasibility(int recommendedManpower)
    {
        var result = new OperationalFeasibilityValidation
        {
            RecommendedManpower = recommendedManpower
        };

        // Check minimum staffing requirements
        result.MeetsMinimumStaffing = recommendedManpower >= 2;

        // Check maximum reasonable staffing (based on budget and practicality)
        result.WithinMaximumLimit = recommendedManpower <= 100; // Reasonable maximum per precinct

        // Check for reasonable staffing patterns (even numbers for shift coverage)
        result.SupportsShiftCoverage = recommendedManpower % 2 == 0 || recommendedManpower == 1;

        // Calculate feasibility factors
        var feasibilityFactors = new List<bool> 
        { 
            result.MeetsMinimumStaffing, 
            result.WithinMaximumLimit, 
            result.SupportsShiftCoverage 
        };
        
        result.FeasibilityScore = feasibilityFactors.Count(f => f) / (double)feasibilityFactors.Count;
        result.IsValid = result.FeasibilityScore >= 0.67; // At least 2 out of 3 criteria

        if (!result.MeetsMinimumStaffing)
            result.FeasibilityIssues.Add("Below minimum staffing requirements");
        if (!result.WithinMaximumLimit)
            result.FeasibilityIssues.Add("Exceeds reasonable maximum staffing");
        if (!result.SupportsShiftCoverage)
            result.FeasibilityIssues.Add("Odd number may complicate shift scheduling");

        return result;
    }

    private double CalculateCaseComplexityWeight(Dictionary<CrimeTypeEnum, int> predictedCrimes)
    {
        // Treat all crimes equally - no arbitrary complexity weighting
        // This avoids hard-coded values and provides fair resource allocation
        return 1.0;
    }

    private double CalculateOverallValidationScore(ValidationResult validation)
    {
        var scores = new[]
        {
            validation.PopulationRatioValidation.IsValid ? 1.0 : 0.0,
            validation.WorkloadCapacityValidation.IsValid ? 1.0 : 0.0,
            validation.InternationalBenchmarkValidation.IsValid ? 1.0 : 0.0,
            validation.OperationalFeasibilityValidation.IsValid ? 1.0 : 0.0
        };

        return scores.Average();
    }

    private List<string> GenerateImprovementRecommendations(ValidationResult validation)
    {
        var recommendations = new List<string>();

        if (!validation.PopulationRatioValidation.IsValid)
        {
            var targetRatio = validation.PopulationRatioValidation.PhilippinesNationalAverage;
            var targetManpower = (int)Math.Ceiling(validation.Population * targetRatio / 1000);
            recommendations.Add($"Increase to {targetManpower} officers to meet national police-to-population ratio standards");
        }

        if (!validation.WorkloadCapacityValidation.IsValid)
        {
            var excessCases = validation.WorkloadCapacityValidation.ComplexityAdjustedCasesPerOfficer - 
                             validation.WorkloadCapacityValidation.StandardCapacityMixed;
            recommendations.Add($"Reduce workload by {excessCases:F1} cases per officer through additional staffing or process improvements");
        }

        if (!validation.OperationalFeasibilityValidation.IsValid)
        {
            recommendations.AddRange(validation.OperationalFeasibilityValidation.FeasibilityIssues);
        }

        return recommendations;
    }
}

// Supporting classes for validation results
public class ValidationResult
{
    public Barangay Precinct { get; set; }
    public int RecommendedManpower { get; set; }
    public int Population { get; set; }
    public Dictionary<CrimeTypeEnum, int> PredictedCrimes { get; set; } = new();
    public PopulationRatioValidation PopulationRatioValidation { get; set; } = new();
    public WorkloadCapacityValidation WorkloadCapacityValidation { get; set; } = new();
    public InternationalBenchmarkValidation InternationalBenchmarkValidation { get; set; } = new();
    public OperationalFeasibilityValidation OperationalFeasibilityValidation { get; set; } = new();
    public double OverallValidationScore { get; set; }
    public List<string> ImprovementRecommendations { get; set; } = new();
}

public class PopulationRatioValidation
{
    public double OfficersPerThousandPopulation { get; set; }
    public double PhilippinesNationalAverage { get; set; }
    public double ASEANAverage { get; set; }
    public double UNRecommendedMinimum { get; set; }
    public string ComplianceLevel { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}

public class WorkloadCapacityValidation
{
    public double CasesPerOfficerPerMonth { get; set; }
    public double ComplexityAdjustedCasesPerOfficer { get; set; }
    public double StandardCapacityMixed { get; set; }
    public double StandardCapacitySimple { get; set; }
    public double StandardCapacityComplex { get; set; }
    public string WorkloadLevel { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}

public class InternationalBenchmarkValidation
{
    public List<BenchmarkComparison> BenchmarkComparisons { get; set; } = new();
    public double OverallCompliance { get; set; }
    public bool IsValid { get; set; }
}

public class BenchmarkComparison
{
    public string BenchmarkName { get; set; } = string.Empty;
    public double BenchmarkValue { get; set; }
    public double ActualValue { get; set; }
    public double CompliancePercentage { get; set; }
    public bool MeetsStandard { get; set; }
}

public class OperationalFeasibilityValidation
{
    public int RecommendedManpower { get; set; }
    public bool MeetsMinimumStaffing { get; set; }
    public bool WithinMaximumLimit { get; set; }
    public bool SupportsShiftCoverage { get; set; }
    public double FeasibilityScore { get; set; }
    public List<string> FeasibilityIssues { get; set; } = new();
    public bool IsValid { get; set; }
}