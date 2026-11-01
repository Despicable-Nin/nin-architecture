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
    // Entry point for generating a forecast for all precincts.
    // 1. Groups raw cluster items by precinct and aggregates them into monthly time series.
    // 2. Runs the selected model (linear / seasonal / ssa / ensemble) for each precinct.
    // 3. Computes holdout validation metrics as a quality signal.
    // 4. Packages everything into a ForecastResponse alongside any user-provided thresholds.
    public async Task<ForecastResponse> GenerateForecast(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters)
    {
        try
        {
            logger.LogInformation("Generating temporal forecast with horizon: {Horizon}, model: {ModelType}",
                parameters.Horizon, parameters.ModelType);

            var clusterDataList = clusterData.ToList();
            var forecastSeries = new List<ForecastSeries>();

            // Aggregate incident records into monthly count time series, keyed by precinct.
            var groupedData = GroupClusterDataForForecasting(clusterDataList, parameters);

            foreach (var ((precinct, crimeType), timeSeriesData) in groupedData)
            {
                // Delegate to the model-specific generator (linear / seasonal / ssa / ensemble).
                var seriesResult = await GenerateForecastForSeries(timeSeriesData, parameters, clusterDataList, precinct);

                forecastSeries.Add(new ForecastSeries
                {
                    Precinct = precinct,
                    CrimeType = crimeType,
                    Shift = null,
                    ClusterId = 0,
                    RequestedModel = seriesResult.RequestedModel,
                    EffectiveModel = seriesResult.EffectiveModel,
                    FallbackReason = seriesResult.FallbackReason,
                    Forecasts = seriesResult.Forecasts,
                    Metadata = new Dictionary<string, object>
                    {
                        { "HistoricalDataPoints", timeSeriesData.Count },
                        { "RequestedModel", seriesResult.RequestedModel },
                        { "ModelUsed", seriesResult.EffectiveModel },
                        { "EffectiveModel", seriesResult.EffectiveModel },
                        { "FallbackReason", seriesResult.FallbackReason ?? string.Empty }
                    }
                });
            }

            if (forecastSeries.Count == 0)
            {
                throw new InvalidOperationException(
                    "No cluster data found to generate forecast. Ensure the cluster data contains items with valid precinct information.");
            }

            // Holdout validation: hold back the last 3 months, train on the rest, compare.
            var metrics = await CalculateRealMetricsAsync(groupedData, parameters);

            // If the caller supplied explicit thresholds, prefer them over computed ones.
            var dynamicThresholds = parameters.CustomThresholds != null
                ? new ThresholdCalculationResult
                {
                    GlobalThresholds = parameters.CustomThresholds,
                    CalculationMethod = "user-provided"
                }
                : new ThresholdCalculationResult();

            var effectiveModel = forecastSeries
                .Select(s => s.EffectiveModel)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? parameters.ModelType;
            var fallbackReason = forecastSeries
                .Select(s => s.FallbackReason)
                .FirstOrDefault(reason => !string.IsNullOrWhiteSpace(reason));

            return new ForecastResponse
            {
                Series = forecastSeries,
                Metrics = metrics,
                RequestedModel = parameters.ModelType,
                EffectiveModel = effectiveModel,
                ModelUsed = effectiveModel,
                FallbackReason = fallbackReason,
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

    // Standalone validation: holds out the last 6 months for each precinct, trains on the
    // remaining history, and compares predictions against actuals.  Uses MAPE < 25 % as the
    // reliability threshold.  Precincts with fewer than 24 data points are skipped with a warning.
    public async Task<ForecastValidationResult> ValidateForecastModel(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters)
    {
        try
        {
            logger.LogInformation("Validating forecast model: {ModelType}", parameters.ModelType);

            var groupedData = GroupClusterDataForForecasting(clusterData, parameters);
            var allMetrics = new List<ForecastMetrics>();
            var warnings = new List<string>();
            var recommendations = new List<string>();

            foreach (var ((precinct, _), timeSeriesData) in groupedData)
            {
                // Need at least 24 months: 18 for training + 6 for holdout.
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

                var metrics = CalculateValidationMetrics(testData, predictions.Forecasts);
                allMetrics.Add(metrics);
            }

            var overallMetrics = allMetrics.Count > 0 ? AverageMetrics(allMetrics) : new ForecastMetrics();
            var approvedReliabilityThreshold = 25.0;
            var approvedHighErrorThreshold = 50.0;

            var isReliable = overallMetrics.MeanAbsolutePercentageError < approvedReliabilityThreshold;

            if (!isReliable)
            {
                recommendations.Add("Consider using more historical data or alternative forecasting models");
            }

            if (overallMetrics.MeanAbsolutePercentageError > approvedHighErrorThreshold)
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

    // Flattens cluster-grouped incident items into per-precinct monthly time series.
    // Each incident becomes a single count (Value = 1); records for the same year/month
    // are summed.  An optional CrimeTypeFilter restricts which crime types are included.
    // Missing months are filled with 0 so the forecast models see a complete calendar timeline.
    private Dictionary<(int Precinct, int CrimeType), List<TimeSeriesData>> GroupClusterDataForForecasting(IEnumerable<ClusterGroup> clusterData, ForecastParameters? parameters = null)
    {
        var grouped = new Dictionary<(int Precinct, int CrimeType), List<TimeSeriesData>>();

        var crimeTypeFilter = ParseCrimeTypeFilter(parameters?.CrimeTypeFilter);

        foreach (var cluster in clusterData)
        {
            foreach (var item in cluster.ClusterItems)
            {
                // Skip items whose crime type is not in the filter, if a filter is set.
                if (crimeTypeFilter.Count > 0 && !crimeTypeFilter.Contains(item.CrimeType))
                    continue;

                var key = ((int)item.Precinct, (int)item.CrimeType);

                if (!grouped.ContainsKey(key))
                    grouped[key] = new List<TimeSeriesData>();

                grouped[key].Add(new TimeSeriesData
                {
                    Date = new DateTime(item.Year, item.Month, 1),
                    Value = 1
                });
            }
        }

        // Aggregate duplicates (same precinct + same year-month + same crime type) and sort chronologically.
        // Then fill missing months with 0.
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

            if (aggregated.Count > 0)
            {
                var minDate = aggregated.Min(d => d.Date);
                var maxDate = aggregated.Max(d => d.Date);
                var filled = new List<TimeSeriesData>();
                var cursor = new DateTime(minDate.Year, minDate.Month, 1);

                while (cursor <= maxDate)
                {
                    var existing = aggregated.FirstOrDefault(d => d.Date == cursor);
                    filled.Add(existing ?? new TimeSeriesData { Date = cursor, Value = 0 });
                    cursor = cursor.AddMonths(1);
                }

                grouped[key] = filled;
            }
            else
            {
                grouped[key] = aggregated;
            }
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

    private sealed record ForecastSeriesGenerationResult(
        List<ForecastPoint> Forecasts,
        string RequestedModel,
        string EffectiveModel,
        string? FallbackReason);

    // Dispatches to the model-specific generator based on parameters.ModelType.
    // Runs on a background thread via Task.Run because ML.NET forecasting can be CPU-heavy.
    // If the chosen model throws, falls back to the linear trend model as a safe default.
    private async Task<ForecastSeriesGenerationResult> GenerateForecastForSeries(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                var requestedModel = parameters.ModelType.Trim();
                var modelMatch = requestedModel.ToLower();

                return modelMatch switch
                {
                    "linear" => new ForecastSeriesGenerationResult(
                        GenerateLinearTrendForecast(data, parameters, clusterData, precinct),
                        requestedModel,
                        "linear",
                        null),
                    "seasonal" => new ForecastSeriesGenerationResult(
                        GenerateSeasonalForecast(data, parameters, clusterData, precinct),
                        requestedModel,
                        "seasonal",
                        null),
                    "ensemble" => new ForecastSeriesGenerationResult(
                        GenerateEnsembleForecast(data, parameters, clusterData, precinct),
                        requestedModel,
                        "ensemble",
                        null),
                    "ssa" or _ => GenerateSSAForecast(data, parameters, clusterData, precinct)
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "{ModelType} forecasting failed, falling back to simple linear trend", parameters.ModelType);

                return new ForecastSeriesGenerationResult(
                    GenerateLinearTrendForecast(data, parameters, clusterData, precinct),
                    parameters.ModelType,
                    "linear",
                    $"{parameters.ModelType} forecasting failed and fell back to linear trend");
            }
        });
    }

    private readonly record struct RecencyWeightedTrend(
        double Slope,
        double Intercept,
        double WeightedAverage);

    private static RecencyWeightedTrend FitRecencyWeightedTrend(
        IReadOnlyList<TimeSeriesData> data,
        bool weightRecentData)
    {
        var firstDate = data[0].Date;
        var weightedPoints = data.Select((point, index) =>
        {
            var x = (point.Date.Year - firstDate.Year) * 12 + point.Date.Month - firstDate.Month + 1;
            return (X: (double)x, point.Value, Weight: GetRecencyWeight(data, index, weightRecentData));
        }).ToList();

        var totalWeight = weightedPoints.Sum(point => point.Weight);
        var meanX = weightedPoints.Sum(point => point.X * point.Weight) / totalWeight;
        var meanY = weightedPoints.Sum(point => point.Value * point.Weight) / totalWeight;
        var denominator = weightedPoints.Sum(point => point.Weight * Math.Pow(point.X - meanX, 2));
        var slope = denominator == 0
            ? 0
            : weightedPoints.Sum(point => point.Weight * (point.X - meanX) * (point.Value - meanY)) / denominator;

        return new RecencyWeightedTrend(slope, meanY - slope * meanX, meanY);
    }

    private static double GetRecencyWeight(
        IReadOnlyList<TimeSeriesData> data,
        int index,
        bool weightRecentData)
    {
        if (!weightRecentData)
            return 1;

        var pointDate = data[index].Date;
        var latestDate = data[^1].Date;
        var ageInMonths = (latestDate.Year - pointDate.Year) * 12 + latestDate.Month - pointDate.Month;
        return Math.Pow(0.5, ageInMonths / 12d);
    }

    // Fits a recency-weighted trend over all analysis months and projects it forward.
    private List<ForecastPoint> GenerateLinearTrendForecast(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        var forecasts = new List<ForecastPoint>();

        var recent = data.TakeLast(Math.Min(12, data.Count)).ToList();
        var fit = FitRecencyWeightedTrend(data, parameters.WeightRecentData);
        var n = data.Count;
        var recentTotal = recent.Sum(d => d.Value);

        // For sparse series (≤6 incidents in the recent 12 months), the slope is dominated
        // by noise rather than signal.  Summing hundreds of noisy upward slopes across
        // (precinct × crimeType) combos inflates the aggregated total.  Fall back to
        // the recency-weighted average across the complete analysis history.
        var useAverageFallback = recentTotal <= 6;

        // Begin at the next full calendar month. This avoids projecting months that are
        // already elapsed and ensures the first forecast point matches the next monthly bucket.
        var lastDate = data.Max(d => d.Date);
        var startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
        var gapMonths = (startDate.Year - lastDate.Year) * 12 + (startDate.Month - lastDate.Month);
        var recentAverage = recent.Average(d => d.Value);
        var historicalAverage = data.Average(d => d.Value);

        // Cap extreme extrapolation: a single noisy series should not project
        // a runaway trend, and the forecast should stay within a sane multiple
        // of the series' long-term average.
        var maxSlope = Math.Max(0.1, recentAverage * 0.15);
        var cappedSlope = Math.Max(-maxSlope, Math.Min(maxSlope, fit.Slope));
        var maxForecast = Math.Max(historicalAverage * 2.0, 1.0);

        var runningConfidence = parameters.ConfidenceLevel * 0.8;
        for (int i = 0; i < parameters.Horizon; i++)
        {
            var stepsAhead = gapMonths + i;
            var forecastValue = useAverageFallback
                ? fit.WeightedAverage
                : Math.Max(0, fit.Intercept + cappedSlope * (n + stepsAhead));
            forecastValue = Math.Min(forecastValue, maxForecast);

            // Error margin widens with each future step to reflect growing uncertainty.
            // When WeightRecentData is true the base margin is tighter (15 % vs 20 %),
            // because recent data is assumed to be more predictive.
            var errorMargin = recentAverage * (parameters.WeightRecentData ? 0.15 : 0.2);

            var (trend, riskLevel) = AnalyzeForecastTrend(forecastValue, recentAverage, clusterData, parameters, precinct);

            // Confidence decays each month by a randomized rate around 0.90 (range 0.87-0.93)
            // to reflect growing uncertainty in a non-uniform way.
            if (i > 0)
                runningConfidence *= 0.87 + Random.Shared.NextDouble() * 0.06;

            forecasts.Add(new ForecastPoint
            {
                Timestamp = startDate.AddMonths(i),
                Forecast = forecastValue,
                LowerBound = Math.Max(0, forecastValue - errorMargin * (1 + i * 0.1)),
                UpperBound = forecastValue + errorMargin * (1 + i * 0.1),
                Confidence = Math.Max(0.1, runningConfidence),
                Trend = trend,
                RiskLevel = riskLevel
            });
        }

        return forecasts;
    }

    // Singular Spectrum Analysis (SSA) via ML.NET's built-in ForecasterBySsa.
    // Decomposes the time series into trend, seasonality, and noise components, then
    // reconstructs and projects each component forward.  The window size is constrained
    // to at most 12 and at most half the series length to satisfy SSA's internal invariants.
    // ML.NET SSA natively outputs confidence intervals; when they are missing (unlikely),
    // sensible fallback bounds (±20 %) are used instead.
    private ForecastSeriesGenerationResult GenerateSSAForecast(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        var forecasts = new List<ForecastPoint>();
        var dataView = mlContext.Data.LoadFromEnumerable(data);

        // SSA requires trainSize > 2 * windowSize; cap conservatively.
        var windowSize = Math.Min(12, Math.Max(1, (data.Count - 1) / 2));

        // Begin at the next full calendar month, regardless of the current day-of-month.
        var today = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var startDate = today.AddMonths(1);
        var lastDate = data.Max(d => d.Date);
        var gapMonths = (startDate.Year - lastDate.Year) * 12 + (startDate.Month - lastDate.Month);
        var effectiveHorizon = parameters.Horizon + gapMonths;
        var maxSSAHorizon = Math.Max(1, data.Count - windowSize);
        if (effectiveHorizon > maxSSAHorizon)
        {
            logger.LogWarning("SSA horizon {EffectiveHorizon} exceeds limit {MaxSSAHorizon} for precinct {Precinct}; falling back to linear trend", effectiveHorizon, maxSSAHorizon, precinct);
            var fallbackForecasts = GenerateLinearTrendForecast(data, parameters, clusterData, precinct);
            return new ForecastSeriesGenerationResult(fallbackForecasts, parameters.ModelType, "linear", $"SSA horizon {effectiveHorizon} exceeded limit {maxSSAHorizon}; used linear fallback");
        }

        var pipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: "ForecastedValues",
            inputColumnName: nameof(TimeSeriesData.Value),
            windowSize: windowSize,
            seriesLength: data.Count,
            trainSize: data.Count,
            horizon: effectiveHorizon,
            confidenceLevel: (float)parameters.ConfidenceLevel,
            confidenceLowerBoundColumn: "LowerBoundValues",
            confidenceUpperBoundColumn: "UpperBoundValues");

        var model = pipeline.Fit(dataView);
        var forecastEngine = model.CreateTimeSeriesEngine<TimeSeriesData, ForecastOutput>(mlContext);
        var forecast = forecastEngine.Predict();

        var recentAverage = data.TakeLast(6).Average(d => d.Value);
        var historicalAverage = data.Average(d => d.Value);
        var maxForecast = Math.Max(historicalAverage * 2.0, 1.0);

        var runningConfidence = parameters.ConfidenceLevel;
        for (int i = gapMonths; i < effectiveHorizon; i++)
        {
            var outputIndex = i - gapMonths;
            var forecastDate = startDate.AddMonths(outputIndex);
            var rawForecastValue = forecast.ForecastedValues[i];
            var forecastValue = Math.Min(Math.Max(0, rawForecastValue), maxForecast);
            // Fallback bounds: if ML.NET didn't produce them, use ±20 % of the point forecast.
            var lowerBound = forecast.LowerBoundValues?[i] ?? forecastValue * 0.8f;
            var upperBound = forecast.UpperBoundValues?[i] ?? forecastValue * 1.2f;

            var delta = Math.Max(0.1f, (float)(forecastValue * 0.1));

            if (lowerBound > forecastValue)
            {
                lowerBound = Math.Max(0f, forecastValue - delta);
            }

            if (upperBound < forecastValue)
            {
                upperBound = forecastValue + delta;
            }

            if (upperBound <= lowerBound)
            {
                lowerBound = Math.Max(0f, forecastValue - delta);
                upperBound = forecastValue + delta;
            }

            lowerBound = Math.Max(0f, lowerBound);
            upperBound = Math.Max(upperBound, lowerBound + 0.1f);

            var (trend, riskLevel) = AnalyzeForecastTrend(forecastValue, recentAverage, clusterData, parameters, precinct);

            // Confidence decays each month by a randomized rate around 0.97 (range 0.95-0.99).
            // SSA is the most sophisticated model so decay is gentler than linear/seasonal.
            if (outputIndex > 0)
                runningConfidence *= 0.95 + Random.Shared.NextDouble() * 0.04;

            forecasts.Add(new ForecastPoint
            {
                Timestamp = forecastDate,
                Forecast = forecastValue,
                LowerBound = Math.Max(0, lowerBound),
                UpperBound = Math.Max(0, upperBound),
                Confidence = Math.Max(0.1, runningConfidence),
                Trend = trend,
                RiskLevel = riskLevel
            });
        }

        return new ForecastSeriesGenerationResult(forecasts, parameters.ModelType, "ssa", null);
    }

    // Hybrid seasonal + linear trend model.
    // 1. Fits a recency-weighted trend over the full analysis history.
    // 2. Computes recency-weighted month-of-year averages from the full history.
    // 3. Multiplies the trend projection by the corresponding seasonal multiplier.
    // This captures recurring yearly patterns (e.g. holiday spikes, monsoon lulls)
    // that a pure linear model would miss.
    private List<ForecastPoint> GenerateSeasonalForecast(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        var forecasts = new List<ForecastPoint>();
        var recent = data.TakeLast(Math.Min(12, data.Count)).ToList();
        var fit = FitRecencyWeightedTrend(data, parameters.WeightRecentData);
        var n = data.Count;
        var recentTotal = recent.Sum(d => d.Value);

        // For sparse series (≤6 incidents in 12 months), use the weighted average
        // across the complete analysis history rather than a noisy extrapolation.
        var useAverageFallback = recentTotal <= 6;

        var weightedMonths = data.Select((point, index) => new
        {
            point.Date,
            point.Value,
            Weight = GetRecencyWeight(data, index, parameters.WeightRecentData)
        });
        var monthlyAverages = weightedMonths
            .GroupBy(point => point.Date.Month)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(point => point.Value * point.Weight) / group.Sum(point => point.Weight));

        // Start at the next full calendar month so that the forecast window excludes
        // already-passed months and aligns with the observed monthly buckets.
        var lastDate = data.Max(d => d.Date);
        var startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
        var gapMonths = (startDate.Year - lastDate.Year) * 12 + (startDate.Month - lastDate.Month);
        var recentAverage = recent.Average(d => d.Value);
        var overallAverage = fit.WeightedAverage;

        // Cap extreme extrapolation to keep seasonal forecasts from running away
        // on noisy/sparse series.
        var maxSlope = Math.Max(0.1, recentAverage * 0.15);
        var cappedSlope = Math.Max(-maxSlope, Math.Min(maxSlope, fit.Slope));
        var maxForecast = Math.Max(overallAverage * 2.0, 1.0);

        var runningConfidence = parameters.ConfidenceLevel * 0.9;
        for (int i = 0; i < parameters.Horizon; i++)
        {
            var forecastDate = startDate.AddMonths(i);
            var stepsAhead = gapMonths + i;
            var trendValue = useAverageFallback
                ? fit.WeightedAverage
                : fit.Intercept + cappedSlope * (n + stepsAhead);

            // seasonalMultiplier > 1 means this month is historically busier than the
            // overall monthly average (across all years), and < 1 means quieter.
            // Using overallAverage as the baseline prevents extreme inflation when the
            // recent 12-month window happens to be unusually quiet but the calendar
            // month has normal historical volume — the multiplier reflects genuine
            // seasonal patterns, not a mismatched baseline.
            var seasonalMultiplier = overallAverage > 0
                ? monthlyAverages.GetValueOrDefault(forecastDate.Month, overallAverage) / overallAverage
                : 1.0;
            var forecastValue = Math.Max(0, Math.Min(maxForecast, trendValue * seasonalMultiplier));

            // Wider base margin (25 % vs 20 %) when IncludeSeasonality is true because the
            // seasonal adjustment adds another layer of uncertainty.
            var errorMargin = recentAverage * (parameters.IncludeSeasonality ? 0.25 : 0.2) * (1 + i * 0.08);

            var (trend, riskLevel) = AnalyzeForecastTrend(forecastValue, recentAverage, clusterData, parameters, precinct);

            // Confidence decays each month by a randomized rate around 0.93 (range 0.90-0.96).
            if (i > 0)
                runningConfidence *= 0.90 + Random.Shared.NextDouble() * 0.06;

            forecasts.Add(new ForecastPoint
            {
                Timestamp = forecastDate,
                Forecast = forecastValue,
                LowerBound = Math.Max(0, forecastValue - errorMargin),
                UpperBound = forecastValue + errorMargin,
                Confidence = Math.Max(0.1, runningConfidence),
                Trend = trend,
                RiskLevel = riskLevel
            });
        }

        return forecasts;
    }

    // Runs all three models (SSA, seasonal, linear) independently, then averages their
    // outputs per time step.  The ensemble lower bound is the minimum across models (most
    // pessimistic), the upper bound the maximum (most optimistic), and confidence follows
    // the most confident model.  Trend/risk are decided by majority vote.
    // If every constituent model fails, falls back to the linear trend.
    private List<ForecastPoint> GenerateEnsembleForecast(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        logger.LogInformation("Generating ensemble forecast for precinct {Precinct}", precinct);

        var modelTypes = new[] { "ssa", "seasonal", "linear" };
        var allResults = new Dictionary<string, List<ForecastPoint>>();

        // Run each model independently; a single failure won't sink the whole ensemble.
        foreach (var modelType in modelTypes)
        {
            try
            {
                var modelParams = parameters with { ModelType = modelType };
                allResults[modelType] = modelType switch
                {
                    "ssa" => GenerateSSAForecast(data, modelParams, clusterData, precinct).Forecasts,
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

            // Average point forecast, widest confidence interval, majority trend/risk.
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

    // Classifies the forecast point into a trend direction (increasing / stable / decreasing)
    // and a risk level (low / medium / high / critical).  Trend thresholds (±10 % by default)
    // come from CustomThresholds if provided, otherwise use the built-in 1.1 / 0.9 constants.
    // Risk thresholds can be user-supplied or computed dynamically from the historical data
    // distribution (see CalculateEnhancedDynamicRiskThresholds).  When precinct-specific
    // thresholds exist they are preferred; otherwise the global fallback is used.
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

        // If the caller provided explicit thresholds, use them directly and skip dynamic calculation.
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

    // Computes data-driven risk thresholds by comparing recent (last 6 months) incident
    // volume against older (prior) volume for each precinct/crime-type combination.  The
    // ratio (recent / older) is collected globally (weighted by data volume) and per
    // precinct.  Weighted percentiles (25th, 75th, 90th) determine the low/medium/high/critical
    // boundaries.  Falls back to safe defaults (0.8 / 1.2 / 1.5) when insufficient data
    // is available.
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

            // Group items by (precinct, crimeType); skip groups with ≤ 6 items (too small).
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

                if (items.Count < 12) continue; // Need ≥ 12 months for a meaningful split.

                // Compare the last 6 months to the preceding period, normalized to a per-6-month basis.
                var recentCount = items.TakeLast(6).Count();
                var olderItems = items.Take(items.Count - 6).ToList();
                var avgOlder = olderItems.Count / Math.Max(1.0, (items.Count - 6) / 6.0);

                if (avgOlder > 0)
                {
                    var ratio = recentCount / avgOlder;
                    var weight = items.Count; // More data = more influence on global thresholds.

                    globalRatiosWithWeights.Add((ratio, weight));

                    if (!precinctRatios.ContainsKey(precinct))
                        precinctRatios[precinct] = new List<double>();
                    precinctRatios[precinct].Add(ratio);

                    dataPointsPerPrecinct[precinct] = dataPointsPerPrecinct.GetValueOrDefault(precinct, 0) + items.Count;
                }
            }

            // Compute global thresholds from weighted percentiles (need ≥ 5 data points).
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

            // Compute per-precinct thresholds from simple percentiles (need ≥ 3 ratios).
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
        Dictionary<(int Precinct, int CrimeType), List<TimeSeriesData>> groupedData,
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

                if (predictions.Forecasts.Count == testData.Count)
                {
                    var m = CalculateValidationMetrics(testData, predictions.Forecasts);
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
