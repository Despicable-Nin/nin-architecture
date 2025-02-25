using espasyo.Application.Common.Models.ML;
using MediatR;

namespace espasyo.Application.Incidents.Queries.GetClusters;

public record GetClustersQuery : IRequest<GetClustersResult>
{
   public DateOnly? DateFrom { get; init; }
   public DateOnly? DateTo { get; init; }
   public string[]? Features { get; init; } = [];
   public int NumberOfClusters { get; init; } = 3;
    public int NumberOfRuns { get; init; } = 1;
}

public record GetClustersResult(IEnumerable<ClusteredModel> Result );