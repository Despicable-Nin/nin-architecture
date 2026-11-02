using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;
using MediatR;

namespace espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastHistoricalData;

public record GetForecastHistoricalDataQuery : IRequest<List<HistoricalDataPointDto>>
{
    public Guid ForecastRunId { get; init; }
}

public class GetForecastHistoricalDataQueryHandler(IForecastRepository forecastRepository)
    : IRequestHandler<GetForecastHistoricalDataQuery, List<HistoricalDataPointDto>>
{
    public async Task<List<HistoricalDataPointDto>> Handle(GetForecastHistoricalDataQuery request, CancellationToken cancellationToken)
    {
        var run = await forecastRepository.GetForecastRunByIdAsync(request.ForecastRunId);
        if (run is null)
        {
            throw new KeyNotFoundException($"Forecast run {request.ForecastRunId} was not found.");
        }

        if (string.IsNullOrWhiteSpace(run.HistoricalDataJson))
        {
            return new List<HistoricalDataPointDto>();
        }

        try
        {
            return ForecastRunSnapshotData.Deserialize(run.HistoricalDataJson).HistoricalData;
        }
        catch (System.Text.Json.JsonException)
        {
            return new List<HistoricalDataPointDto>();
        }
    }
}
