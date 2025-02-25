using espasyo.Application.Common.Models.ML;
using MediatR;

namespace espasyo.Application.Incidents.Queries.GetGroupedClusters;

public record GetGroupedClustersQuery : IRequest<GroupedClusterResponse>
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string[]? Features { get; init; } = [];
    public int NumberOfClusters { get; init; } = 3;
    public int NumberOfRuns { get; init; } = 1;
    public Filter Filters { get; init; } = new();
}