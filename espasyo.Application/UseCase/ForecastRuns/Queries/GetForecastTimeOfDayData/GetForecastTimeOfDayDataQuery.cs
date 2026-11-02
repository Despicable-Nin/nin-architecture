using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;
using MediatR;

namespace espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastTimeOfDayData;

public record GetForecastTimeOfDayDataQuery : IRequest<List<TimeOfDayPredictionDto>>
{
    public Guid ForecastRunId { get; init; }
}

public class GetForecastTimeOfDayDataQueryHandler(IForecastRepository forecastRepository)
    : IRequestHandler<GetForecastTimeOfDayDataQuery, List<TimeOfDayPredictionDto>>
{
    public async Task<List<TimeOfDayPredictionDto>> Handle(
        GetForecastTimeOfDayDataQuery request,
        CancellationToken cancellationToken)
    {
        var run = await forecastRepository.GetForecastRunByIdAsync(request.ForecastRunId);
        if (run is null)
            throw new KeyNotFoundException($"Forecast run {request.ForecastRunId} was not found.");

        if (string.IsNullOrWhiteSpace(run.HistoricalDataJson))
            return new List<TimeOfDayPredictionDto>();

        try
        {
            return ForecastRunSnapshotData.Deserialize(run.HistoricalDataJson).TimeOfDayPredictions;
        }
        catch (System.Text.Json.JsonException)
        {
            return new List<TimeOfDayPredictionDto>();
        }
    }
}
