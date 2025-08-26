using espasyo.Application.Common.Models.ML;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.AssessDataQuality;

public record AssessDataQualityCommand : IRequest<DataQualityAssessment>
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = new List<ClusterGroup>();
}
