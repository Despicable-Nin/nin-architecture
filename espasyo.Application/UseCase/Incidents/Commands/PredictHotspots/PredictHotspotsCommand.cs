using espasyo.Application.Common.Models.ML;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.PredictHotspots;

public record PredictHotspotsCommand : IRequest<GeoJsonFeatureCollection>
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = [];
    public int Horizon { get; init; } = 6;
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ModelType { get; init; } = "SSA";
    public bool IncludeSeasonality { get; init; } = true;
    public bool WeightRecentData { get; init; } = true;
    public double HotspotThreshold { get; init; } = 0.7;
}
