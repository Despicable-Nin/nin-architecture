using System.Text.Json;
using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace espasyo.Application.Services;

public class PipelineResult
{
    public Guid AnalysisRunId { get; set; }
    public Guid ForecastRunId { get; set; }
    public int RecommendationCount { get; set; }
    public int TotalIncidentsProcessed { get; set; }
    public int ClusterCount { get; set; }
    public int ForecastSeriesCount { get; set; }
    public List<string> PrecinctsProcessed { get; set; } = new();
}

public class PipelineRequest
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string[]? Features { get; init; }
    public int NumberOfClusters { get; init; } = 3;
    public int NumberOfRuns { get; init; } = 10;
    public int ForecastHorizon { get; init; } = 6;
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ForecastModelType { get; init; } = "SSA";
    public bool IncludeSeasonality { get; init; } = true;
    public bool WeightRecentData { get; init; } = true;
    public string GeneratedById { get; init; } = string.Empty;
}

public class PipelineOrchestratorService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IMachineLearningService _mlService;
    private readonly IAnalysisRunRepository _analysisRunRepository;
    private readonly IForecastRepository _forecastRepository;
    private readonly IManpowerRecommendationRepository _manpowerRecommendationRepository;
    private readonly IPrecinctRepository _precinctRepository;
    private readonly MLManpowerAllocationService _manpowerService;
    private readonly ILogger<PipelineOrchestratorService> _logger;

    public PipelineOrchestratorService(
        IIncidentRepository incidentRepository,
        IMachineLearningService mlService,
        IAnalysisRunRepository analysisRunRepository,
        IForecastRepository forecastRepository,
        IManpowerRecommendationRepository manpowerRecommendationRepository,
        IPrecinctRepository precinctRepository,
        MLManpowerAllocationService manpowerService,
        ILogger<PipelineOrchestratorService> logger)
    {
        _incidentRepository = incidentRepository;
        _mlService = mlService;
        _analysisRunRepository = analysisRunRepository;
        _forecastRepository = forecastRepository;
        _manpowerRecommendationRepository = manpowerRecommendationRepository;
        _precinctRepository = precinctRepository;
        _manpowerService = manpowerService;
        _logger = logger;
    }

    public async Task<PipelineResult> RunFullPipeline(PipelineRequest request)
    {
        var result = new PipelineResult();
        var startedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation("Pipeline started: dateFrom={DateFrom}, dateTo={DateTo}, k={K}, horizon={Horizon}",
            request.DateFrom, request.DateTo, request.NumberOfClusters, request.ForecastHorizon);

        // ── Stage 1: Fetch incidents ──
        _logger.LogInformation("Stage 1/7: Fetching incidents...");
        KeyValuePair<DateOnly, DateOnly>? dateRange = null;
        if (request.DateFrom.HasValue && request.DateTo.HasValue)
            dateRange = new KeyValuePair<DateOnly, DateOnly>(request.DateFrom.Value, request.DateTo.Value);

        var incidents = (await _incidentRepository.GetAllIncidentsAsync(dateRange)).ToList();
        result.TotalIncidentsProcessed = incidents.Count;
        _logger.LogInformation("Fetched {Count} incidents", incidents.Count);

        if (incidents.Count == 0)
        {
            _logger.LogWarning("No incidents found — pipeline aborted");
            return result;
        }

        // ── Stage 2: Run K-Means clustering ──
        _logger.LogInformation("Stage 2/7: Running K-Means clustering...");
        var trainerModels = incidents.Select(x => new TrainerModel
        {
            Address = x.Address,
            Latitude = x.GetLatitude(),
            Longitude = x.GetLongitude(),
            Severity = (int)x.Severity,
            Weather = (int)x.Weather,
            CaseId = x.CaseId,
            Motive = (int)x.Motive,
            CrimeType = (int)x.CrimeType,
            PoliceDistrict = (int)x.PoliceDistrict,
            TimeStamp = x.TimeStamp.ToString(),
            TimeStampUnix = x.TimeStamp!.Value.ToUnixTimeSeconds()
        }).ToArray();

        var clusterResult = _mlService.PerformKMeansAndGetGroupedClusters(
            trainerModels, request.Features, request.NumberOfClusters, request.NumberOfRuns);

        result.ClusterCount = clusterResult.ClusterGroups.Count();
        _logger.LogInformation("Generated {Count} clusters", result.ClusterCount);

        // ── Stage 3: Persist AnalysisRun ──
        _logger.LogInformation("Stage 3/7: Persisting analysis run...");
        var analysisParams = new
        {
            request.DateFrom,
            request.DateTo,
            request.Features,
            request.NumberOfClusters,
            request.NumberOfRuns
        };

        var analysisRun = new AnalysisRun(
            JsonSerializer.Serialize(analysisParams),
            JsonSerializer.Serialize(clusterResult.ClusterGroups),
            "{}",
            request.GeneratedById);

        await _analysisRunRepository.SaveAsync(analysisRun);
        result.AnalysisRunId = analysisRun.Id;

        // ── Stage 4: Generate forecast ──
        _logger.LogInformation("Stage 4/7: Generating forecast...");
        var forecastParams = new ForecastParameters
        {
            Horizon = request.ForecastHorizon,
            ConfidenceLevel = request.ConfidenceLevel,
            ModelType = request.ForecastModelType,
            IncludeSeasonality = request.IncludeSeasonality,
            WeightRecentData = request.WeightRecentData
        };

        var forecast = await _mlService.GenerateStatisticalForecast(clusterResult.ClusterGroups, forecastParams);
        result.ForecastSeriesCount = forecast.Series.Count;
        _logger.LogInformation("Generated {Count} forecast series", result.ForecastSeriesCount);

        // ── Stage 5: Persist ForecastRun + ForecastResults ──
        _logger.LogInformation("Stage 5/7: Persisting forecast...");
        var precinctCodes = incidents
            .Select(i => i.PoliceDistrict)
            .Distinct()
            .ToList();

        var firstPrecinctId = (await _precinctRepository.GetAllAsync()).FirstOrDefault()?.Id ?? Guid.Empty;

        var forecastRun = new ForecastRun(
            firstPrecinctId,
            request.ForecastHorizon,
            request.ConfidenceLevel,
            ParseModelType(request.ForecastModelType),
            request.GeneratedById);

        var forecastResults = new List<ForecastResult>();
        foreach (var series in forecast.Series)
        {
            foreach (var point in series.Forecasts)
            {
                forecastResults.Add(new ForecastResult(
                    forecastRun.Id,
                    (Barangay)series.Precinct,
                    (CrimeTypeEnum)series.CrimeType,
                    point.Timestamp.Month,
                    point.Timestamp.Year,
                    point.Forecast,
                    point.LowerBound,
                    point.UpperBound,
                    point.Confidence,
                    point.RiskLevel,
                    point.Trend));
            }
        }

        forecastRun.MarkCompleted(forecast.Series.Count);
        await _forecastRepository.SaveForecastRunAsync(forecastRun);
        await _forecastRepository.SaveForecastResultsAsync(forecastResults);
        result.ForecastRunId = forecastRun.Id;

        // ── Stage 6: Run manpower optimization per precinct ──
        _logger.LogInformation("Stage 6/7: Computing manpower recommendations...");
        var allPrecincts = await _precinctRepository.GetAllAsync();
        var recommendations = new List<Domain.Entities.ManpowerRecommendation>();

        var precinctSeriesMap = forecast.Series
            .GroupBy(s => s.Precinct)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var precinct in allPrecincts)
        {
            var precinctBarangay = precinct.Barangay;
            var precinctCode = (int)precinctBarangay;

            if (!precinctSeriesMap.TryGetValue(precinctCode, out var seriesForPrecinct))
                continue;

            var predictedCrimeCounts = new Dictionary<CrimeTypeEnum, int>();
            foreach (var series in seriesForPrecinct)
            {
                var totalPredicted = (int)Math.Round(series.Forecasts.Sum(f => f.Forecast));
                if (totalPredicted > 0)
                    predictedCrimeCounts[(CrimeTypeEnum)series.CrimeType] = totalPredicted;
            }

            if (predictedCrimeCounts.Count == 0) continue;

            var historicalData = incidents
                .Where(i => i.PoliceDistrict == precinctBarangay)
                .Select(i => new HistoricalIncidentData
                {
                    CrimeType = i.CrimeType,
                    Precinct = i.PoliceDistrict,
                    TimeStamp = i.TimeStamp ?? DateTimeOffset.Now,
                    Weather = i.Weather,
                    Severity = i.Severity
                });

            var manpowerRec = await _manpowerService.CalculateOptimalManpowerAsync(
                precinctBarangay, predictedCrimeCounts, historicalData, DateTime.Now.Year);

            foreach (var shift in new[] { ShiftEnum.Morning, ShiftEnum.Evening, ShiftEnum.Night })
            {
                var shiftShare = shift switch
                {
                    ShiftEnum.Morning => 0.4f,
                    ShiftEnum.Evening => 0.35f,
                    ShiftEnum.Night => 0.25f,
                    _ => 0.33f
                };

                var shiftHeadCount = (int)Math.Max(1, Math.Round(manpowerRec.RecommendedManpower * shiftShare));

                var recommendation = new Domain.Entities.ManpowerRecommendation(
                    forecastRun.Id,
                    precinct.Id,
                    shift,
                    shiftHeadCount,
                    manpowerRec.PredictedWorkloadHours * shiftShare,
                    manpowerRec.CrimeComplexityScore,
                    manpowerRec.OptimizationConfidence,
                    manpowerRec.MLJustification);

                await _manpowerRecommendationRepository.SaveAsync(recommendation);
                recommendations.Add(recommendation);
            }

            result.PrecinctsProcessed.Add(precinct.Name);
        }

        result.RecommendationCount = recommendations.Count;
        _logger.LogInformation("Stage 7/7: Generated {Count} manpower recommendations across {Precincts} precincts",
            recommendations.Count, result.PrecinctsProcessed.Count);

        var elapsed = DateTimeOffset.UtcNow - startedAt;
        _logger.LogInformation("Pipeline completed in {Elapsed:mm\\:ss\\.fff}", elapsed);

        return result;
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
