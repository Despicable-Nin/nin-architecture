using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastRiskPeriods;

public record GetForecastRiskPeriodsQuery : IRequest<List<MonthlyForecastRiskPeriod>>
{
    public Guid ForecastRunId { get; init; }
}

public class GetForecastRiskPeriodsQueryHandler(IForecastRepository forecastRepository)
    : IRequestHandler<GetForecastRiskPeriodsQuery, List<MonthlyForecastRiskPeriod>>
{
    public async Task<List<MonthlyForecastRiskPeriod>> Handle(
        GetForecastRiskPeriodsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await forecastRepository.GetForecastResultsAsync(request.ForecastRunId);
        return MonthlyForecastRiskAggregator.Aggregate(results.Select(result =>
            new ForecastRiskObservation(
                result.Year,
                result.Month,
                result.PredictedValue,
                result.RiskLevel)));
    }
}
