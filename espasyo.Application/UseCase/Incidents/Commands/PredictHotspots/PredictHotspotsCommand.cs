using espasyo.Application.Common.Models.ML;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.PredictHotspots;

public record PredictHotspotsCommand : IRequest<GeoJsonFeatureCollection>
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = [];
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
    public double HotspotThreshold { get; init; } = 0.7;
}
