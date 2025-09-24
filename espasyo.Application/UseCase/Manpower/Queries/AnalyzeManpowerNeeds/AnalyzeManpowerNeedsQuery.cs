using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.UseCase.Manpower.Queries.AnalyzeManpowerNeeds;

public class AnalyzeManpowerNeedsQuery : IRequest<ManpowerAnalysisResponse>
{
    public int Year { get; set; }
    public Dictionary<Barangay, int> PredictedCaseCounts { get; set; } = new();
}

public class ManpowerAnalysisResponse
{
    public int Year { get; set; }
    public IEnumerable<PrecinctAnalysis> PrecinctAnalyses { get; set; } = Array.Empty<PrecinctAnalysis>();
    public ManpowerSummary Summary { get; set; } = new();
}

public class PrecinctAnalysis
{
    public Barangay Precinct { get; set; }
    public string PrecinctName { get; set; } = string.Empty;
    public int CurrentAllocation { get; set; }
    public int PredictedCases { get; set; }
    public string SeverityLevel { get; set; } = string.Empty;
    public bool RequiresAdjustment { get; set; }
    public int RecommendedAdjustment { get; set; }
    public int RecommendedAllocation { get; set; }
    public string Justification { get; set; } = string.Empty;
}

public class ManpowerSummary
{
    public int TotalCurrentManpower { get; set; }
    public int TotalRecommendedManpower { get; set; }
    public int NetAdjustment { get; set; }
    public int PrecinctRequiringIncrease { get; set; }
    public int PrecinctRequiringDecrease { get; set; }
    public int PrecinctWithoutChange { get; set; }
}