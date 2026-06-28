using espasyo.Application.Common.Models.ML;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.GenerateStatisticalForecast;

public record GenerateStatisticalForecastCommand : IRequest<ForecastResponse>
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
    public string[] PredictionTypes { get; init; } = ["temporal"];
}
