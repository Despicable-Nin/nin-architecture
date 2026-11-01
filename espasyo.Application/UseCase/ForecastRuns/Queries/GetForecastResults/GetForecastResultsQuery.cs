using System.Text.Json;
using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;
using MediatR;

namespace espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastResults;

public record GetForecastResultsQuery : IRequest<IEnumerable<ForecastResultDto>>
{
    public Guid ForecastRunId { get; init; }
}

public record ForecastResultDto
{
    public Guid Id { get; init; }
    public string Precinct { get; init; } = string.Empty;
    public string CrimeType { get; init; } = string.Empty;
    public string? Shift { get; init; }
    public uint ClusterId { get; init; }
    public int Month { get; init; }
    public int Year { get; init; }
    public double PredictedValue { get; init; }
    public double? LastYearActual { get; init; }
    public double LowerBound { get; init; }
    public double UpperBound { get; init; }
    public double Confidence { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public string Trend { get; init; } = string.Empty;
}

public class GetForecastResultsQueryHandler(IForecastRepository forecastRepository)
    : IRequestHandler<GetForecastResultsQuery, IEnumerable<ForecastResultDto>>
{
    public async Task<IEnumerable<ForecastResultDto>> Handle(GetForecastResultsQuery request, CancellationToken cancellationToken)
    {
        var results = await forecastRepository.GetForecastResultsAsync(request.ForecastRunId);
        var run = await forecastRepository.GetForecastRunByIdAsync(request.ForecastRunId);
        var historicalData = string.IsNullOrWhiteSpace(run?.HistoricalDataJson)
            ? []
            : JsonSerializer.Deserialize<List<HistoricalDataPointDto>>(run.HistoricalDataJson) ?? [];
        var historicalLookup = ForecastYearOverYearComparison.BuildLookup(
            historicalData.Select(point => new ForecastHistoricalObservation(
                point.Precinct,
                point.CrimeType,
                point.Year,
                point.Month,
                point.Count)));

        return results.Select(r => new ForecastResultDto
        {
            Id = r.Id,
            Precinct = r.Precinct.ToString(),
            CrimeType = r.CrimeType.ToString(),
            Shift = r.Shift,
            ClusterId = r.ClusterId,
            Month = r.Month,
            Year = r.Year,
            PredictedValue = r.PredictedValue,
            LastYearActual = ForecastYearOverYearComparison.GetPriorYearActual(
                historicalLookup,
                (int)r.Precinct,
                (int)r.CrimeType,
                r.Year,
                r.Month),
            LowerBound = r.LowerBound,
            UpperBound = r.UpperBound,
            Confidence = r.Confidence,
            RiskLevel = r.RiskLevel,
            Trend = r.Trend
        });
    }
}
