using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;
using MediatR;

namespace espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastSpatialData;

public record GetForecastSpatialDataQuery : IRequest<IEnumerable<SpatialPredictionDto>>
{
    public Guid ForecastRunId { get; init; }
}

public class GetForecastSpatialDataQueryHandler(IForecastRepository forecastRepository)
    : IRequestHandler<GetForecastSpatialDataQuery, IEnumerable<SpatialPredictionDto>>
{
    public async Task<IEnumerable<SpatialPredictionDto>> Handle(
        GetForecastSpatialDataQuery request,
        CancellationToken cancellationToken)
    {
        var results = await forecastRepository.GetSpatialForecastResultsAsync(request.ForecastRunId);
        return results.Select(result => new SpatialPredictionDto
        {
            Precinct = (int)result.Precinct,
            ClusterId = result.ClusterId,
            Latitude = result.Latitude,
            Longitude = result.Longitude,
            Month = result.Month,
            Year = result.Year,
            PredictedValue = result.PredictedValue,
            LowerBound = result.LowerBound,
            UpperBound = result.UpperBound,
            Confidence = result.Confidence,
            RiskLevel = result.RiskLevel,
            Trend = result.Trend
        });
    }
}
