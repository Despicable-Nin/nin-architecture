using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace espasyo.Application.UseCase.ForecastRuns.Queries.EvaluateForecastRun;

public record EvaluateForecastRunQuery : IRequest<ForecastEvaluationResult>
{
    public Guid ForecastRunId { get; init; }
}

public record ForecastEvaluationResult
{
    public Guid ForecastRunId { get; init; }
    public int TotalComparisons { get; init; }
    public double MeanAbsoluteError { get; init; }
    public double RootMeanSquareError { get; init; }
    public double MeanAbsolutePercentageError { get; init; }
    public bool IsReliable { get; init; }
    public List<ForecastComparisonDetail> Details { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}

public record ForecastComparisonDetail
{
    public string Precinct { get; init; } = string.Empty;
    public string CrimeType { get; init; } = string.Empty;
    public int Month { get; init; }
    public int Year { get; init; }
    public double PredictedValue { get; init; }
    public double ActualValue { get; init; }
    public double AbsoluteError { get; init; }
    public double PercentageError { get; init; }
}

public class EvaluateForecastRunQueryHandler(
    IForecastRepository forecastRepository,
    IIncidentRepository incidentRepository,
    ILogger<EvaluateForecastRunQueryHandler> logger
) : IRequestHandler<EvaluateForecastRunQuery, ForecastEvaluationResult>
{
    public async Task<ForecastEvaluationResult> Handle(EvaluateForecastRunQuery request, CancellationToken cancellationToken)
    {
        var run = await forecastRepository.GetForecastRunByIdAsync(request.ForecastRunId);
        if (run == null)
            throw new ArgumentException($"Forecast run {request.ForecastRunId} not found");

        var forecastResults = (await forecastRepository.GetForecastResultsAsync(request.ForecastRunId)).ToList();
        if (!forecastResults.Any())
            throw new ArgumentException($"No forecast results found for run {request.ForecastRunId}");

        var allIncidents = (await incidentRepository.GetAllIncidentsAsync()).ToList();

        var details = new List<ForecastComparisonDetail>();
        var warnings = new List<string>();

        foreach (var result in forecastResults)
        {
            var year = result.Year;
            var month = result.Month;
            var precinct = result.Precinct;
            var crimeTypeCode = (int)result.CrimeType;

            var actualCount = allIncidents.Count(i =>
                i.TimeStamp.HasValue &&
                i.TimeStamp.Value.Year == year &&
                i.TimeStamp.Value.Month == month &&
                i.PrecinctId != Guid.Empty &&
                i.Precinct?.Barangay == precinct &&
                (int)i.CrimeType == crimeTypeCode);

            var absError = Math.Abs(result.PredictedValue - actualCount);
            var pctError = actualCount > 0
                ? Math.Abs(result.PredictedValue - actualCount) / actualCount * 100
                : result.PredictedValue > 0 ? 100 : 0;

            details.Add(new ForecastComparisonDetail
            {
                Precinct = precinct.ToString(),
                CrimeType = result.CrimeType.ToString(),
                Month = month,
                Year = year,
                PredictedValue = result.PredictedValue,
                ActualValue = actualCount,
                AbsoluteError = absError,
                PercentageError = pctError
            });
        }

        if (!details.Any())
            return new ForecastEvaluationResult
            {
                ForecastRunId = request.ForecastRunId,
                TotalComparisons = 0,
                Warnings = ["No historical data available for the forecast period"]
            };

        var mae = details.Average(d => d.AbsoluteError);
        var rmse = Math.Sqrt(details.Average(d => d.AbsoluteError * d.AbsoluteError));
        var mape = details.Where(d => d.ActualValue > 0).Select(d => d.PercentageError).DefaultIfEmpty(0).Average();

        var unreliableComparisons = details.Count(d => d.PercentageError > 25);
        if (unreliableComparisons > details.Count / 2)
            warnings.Add($"More than half ({unreliableComparisons}/{details.Count}) of comparisons have >25% error");

        if (details.Count(d => d.ActualValue == 0) > details.Count / 2)
            warnings.Add("Most forecast periods have zero actual incidents — evaluation may be unreliable");

        return new ForecastEvaluationResult
        {
            ForecastRunId = request.ForecastRunId,
            TotalComparisons = details.Count,
            MeanAbsoluteError = Math.Round(mae, 2),
            RootMeanSquareError = Math.Round(rmse, 2),
            MeanAbsolutePercentageError = Math.Round(mape, 2),
            IsReliable = mape < 25,
            Details = details,
            Warnings = warnings
        };
    }

    private static readonly Dictionary<Barangay, string> _precinctCodes = new()
    {
        [Barangay.Alabang] = "ALB",
        [Barangay.Ayala_Alabang] = "AAL",
        [Barangay.Sucat] = "SUC",
        [Barangay.Poblacion] = "POB",
        [Barangay.Putatan] = "PUT",
        [Barangay.Tunasan] = "TUN",
        [Barangay.Cupang] = "CUP",
        [Barangay.Bayanan] = "BAY",
        [Barangay.Buli] = "BUL"
    };
}
