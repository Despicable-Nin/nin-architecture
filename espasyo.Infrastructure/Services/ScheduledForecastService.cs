using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace espasyo.Infrastructure.Services;

public class ScheduledForecastService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledForecastService> _logger;
    private readonly IConfiguration _configuration;

    public ScheduledForecastService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledForecastService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue<bool>("ScheduledForecast:Enabled", false))
        {
            _logger.LogInformation("ScheduledForecast is disabled");
            return;
        }

        var initialDelay = TimeSpan.FromMinutes(
            _configuration.GetValue<int>("ScheduledForecast:InitialDelayMinutes", 5));
        var interval = TimeSpan.FromHours(
            _configuration.GetValue<int>("ScheduledForecast:IntervalHours", 168));

        _logger.LogInformation(
            "ScheduledForecast starting. Initial delay: {Delay}, Interval: {Interval}h",
            initialDelay, interval.TotalHours);

        await Task.Delay(initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunForecastCycle(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled forecast cycle failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunForecastCycle(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var incidentRepo = sp.GetRequiredService<IIncidentRepository>();
        var precinctRepo = sp.GetRequiredService<IPrecinctRepository>();
        var mlService = sp.GetRequiredService<IMachineLearningService>();
        var forecastRepo = sp.GetRequiredService<IForecastRepository>();

        var lookbackYears = _configuration.GetValue<int>("ScheduledForecast:LookbackYears", 3);
        var horizon = _configuration.GetValue<int>("ScheduledForecast:Horizon", 6);
        var modelType = _configuration.GetValue<string>("ScheduledForecast:ModelType", "Linear");
        var confidenceLevel = _configuration.GetValue<double>("ScheduledForecast:ConfidenceLevel", 0.95);

        var dateRange = new KeyValuePair<DateOnly, DateOnly>(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-lookbackYears)),
            DateOnly.FromDateTime(DateTime.UtcNow));

        var incidents = (await incidentRepo.GetFilteredIncidentsAsync(dateRange)).ToList();

        if (incidents.Count == 0)
        {
            _logger.LogWarning("No incidents found for scheduled forecast");
            return;
        }

        _logger.LogInformation("Running scheduled forecast on {Count} incidents", incidents.Count);

        var precincts = (await precinctRepo.GetAllAsync())
            .ToDictionary(p => p.Id, p => p.Barangay);

        var clusterItems = incidents
            .Where(i => precincts.ContainsKey(i.PrecinctId))
            .Select(i => new ClusterItem
            {
                CaseId = i.CaseId ?? "",
                Latitude = i.GetLatitude(),
                Longitude = i.GetLongitude(),
                Month = i.GetMonth(),
                Year = i.GetYear(),
                TimeOfDay = i.GetTimeOfDay() ?? "Unknown",
                Precinct = precincts[i.PrecinctId],
                CrimeType = i.CrimeType,
                ClusterId = 0
            })
            .ToList();

        var clusterGroup = new ClusterGroup
        {
            ClusterId = 0,
            ClusterItems = clusterItems
        };

        var parameters = new ForecastParameters
        {
            Horizon = horizon,
            ConfidenceLevel = confidenceLevel,
            ModelType = modelType,
            IncludeSeasonality = true,
            WeightRecentData = true
        };

        ForecastResponse forecast;
        try
        {
            forecast = await mlService.GenerateStatisticalForecast([clusterGroup], parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled forecast generation failed");
            return;
        }

        if (forecast.Series.Count == 0)
        {
            _logger.LogWarning("Scheduled forecast produced no series");
            return;
        }

        var firstPrecinctId = precincts.Keys.First();
        var run = new ForecastRun(
            firstPrecinctId,
            horizon,
            confidenceLevel,
            ParseModelType(modelType),
            "SYSTEM");

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
        await forecastRepo.SaveForecastRunAsync(run);
        await forecastRepo.SaveForecastResultsAsync(results);

        _logger.LogInformation(
            "Scheduled forecast complete. Run: {RunId}, Series: {SeriesCount}, Results: {ResultCount}",
            run.Id, forecast.Series.Count, results.Count);
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
