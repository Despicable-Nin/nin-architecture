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
        var manpowerAllocations = await _manpowerRepository.GetByYearAsync(request.Year);
        var manpowerDict = manpowerAllocations.ToDictionary(m => m.Precinct, m => m);
        
        var analyses = new List<PrecinctAnalysis>();
        var summary = new ManpowerSummary();

        foreach (var (precinct, predictedCases) in request.PredictedCaseCounts)
        {
            if (!manpowerDict.TryGetValue(precinct, out var manpower))
            {
                // No manpower allocation for this precinct - skip or create default
                continue;
            }

            var severityLevel = manpower.GetSeverityLevel(predictedCases);
            var requiresAdjustment = manpower.RequiresManpowerAdjustment(predictedCases);
            var recommendedAdjustment = manpower.GetRecommendedManpowerAdjustment(predictedCases);
            var recommendedAllocation = Math.Max(1, manpower.AllocatedCount + recommendedAdjustment);

            var justification = GenerateJustification(
                severityLevel, 
                predictedCases, 
                manpower.AllocatedCount, 
                recommendedAdjustment,
                manpower.MildThreshold,
                manpower.ModerateThreshold,
                manpower.CriticalThreshold
            );

            analyses.Add(new PrecinctAnalysis
            {
                Precinct = precinct,
                PrecinctName = precinct.ToString(),
                CurrentAllocation = manpower.AllocatedCount,
                PredictedCases = predictedCases,
                SeverityLevel = severityLevel,
                RequiresAdjustment = requiresAdjustment,
                RecommendedAdjustment = recommendedAdjustment,
                RecommendedAllocation = recommendedAllocation,
                Justification = justification
            });

            // Update summary
            summary.TotalCurrentManpower += manpower.AllocatedCount;
            summary.TotalRecommendedManpower += recommendedAllocation;
            
            if (recommendedAdjustment > 0)
                summary.PrecinctRequiringIncrease++;
            else if (recommendedAdjustment < 0)
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

    private static string GenerateJustification(
        string severityLevel, 
        int predictedCases, 
        int currentAllocation, 
        int adjustment,
        int mildThreshold,
        int moderateThreshold,
        int criticalThreshold)
    {
        if (adjustment == 0)
        {
            return $"Current allocation of {currentAllocation} is adequate for {severityLevel.ToLower()} severity level ({predictedCases} predicted cases).";
        }

        if (adjustment > 0)
        {
            return $"Increase by {adjustment} officers. {severityLevel} severity with {predictedCases} predicted cases (exceeds {criticalThreshold} critical threshold) requires additional resources.";
        }

        return $"Potential reduction of {Math.Abs(adjustment)} officers. {severityLevel} severity with {predictedCases} predicted cases (below {mildThreshold} mild threshold) indicates over-allocation.";
    }
}