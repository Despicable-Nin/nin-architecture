using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.Manpower.Queries.AnalyzeManpowerNeeds;

public class AnalyzeManpowerNeedsQueryHandler : IRequestHandler<AnalyzeManpowerNeedsQuery, ManpowerAnalysisResponse>
{
    private readonly IManpowerRepository _manpowerRepository;

    public AnalyzeManpowerNeedsQueryHandler(IManpowerRepository manpowerRepository)
    {
        _manpowerRepository = manpowerRepository;
    }

    public async Task<ManpowerAnalysisResponse> Handle(AnalyzeManpowerNeedsQuery request, CancellationToken cancellationToken)
    {
        var manpowerAllocations = await _manpowerRepository.GetAllManpowerAsync();
        var manpowerDict = manpowerAllocations.ToDictionary(m => m.PrecinctId, m => m);
        
        var analyses = new List<PrecinctAnalysis>();
        var summary = new ManpowerSummary();

        // For simplified analysis, assume standard thresholds
        const int mildThreshold = 15;
        const int moderateThreshold = 30;
        const int criticalThreshold = 45;

        // Iterate through ALL precincts with manpower data, not just those in request
        foreach (var manpower in manpowerAllocations)
        {
            // Get predicted cases for this precinct (default to 0 if not provided)
            var predictedCases = request.PredictedCaseCounts.TryGetValue(manpower.PrecinctId, out var cases) 
                ? cases 
                : 0; // Default to 0 cases if no prediction provided

            var severityLevel = GetSeverityLevel(predictedCases, mildThreshold, moderateThreshold, criticalThreshold);
            var recommendedCount = CalculateRecommendedManpower(predictedCases, severityLevel);
            var adjustment = recommendedCount - manpower.HeadCount;
            var requiresAdjustment = adjustment != 0;

            var justification = GenerateJustification(
                severityLevel, 
                predictedCases, 
                manpower.HeadCount, 
                adjustment
            );

            analyses.Add(new PrecinctAnalysis
            {
                PrecinctId = manpower.PrecinctId,
                PrecinctName = manpower.Precinct?.Name ?? GetPrecinctNameFromBarangay(manpower.Precinct?.Barangay),
                CurrentAllocation = manpower.HeadCount,
                PredictedCases = predictedCases,
                SeverityLevel = severityLevel,
                RequiresAdjustment = requiresAdjustment,
                RecommendedAdjustment = adjustment,
                RecommendedAllocation = recommendedCount,
                Justification = justification
            });

            // Update summary
            summary.TotalCurrentManpower += manpower.HeadCount;
            summary.TotalRecommendedManpower += recommendedCount;
            
            if (adjustment > 0)
                summary.PrecinctRequiringIncrease++;
            else if (adjustment < 0)
                summary.PrecinctRequiringDecrease++;
            else
                summary.PrecinctWithoutChange++;
        }

        summary.NetAdjustment = summary.TotalRecommendedManpower - summary.TotalCurrentManpower;

        return new ManpowerAnalysisResponse
        {
            Year = request.Year,
            PrecinctAnalyses = analyses,
            Summary = summary
        };
    }

    private static string GetSeverityLevel(int predictedCases, int mildThreshold, int moderateThreshold, int criticalThreshold)
    {
        if (predictedCases <= mildThreshold)
            return "Mild";
        if (predictedCases <= moderateThreshold)
            return "Moderate";
        if (predictedCases <= criticalThreshold)
            return "High";
        return "Critical";
    }
    
    private static int CalculateRecommendedManpower(int predictedCases, string severityLevel)
    {
        // Simple calculation based on severity level
        return severityLevel switch
        {
            "Mild" => Math.Max(10, (int)Math.Ceiling(predictedCases / 2.0)),
            "Moderate" => Math.Max(15, (int)Math.Ceiling(predictedCases / 1.5)),
            "High" => Math.Max(20, (int)Math.Ceiling(predictedCases / 1.2)),
            "Critical" => Math.Max(25, (int)Math.Ceiling(predictedCases / 1.0)),
            _ => 15
        };
    }

    private static string GenerateJustification(
        string severityLevel, 
        int predictedCases, 
        int currentAllocation, 
        int adjustment)
    {
        if (adjustment == 0)
        {
            return $"Current allocation of {currentAllocation} is adequate for {severityLevel.ToLower()} severity level ({predictedCases} predicted cases).";
        }

        if (adjustment > 0)
        {
            return $"Increase by {adjustment} officers. {severityLevel} severity with {predictedCases} predicted cases requires additional resources.";
        }

        return $"Potential reduction of {Math.Abs(adjustment)} officers. {severityLevel} severity with {predictedCases} predicted cases indicates over-allocation.";
    }
    
    private static string GetPrecinctNameFromBarangay(Domain.Enums.Barangay? barangay)
    {
        if (barangay == null) return "Unknown";
        
        return barangay.Value.ToString().Replace("_", " ");
    }
}
