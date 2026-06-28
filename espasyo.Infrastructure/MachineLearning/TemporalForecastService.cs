using Microsoft.ML;
using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Domain.Enums;
using Microsoft.ML.Transforms.TimeSeries;

namespace espasyo.Infrastructure.MachineLearning;

public class TemporalForecastService(
    MLContext mlContext,
    ILogger<TemporalForecastService> logger
) : ITemporalForecastService
{
    public async Task<ForecastResponse> GenerateForecast(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters)
    {
        try
        {
            logger.LogInformation("Generating temporal forecast with horizon: {Horizon}, model: {ModelType}",
                parameters.Horizon, parameters.ModelType);

            var clusterDataList = clusterData.ToList();
            var forecastSeries = new List<ForecastSeries>();

            var groupedData = GroupClusterDataForForecasting(clusterDataList, parameters);

            foreach (var (precinct, timeSeriesData) in groupedData)
            {
                var forecasts = await GenerateForecastForSeries(timeSeriesData, parameters, clusterDataList, precinct);

                forecastSeries.Add(new ForecastSeries
                {
                    Precinct = precinct,
                    CrimeType = 0,
                    ClusterId = 0,
                    Forecasts = forecasts,
                    Metadata = new Dictionary<string, object>
                    {
                        { "HistoricalDataPoints", timeSeriesData.Count },
                        { "ModelUsed", parameters.ModelType }
                    }
                });
            }

            if (forecastSeries.Count == 0)
            {
                throw new InvalidOperationException(
                    "No cluster data found to generate forecast. Ensure the cluster data contains items with valid precinct information.");
            }

            var metrics = await CalculateRealMetricsAsync(groupedData, parameters);

            var dynamicThresholds = parameters.CustomThresholds != null
                ? new ThresholdCalculationResult
                {
                    GlobalThresholds = parameters.CustomThresholds,
                    CalculationMethod = "user-provided"
                }
                : new ThresholdCalculationResult();

            return new ForecastResponse
            {
                Series = forecastSeries,
                Metrics = metrics,
                ModelUsed = parameters.ModelType,
                GeneratedAt = DateTime.UtcNow,
                DynamicThresholds = dynamicThresholds
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating temporal forecast");
            throw;
        }
    }

    public async Task<ForecastValidationResult> ValidateForecastModel(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters)
    {
        try
        {
            logger.LogInformation("Validating forecast model: {ModelType}", parameters.ModelType);

            var groupedData = GroupClusterDataForForecasting(clusterData, parameters);
            var allMetrics = new List<ForecastMetrics>();
            var warnings = new List<string>();
            var recommendations = new List<string>();

            foreach (var (precinct, timeSeriesData) in groupedData)
            {
                if (timeSeriesData.Count < 24)
                {
                    warnings.Add($"Insufficient data for reliable validation (Precinct: {precinct})");
                    continue;
                }

                var trainSize = timeSeriesData.Count - 6;
                var trainData = timeSeriesData.Take(trainSize).ToList();
                var testData = timeSeriesData.Skip(trainSize).ToList();

                var testParameters = parameters with { Horizon = 6 };
                var predictions = await GenerateForecastForSeries(trainData, testParameters, clusterData);

                var metrics = CalculateValidationMetrics(testData, predictions);
                allMetrics.Add(metrics);
            }

            var overallMetrics = allMetrics.Count > 0 ? AverageMetrics(allMetrics) : new ForecastMetrics();
            var isReliable = overallMetrics.MeanAbsolutePercentageError < 25.0;

            if (!isReliable)
            {
                recommendations.Add("Consider using more historical data or alternative forecasting models");
            }

            if (overallMetrics.MeanAbsolutePercentageError > 50.0)
            {
                warnings.Add("High forecast error detected - results may not be reliable");
            }

            return new ForecastValidationResult
            {
                Metrics = overallMetrics,
                IsReliable = isReliable,
                Warnings = warnings,
                Recommendations = recommendations
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating forecast model");
            throw;
        }
    }

    private Dictionary<int, List<TimeSeriesData>> GroupClusterDataForForecasting(IEnumerable<ClusterGroup> clusterData, ForecastParameters? parameters = null)
    {
        var grouped = new Dictionary<int, List<TimeSeriesData>>();

        var crimeTypeFilter = ParseCrimeTypeFilter(parameters?.CrimeTypeFilter);

        foreach (var cluster in clusterData)
        {
            foreach (var item in cluster.ClusterItems)
            {
                if (crimeTypeFilter.Count > 0 && !crimeTypeFilter.Contains(item.CrimeType))
                    continue;

                var key = (int)item.Precinct;

                if (!grouped.ContainsKey(key))
                    grouped[key] = new List<TimeSeriesData>();

                grouped[key].Add(new TimeSeriesData
                {
                    Date = new DateTime(item.Year, item.Month, 1),
                    Value = 1
                });
            }
        }

        foreach (var key in grouped.Keys.ToList())
        {
            var aggregated = grouped[key]
                .GroupBy(d => new { d.Date.Year, d.Date.Month })
                .Select(g => new TimeSeriesData
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Value = g.Sum(x => x.Value)
                })
                .OrderBy(d => d.Date)
                .ToList();

            grouped[key] = aggregated;
        }

        return grouped;
    }

    private static HashSet<CrimeTypeEnum> ParseCrimeTypeFilter(string[]? crimeTypeFilter)
    {
        if (crimeTypeFilter is null || crimeTypeFilter.Length == 0)
            return [];

        var result = new HashSet<CrimeTypeEnum>();
        foreach (var name in crimeTypeFilter)
        {
            if (Enum.TryParse<CrimeTypeEnum>(name, ignoreCase: true, out var parsed))
                result.Add(parsed);
        }
        return result;
    }

    private async Task<List<ForecastPoint>> GenerateForecastForSeries(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        return await Task.Run(() =>
        {
            var forecasts = new List<ForecastPoint>();

            try
            {
                forecasts = parameters.ModelType.ToLower() switch
                {
                    "linear" => GenerateLinearTrendForecast(data, parameters, clusterData, precinct),
                    "seasonal" => GenerateSeasonalForecast(data, parameters, clusterData, precinct),
                    "ensemble" => GenerateEnsembleForecast(data, parameters, clusterData, precinct),
                    "ssa" or _ => GenerateSSAForecast(data, parameters, clusterData, precinct)
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "{ModelType} forecasting failed, falling back to simple linear trend", parameters.ModelType);

                forecasts = GenerateLinearTrendForecast(data, parameters, clusterData, precinct);
            }

            return forecasts;
        });
    }

    private List<ForecastPoint> GenerateLinearTrendForecast(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        var forecasts = new List<ForecastPoint>();
        var recent = data.TakeLast(Math.Min(12, data.Count)).ToList();

        var n = recent.Count;
        var sumX = n * (n + 1) / 2;
        var sumY = recent.Sum(d => d.Value);
        var sumXY = recent.Select((d, i) => (i + 1) * d.Value).Sum();
        var sumXX = n * (n + 1) * (2 * n + 1) / 6;

        var slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
        var intercept = (sumY - slope * sumX) / n;

        var lastDate = data.Max(d => d.Date);
        var recentAverage = recent.Average(d => d.Value);

        for (int i = 0; i < parameters.Horizon; i++)
        {
            var forecastValue = Math.Max(0, intercept + slope * (n + i + 1));
            var errorMargin = recentAverage * (parameters.WeightRecentData ? 0.15 : 0.2);

            var (trend, riskLevel) = AnalyzeForecastTrend(forecastValue, recentAverage, clusterData, parameters, precinct);

            var decayedConfidence = parameters.ConfidenceLevel * 0.8 * Math.Pow(0.90, i);

            forecasts.Add(new ForecastPoint
            {
                Timestamp = lastDate.AddMonths(i + 1),
                Forecast = forecastValue,
                LowerBound = Math.Max(0, forecastValue - errorMargin * (1 + i * 0.1)),
                UpperBound = forecastValue + errorMargin * (1 + i * 0.1),
                Confidence = Math.Max(0.1, decayedConfidence),
                Trend = trend,
                RiskLevel = riskLevel
            });
        }

        return forecasts;
    }

    private List<ForecastPoint> GenerateSSAForecast(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        var forecasts = new List<ForecastPoint>();
        var dataView = mlContext.Data.LoadFromEnumerable(data);

        var windowSize = Math.Min(12, Math.Max(1, (data.Count - 1) / 2));

        var pipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: "ForecastedValues",
            inputColumnName: nameof(TimeSeriesData.Value),
            windowSize: windowSize,
            seriesLength: data.Count,
            trainSize: data.Count,
            horizon: parameters.Horizon,
            confidenceLevel: (float)parameters.ConfidenceLevel,
            confidenceLowerBoundColumn: "LowerBoundValues",
            confidenceUpperBoundColumn: "UpperBoundValues");

        var model = pipeline.Fit(dataView);
        var forecastEngine = model.CreateTimeSeriesEngine<TimeSeriesData, ForecastOutput>(mlContext);
        var forecast = forecastEngine.Predict();

        var lastDate = data.Max(d => d.Date);
        var recentAverage = data.TakeLast(6).Average(d => d.Value);

        for (int i = 0; i < parameters.Horizon; i++)
        {
            var forecastDate = lastDate.AddMonths(i + 1);
            var forecastValue = forecast.ForecastedValues[i];
            var lowerBound = forecast.LowerBoundValues?[i] ?? forecastValue * 0.8f;
            var upperBound = forecast.UpperBoundValues?[i] ?? forecastValue * 1.2f;

            var (trend, riskLevel) = AnalyzeForecastTrend(forecastValue, recentAverage, clusterData, parameters, precinct);

            var decayedConfidence = parameters.ConfidenceLevel * Math.Pow(0.97, i);

            forecasts.Add(new ForecastPoint
            {
                Timestamp = forecastDate,
                Forecast = Math.Max(0, forecastValue),
                LowerBound = Math.Max(0, lowerBound),
                UpperBound = Math.Max(0, upperBound),
                Confidence = Math.Max(0.1, decayedConfidence),
                Trend = trend,
                RiskLevel = riskLevel
            });
        }

        return forecasts;
    }

    private List<ForecastPoint> GenerateSeasonalForecast(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        var forecasts = new List<ForecastPoint>();
        var recent = data.TakeLast(Math.Min(12, data.Count)).ToList();

        var n = recent.Count;
        var sumX = n * (n + 1) / 2;
        var sumY = recent.Sum(d => d.Value);
        var sumXY = recent.Select((d, i) => (i + 1) * d.Value).Sum();
        var sumXX = n * (n + 1) * (2 * n + 1) / 6;

        var slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
        var intercept = (sumY - slope * sumX) / n;

        var monthlyAverages = data
            .GroupBy(d => d.Date.Month)
            .ToDictionary(g => g.Key, g => g.Average(d => d.Value));

        var lastDate = data.Max(d => d.Date);
        var recentAverage = recent.Average(d => d.Value);

        for (int i = 0; i < parameters.Horizon; i++)
        {
            var forecastDate = lastDate.AddMonths(i + 1);
            var trendValue = intercept + slope * (n + i + 1);

            var seasonalMultiplier = monthlyAverages.GetValueOrDefault(forecastDate.Month, recentAverage) / recentAverage;
            var forecastValue = Math.Max(0, trendValue * seasonalMultiplier);

            var errorMargin = recentAverage * (parameters.IncludeSeasonality ? 0.25 : 0.2) * (1 + i * 0.08);

            var (trend, riskLevel) = AnalyzeForecastTrend(forecastValue, recentAverage, clusterData, parameters, precinct);

            var decayedConfidence = parameters.ConfidenceLevel * 0.9 * Math.Pow(0.93, i);

            forecasts.Add(new ForecastPoint
            {
                Timestamp = forecastDate,
                Forecast = forecastValue,
                LowerBound = Math.Max(0, forecastValue - errorMargin),
                UpperBound = forecastValue + errorMargin,
                Confidence = Math.Max(0.1, decayedConfidence),
                Trend = trend,
                RiskLevel = riskLevel
            });
        }

        return forecasts;
    }

    private List<ForecastPoint> GenerateEnsembleForecast(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        logger.LogInformation("Generating ensemble forecast for precinct {Precinct}", precinct);

        var modelTypes = new[] { "ssa", "seasonal", "linear" };
        var allResults = new Dictionary<string, List<ForecastPoint>>();

        foreach (var modelType in modelTypes)
        {
            try
            {
                var modelParams = parameters with { ModelType = modelType };
                allResults[modelType] = modelType switch
                {
                    "ssa" => GenerateSSAForecast(data, modelParams, clusterData, precinct),
                    "seasonal" => GenerateSeasonalForecast(data, modelParams, clusterData, precinct),
                    "linear" => GenerateLinearTrendForecast(data, modelParams, clusterData, precinct),
                    _ => throw new InvalidOperationException($"Unknown model type: {modelType}")
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "{ModelType} model failed in ensemble, skipping", modelType);
            }
        }

        if (allResults.Count == 0)
        {
            logger.LogWarning("All ensemble models failed for precinct {Precinct}, falling back to linear trend", precinct);
            return GenerateLinearTrendForecast(data, parameters, clusterData, precinct);
        }

        var horizon = parameters.Horizon;
        var forecasts = new List<ForecastPoint>();

        for (int i = 0; i < horizon; i++)
        {
            var values = allResults.Values
                .Select(r => r.ElementAtOrDefault(i))
                .OfType<ForecastPoint>()
                .ToList();

            if (values.Count == 0) break;

            var ensembleForecast = values.Average(p => p.Forecast);
            var ensembleLower = values.Min(p => p.LowerBound);
            var ensembleUpper = values.Max(p => p.UpperBound);
            var ensembleConfidence = values.Max(p => p.Confidence);
            var ensembleTrend = values.GroupBy(p => p.Trend).OrderByDescending(g => g.Count()).First().Key;
            var ensembleRisk = values.GroupBy(p => p.RiskLevel).OrderByDescending(g => g.Count()).First().Key;

            forecasts.Add(new ForecastPoint
            {
                Timestamp = values[0].Timestamp,
                Forecast = Math.Max(0, ensembleForecast),
                LowerBound = Math.Max(0, ensembleLower),
                UpperBound = Math.Max(0, ensembleUpper),
                Confidence = ensembleConfidence,
                Trend = ensembleTrend,
                RiskLevel = ensembleRisk
            });
        }

        return forecasts;
    }

    private (string trend, string riskLevel) AnalyzeForecastTrend(
        double forecastValue,
        double recentAverage,
        IEnumerable<ClusterGroup> allClusterData,
        ForecastParameters parameters,
        int? targetPrecinct = null)
    {
        var t = parameters.CustomThresholds;

        var trendIncreaseThreshold = t?.TrendIncreaseThreshold ?? 1.1;
        var trendDecreaseThreshold = t?.TrendDecreaseThreshold ?? 0.9;
        var trend = forecastValue > recentAverage * trendIncreaseThreshold ? "increasing" :
                   forecastValue < recentAverage * trendDecreaseThreshold ? "decreasing" : "stable";

        if (t != null)
        {
            var ratio = recentAverage > 0 ? forecastValue / recentAverage : 1.0;
            var riskLevel = ratio > t.HighMax ? "critical" :
                           ratio > t.MediumMax ? "high" :
                           ratio > t.LowMax ? "medium" : "low";
            return (trend, riskLevel);
        }

        var thresholds = CalculateEnhancedDynamicRiskThresholds(allClusterData);

        var activeThresholds = targetPrecinct.HasValue && thresholds.PrecinctSpecificThresholds.ContainsKey(targetPrecinct.Value)
            ? thresholds.PrecinctSpecificThresholds[targetPrecinct.Value]
            : thresholds.GlobalThresholds;

        var ratio2 = recentAverage > 0 ? forecastValue / recentAverage : 1.0;
        var riskLevel2 = ratio2 > activeThresholds.HighMax ? "critical" :
                        ratio2 > activeThresholds.MediumMax ? "high" :
                        ratio2 > activeThresholds.LowMax ? "medium" : "low";

        return (trend, riskLevel2);
    }

    private (double lowMax, double mediumMax, double highMax) CalculateDynamicRiskThresholds(IEnumerable<ClusterGroup> clusterData)
    {
        var enhanced = CalculateEnhancedDynamicRiskThresholds(clusterData);
        return (enhanced.GlobalThresholds.LowMax, enhanced.GlobalThresholds.MediumMax, enhanced.GlobalThresholds.HighMax);
    }

    private ThresholdCalculationResult CalculateEnhancedDynamicRiskThresholds(IEnumerable<ClusterGroup> clusterData)
    {
        try
        {
            var result = new Dictionary<string, object>
            {
                ["GlobalThresholds"] = new { LowMax = 0.8, MediumMax = 1.2, HighMax = 1.5 },
                ["PrecinctSpecificThresholds"] = new Dictionary<int, object>(),
                ["TotalDataPointsUsed"] = 0,
                ["DataPointsPerPrecinct"] = new Dictionary<int, int>(),
                ["GlobalStatistics"] = new Dictionary<string, double>(),
                ["PrecinctStatistics"] = new Dictionary<int, Dictionary<string, double>>(),
                ["Warnings"] = new List<string>()
            };

            var warnings = (List<string>)result["Warnings"];
            var precinctThresholds = (Dictionary<int, object>)result["PrecinctSpecificThresholds"];
            var dataPointsPerPrecinct = (Dictionary<int, int>)result["DataPointsPerPrecinct"];
            var precinctStats = (Dictionary<int, Dictionary<string, double>>)result["PrecinctStatistics"];

            var allGroups = clusterData
                .SelectMany(c => c.ClusterItems)
                .GroupBy(item => new { Precinct = (int)item.Precinct, CrimeType = (int)item.CrimeType })
                .Where(g => g.Count() > 6)
                .ToList();

            if (allGroups.Count == 0)
            {
                warnings.Add("No sufficient data for threshold calculation, using defaults");
                return CreateThresholdResult(result, warnings);
            }

            var globalRatiosWithWeights = new List<(double ratio, int weight)>();
            var precinctRatios = new Dictionary<int, List<double>>();

            foreach (var group in allGroups)
            {
                var precinct = group.Key.Precinct;
                var items = group.OrderBy(i => new DateTime(i.Year, i.Month, 1)).ToList();

                if (items.Count < 12) continue;

                var recentCount = items.TakeLast(6).Count();
                var olderItems = items.Take(items.Count - 6).ToList();
                var avgOlder = olderItems.Count / Math.Max(1.0, (items.Count - 6) / 6.0);

                if (avgOlder > 0)
                {
                    var ratio = recentCount / avgOlder;
                    var weight = items.Count;

                    globalRatiosWithWeights.Add((ratio, weight));

                    if (!precinctRatios.ContainsKey(precinct))
                        precinctRatios[precinct] = new List<double>();
                    precinctRatios[precinct].Add(ratio);

                    dataPointsPerPrecinct[precinct] = dataPointsPerPrecinct.GetValueOrDefault(precinct, 0) + items.Count;
                }
            }

            if (globalRatiosWithWeights.Count >= 5)
            {
                var globalThresholds = CalculateWeightedPercentileThresholds(globalRatiosWithWeights);
                result["GlobalThresholds"] = new
                {
                    LowMax = globalThresholds.lowMax,
                    MediumMax = globalThresholds.mediumMax,
                    HighMax = globalThresholds.highMax
                };

                var totalWeight = globalRatiosWithWeights.Sum(x => x.weight);
                var weightedMean = globalRatiosWithWeights.Sum(x => x.ratio * x.weight) / totalWeight;
                var globalStats = new Dictionary<string, double>
                {
                    ["WeightedMean"] = weightedMean,
                    ["TotalDataPoints"] = globalRatiosWithWeights.Sum(x => x.weight),
                    ["PrecinctsCovered"] = precinctRatios.Keys.Count
                };
                result["GlobalStatistics"] = globalStats;
                result["TotalDataPointsUsed"] = (int)globalStats["TotalDataPoints"];
            }
            else
            {
                warnings.Add($"Insufficient data for global thresholds ({globalRatiosWithWeights.Count} data points), using defaults");
            }

            foreach (var (precinct, ratios) in precinctRatios)
            {
                if (ratios.Count >= 3)
                {
                    var sortedRatios = ratios.OrderBy(r => r).ToList();
                    var precinctThreshold = CalculateSimplePercentileThresholds(sortedRatios);

                    precinctThresholds[precinct] = new
                    {
                        LowMax = precinctThreshold.lowMax,
                        MediumMax = precinctThreshold.mediumMax,
                        HighMax = precinctThreshold.highMax
                    };

                    precinctStats[precinct] = new Dictionary<string, double>
                    {
                        ["Mean"] = ratios.Average(),
                        ["DataPoints"] = ratios.Count,
                        ["Min"] = ratios.Min(),
                        ["Max"] = ratios.Max()
                    };
                }
                else
                {
                    warnings.Add($"Insufficient data for precinct {precinct} specific thresholds ({ratios.Count} data points), using global");
                }
            }

            logger.LogInformation("Enhanced thresholds calculated: Global from {GlobalPoints} weighted data points, {PrecinctCount} precinct-specific thresholds",
                globalRatiosWithWeights.Sum(x => x.weight), precinctThresholds.Count);

            return CreateThresholdResult(result, warnings);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error calculating enhanced dynamic thresholds, using defaults");
            var fallbackResult = new Dictionary<string, object>
            {
                ["GlobalThresholds"] = new { LowMax = 0.8, MediumMax = 1.2, HighMax = 1.5 },
                ["PrecinctSpecificThresholds"] = new Dictionary<int, object>(),
                ["TotalDataPointsUsed"] = 0,
                ["DataPointsPerPrecinct"] = new Dictionary<int, int>(),
                ["GlobalStatistics"] = new Dictionary<string, double>(),
                ["PrecinctStatistics"] = new Dictionary<int, Dictionary<string, double>>(),
                ["Warnings"] = new List<string> { "Error in threshold calculation, using safe defaults" }
            };
            return CreateThresholdResult(fallbackResult, (List<string>)fallbackResult["Warnings"]);
        }
    }

    private (double lowMax, double mediumMax, double highMax) CalculateWeightedPercentileThresholds(
        List<(double ratio, int weight)> weightedRatios)
    {
        var sorted = weightedRatios.OrderBy(x => x.ratio).ToList();
        var totalWeight = sorted.Sum(x => x.weight);

        var percentile25 = CalculateWeightedPercentile(sorted, totalWeight, 0.25);
        var percentile75 = CalculateWeightedPercentile(sorted, totalWeight, 0.75);
        var percentile90 = CalculateWeightedPercentile(sorted, totalWeight, 0.90);

        var lowMax = Math.Max(0.6, Math.Min(1.0, percentile25));
        var mediumMax = Math.Max(1.0, Math.Min(1.4, percentile75));
        var highMax = Math.Max(1.3, Math.Min(2.0, percentile90));

        return (lowMax, mediumMax, highMax);
    }

    private static double CalculateWeightedPercentile(
        List<(double ratio, int weight)> sortedWeightedRatios,
        int totalWeight,
        double percentile)
    {
        var targetWeight = totalWeight * percentile;
        var cumulativeWeight = 0;

        for (int i = 0; i < sortedWeightedRatios.Count; i++)
        {
            cumulativeWeight += sortedWeightedRatios[i].weight;

            if (cumulativeWeight >= targetWeight)
            {
                if (i > 0 && cumulativeWeight > targetWeight)
                {
                    var prevWeight = cumulativeWeight - sortedWeightedRatios[i].weight;
                    var ratio = (targetWeight - prevWeight) / sortedWeightedRatios[i].weight;
                    return sortedWeightedRatios[i - 1].ratio +
                           (sortedWeightedRatios[i].ratio - sortedWeightedRatios[i - 1].ratio) * ratio;
                }
                return sortedWeightedRatios[i].ratio;
            }
        }

        return sortedWeightedRatios.LastOrDefault().ratio;
    }

    private static (double lowMax, double mediumMax, double highMax) CalculateSimplePercentileThresholds(
        List<double> sortedRatios)
    {
        if (sortedRatios.Count == 0) return (0.8, 1.2, 1.5);

        var percentile25 = sortedRatios[Math.Max(0, (int)(sortedRatios.Count * 0.25))];
        var percentile75 = sortedRatios[Math.Max(0, (int)(sortedRatios.Count * 0.75))];
        var percentile90 = sortedRatios[Math.Max(0, (int)(sortedRatios.Count * 0.90))];

        var lowMax = Math.Max(0.6, Math.Min(1.0, percentile25));
        var mediumMax = Math.Max(1.0, Math.Min(1.4, percentile75));
        var highMax = Math.Max(1.3, Math.Min(2.0, percentile90));

        return (lowMax, mediumMax, highMax);
    }

    private static ThresholdCalculationResult CreateThresholdResult(
        Dictionary<string, object> result,
        List<string> warnings)
    {
        var globalThresholds = (dynamic)result["GlobalThresholds"];
        var precinctThresholds = (Dictionary<int, object>)result["PrecinctSpecificThresholds"];

        return new ThresholdCalculationResult
        {
            GlobalThresholds = new DynamicThresholds
            {
                LowMax = globalThresholds.LowMax,
                MediumMax = globalThresholds.MediumMax,
                HighMax = globalThresholds.HighMax
            },
            PrecinctSpecificThresholds = precinctThresholds.ToDictionary(
                kvp => kvp.Key,
                kvp => {
                    var threshold = (dynamic)kvp.Value;
                    return new DynamicThresholds
                    {
                        LowMax = threshold.LowMax,
                        MediumMax = threshold.MediumMax,
                        HighMax = threshold.HighMax
                    };
                }
            ),
            TotalDataPointsUsed = (int)result["TotalDataPointsUsed"],
            DataPointsPerPrecinct = (Dictionary<int, int>)result["DataPointsPerPrecinct"],
            GlobalStatistics = (Dictionary<string, double>)result["GlobalStatistics"],
            PrecinctStatistics = (Dictionary<int, Dictionary<string, double>>)result["PrecinctStatistics"],
            Warnings = warnings
        };
    }

    private async Task<ForecastMetrics> CalculateRealMetricsAsync(
        Dictionary<int, List<TimeSeriesData>> groupedData,
        ForecastParameters parameters)
    {
        const int HoldoutMonths = 3;
        const int MinTrainMonths = 6;

        var allMetrics = new List<ForecastMetrics>();

        foreach (var (_, timeSeriesData) in groupedData)
        {
            if (timeSeriesData.Count < MinTrainMonths + HoldoutMonths)
                continue;

            var trainData = timeSeriesData.Take(timeSeriesData.Count - HoldoutMonths).ToList();
            var testData = timeSeriesData.Skip(timeSeriesData.Count - HoldoutMonths).ToList();

            try
            {
                var holdoutParams = parameters with { Horizon = HoldoutMonths };
                var predictions = await GenerateForecastForSeries(trainData, holdoutParams, Enumerable.Empty<ClusterGroup>());

                if (predictions.Count == testData.Count)
                {
                    var m = CalculateValidationMetrics(testData, predictions);
                    allMetrics.Add(m);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Holdout validation failed for one series — skipping it in metric aggregation.");
            }
        }

        if (allMetrics.Count == 0)
        {
            logger.LogWarning("No series had sufficient data for holdout validation. Returning zeroed metrics.");
            return new ForecastMetrics
            {
                MeanAbsoluteError = 0,
                RootMeanSquareError = 0,
                MeanAbsolutePercentageError = 0,
                ModelAccuracy = 0
            };
        }

        var result = AverageMetrics(allMetrics);
        logger.LogInformation(
            "Forecast accuracy from {Count} series holdout — MAE: {MAE:F2}, RMSE: {RMSE:F2}, MAPE: {MAPE:F1}%, Accuracy: {Acc:P0}",
            allMetrics.Count, result.MeanAbsoluteError, result.RootMeanSquareError,
            result.MeanAbsolutePercentageError, result.ModelAccuracy);

        return result;
    }

    private static ForecastMetrics CalculateValidationMetrics(List<TimeSeriesData> actual, List<ForecastPoint> predicted)
    {
        if (actual.Count != predicted.Count)
            throw new ArgumentException("Actual and predicted data must have same length");

        var errors = actual.Zip(predicted, (a, p) => Math.Abs(a.Value - (float)p.Forecast)).ToList();
        var relativeErrors = actual.Zip(predicted, (a, p) =>
            a.Value != 0 ? Math.Abs(a.Value - (float)p.Forecast) / a.Value : 0).ToList();

        var mae = errors.Average();
        var rmse = Math.Sqrt(errors.Select(e => e * e).Average());
        var mape = relativeErrors.Average() * 100;
        var accuracy = Math.Max(0, 1 - mape / 100);

        return new ForecastMetrics
        {
            MeanAbsoluteError = mae,
            RootMeanSquareError = rmse,
            MeanAbsolutePercentageError = mape,
            ModelAccuracy = accuracy
        };
    }

    private static ForecastMetrics AverageMetrics(List<ForecastMetrics> metrics)
    {
        return new ForecastMetrics
        {
            MeanAbsoluteError = metrics.Average(m => m.MeanAbsoluteError),
            RootMeanSquareError = metrics.Average(m => m.RootMeanSquareError),
            MeanAbsolutePercentageError = metrics.Average(m => m.MeanAbsolutePercentageError),
            ModelAccuracy = metrics.Average(m => m.ModelAccuracy)
        };
    }
}
