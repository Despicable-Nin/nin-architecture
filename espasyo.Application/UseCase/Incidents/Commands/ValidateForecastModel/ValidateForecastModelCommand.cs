using espasyo.Application.Common.Models.ML;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.ValidateForecastModel;

public record ValidateForecastModelCommand : IRequest<ForecastValidationResult>
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = new List<ClusterGroup>();
    public int Horizon { get; init; } = 6;
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ModelType { get; init; } = "SSA";
    public bool IncludeSeasonality { get; init; } = true;
    public bool WeightRecentData { get; init; } = true;
}
