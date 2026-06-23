using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastRun;

public record SaveForecastRunCommand : IRequest<Guid>
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = new List<ClusterGroup>();
    public Guid PrecinctId { get; init; }
    public int Horizon { get; init; } = 6;
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ModelType { get; init; } = "SSA";
    public bool IncludeSeasonality { get; init; } = true;
    public bool WeightRecentData { get; init; } = true;
    public string GeneratedById { get; init; } = string.Empty;
}

public class SaveForecastRunCommandHandler(
    IMachineLearningService machineLearningService,
    IForecastRepository forecastRepository,
    ILogger<SaveForecastRunCommandHandler> logger
) : IRequestHandler<SaveForecastRunCommand, Guid>
{
    public async Task<Guid> Handle(SaveForecastRunCommand request, CancellationToken cancellationToken)
    {
        var modelType = ParseModelType(request.ModelType);

        var run = new ForecastRun(
            request.PrecinctId,
            request.Horizon,
            request.ConfidenceLevel,
            modelType,
            request.GeneratedById);

        try
        {
            var parameters = new ForecastParameters
            {
                Horizon = request.Horizon,
                ConfidenceLevel = request.ConfidenceLevel,
                ModelType = request.ModelType,
                IncludeSeasonality = request.IncludeSeasonality,
                WeightRecentData = request.WeightRecentData
            };

            var forecast = await machineLearningService.GenerateStatisticalForecast(request.ClusterData, parameters);

            var results = new List<ForecastResult>();
            foreach (var series in forecast.Series)
            {
                foreach (var point in series.Forecasts)
                {
                    results.Add(new ForecastResult(
                        run.Id,
                        (Barangay)series.Precinct,
                        (CrimeTypeEnum)series.CrimeType,
                        point.Timestamp.Month,
                        point.Timestamp.Year,
                        point.Forecast,
                        point.LowerBound,
                        point.UpperBound,
                        point.Confidence,
                        point.RiskLevel,
                        point.Trend,
                        series.ClusterId));
                }
            }

            run.MarkCompleted(forecast.Series.Count);

            await forecastRepository.SaveForecastRunAsync(run);
            await forecastRepository.SaveForecastResultsAsync(results);

            logger.LogInformation("Saved forecast run {RunId} with {ResultCount} results", run.Id, results.Count);

            return run.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Forecast run {RunId} failed", run.Id);
            run.MarkFailed();
            await forecastRepository.SaveForecastRunAsync(run);
            throw;
        }
    }

    private static ForecastModelTypeEnum ParseModelType(string modelType)
    {
        return modelType.ToLowerInvariant() switch
        {
            "ssa" => ForecastModelTypeEnum.SSA,
            "linear" => ForecastModelTypeEnum.Linear,
            "seasonal" => ForecastModelTypeEnum.Seasonal,
            "ensemble" => ForecastModelTypeEnum.Ensemble,
            _ => ForecastModelTypeEnum.SSA
        };
    }
}
