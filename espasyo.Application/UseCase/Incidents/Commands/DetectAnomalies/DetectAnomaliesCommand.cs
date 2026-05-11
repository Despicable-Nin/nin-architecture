using espasyo.Application.Common.Models.ML;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.DetectAnomalies;

public record DetectAnomaliesCommand : IRequest<AnomalyDetectionResponse>
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string Method { get; init; } = "all";
    public string GroupBy { get; init; } = "month";
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = [];
}
