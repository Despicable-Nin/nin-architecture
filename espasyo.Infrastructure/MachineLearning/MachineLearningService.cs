using Microsoft.ML;
using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Domain.Enums;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using System.Collections.Concurrent;

namespace espasyo.Infrastructure.MachineLearning;

public class MachineLearningService(
    MLContext mlContext,
    ILogger<MachineLearningService> logger,
    ITemporalForecastService temporalForecastService
) : IMachineLearningService
{
    public IEnumerable<ClusteredModel> PerformKMeansClustering(IEnumerable<TrainerModel> data, string[]? features, int numberOfClusters = 3, int runs = 10)
    {
        try
        {
            // Default to spatial features and a primary categorical feature if none provided
            // This prevents the curse of dimensionality and ensures geographic proximity is weighted appropriately
            if (features == null || features.Length == 0)
            {
                features = ["Latitude", "Longitude", "CrimeType"];
            }

            logger.LogInformation($"Performing KMeansClustering with features: {features}");
            var trainerModels = data as TrainerModel[] ?? data.ToArray();
            logger.LogInformation("Total input records: {Count}", trainerModels.Count());

            var dataView = mlContext.Data.LoadFromEnumerable(trainerModels);

            // Initialize a pipeline
            IEstimator<ITransformer>? pipeline = null;

            // Dynamically append transformations for each feature
            foreach (var feature in features)
            {
                if (IsCategoricalFeature(feature))
                {
                    // OneHotEncoding for categorical features
                    var oneHotEncoding = mlContext.Transforms.Categorical.OneHotEncoding(feature + "Encoded", feature);
                    pipeline = pipeline == null ? oneHotEncoding : pipeline.Append(oneHotEncoding);
                }
                else if (IsFloat(feature))
                {
                    // Convert float features to Single
                    var dataKindSingle = mlContext.Transforms.Conversion.ConvertType(feature + "Encoded", feature);
                    pipeline = pipeline == null ? dataKindSingle : pipeline.Append(dataKindSingle);
                }
                else
                {
                    // Normalizing numerical features
                    var normalize = mlContext.Transforms.NormalizeMinMax(feature);
                    pipeline = pipeline == null ? normalize : pipeline.Append(normalize);
                }
            }

            // Concatenate all selected encoded features into a single "Features" column
            var selectedFeatureColumns = features.Select(f => IsCategoricalFeature(f) || IsFloat(f) ? $"{f}Encoded" : f).ToArray();
            pipeline = pipeline.Append(mlContext.Transforms.Concatenate("Features", selectedFeatureColumns))
                .Append(mlContext.Transforms.NormalizeMeanVariance("Features"))
                .Append(mlContext.Clustering.Trainers.KMeans(numberOfClusters: numberOfClusters));

            double bestScore = double.MaxValue;
            ITransformer? bestModel = null;

            for (int i = 0; i < runs; i++)
            {
                var model = pipeline.Fit(dataView);
                var predictions = model.Transform(dataView);

                var metrics = mlContext.Clustering.Evaluate(predictions, scoreColumnName: "Score");

                if (metrics.AverageDistance < bestScore)
                {
                    bestScore = metrics.AverageDistance;
                    bestModel = model;
                }
            }

            var finalPredictions = bestModel!.Transform(dataView);
            var clusterPredictions = mlContext.Data.CreateEnumerable<ClusteredModel>(finalPredictions, reuseRowObject: false, true).ToList();

            // Check the count of records after prediction
            logger.LogInformation("Total records after prediction: {Count}", clusterPredictions.Count);

            foreach (var prediction in clusterPredictions)
            {
                Console.WriteLine($"CaseId: {prediction.CaseId}, Assigned Cluster: {prediction.ClusterId}");
            }

            foreach (var clusterId in clusterPredictions.Select(x => x.ClusterId).OrderBy(x => x.ToString()).Distinct().ToArray())
            {
                Console.WriteLine($"ClusterId: {clusterId} {clusterPredictions.Count(x => x.ClusterId == clusterId)}");
            }

            return clusterPredictions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while performing KMeans clustering.");
            throw;
        }
    }
    
    /// <summary>
    /// Performs KMeans clustering and returns a grouped response with cluster details.
    /// </summary>
    /// <param name="data">Enumerable of TrainerModel objects as input data.</param>
    /// <param name="features">Array of feature names to use for clustering.</param>
    /// <param name="numberOfClusters">Number of clusters to create.</param>
    /// <param name="runs">Number of training runs for selecting the best model.</param>
    /// <returns>An enumerable of ClusterResponse objects with the desired structure.</returns>
    public GroupedClusterResponse PerformKMeansAndGetGroupedClusters(IEnumerable<TrainerModel> data, string[]? features, int numberOfClusters = 3, int runs = 10)
    {
        return PerformKMeansAndGetGroupedClusters(data, features, numberOfClusters, runs, autoSelectK: false, seed: null);
    }

    public GroupedClusterResponse PerformKMeansAndGetGroupedClusters(IEnumerable<TrainerModel> data, string[]? features, int numberOfClusters, int runs, bool autoSelectK, int? seed)
    {
        try
        {
            // Default to spatial features and a primary categorical feature if none provided.
            // This prevents the curse of dimensionality and ensures geographic proximity is weighted appropriately
            if (features == null || features.Length == 0)
            {
                features = ["Latitude", "Longitude", "CrimeType"];
            }

            logger.LogInformation("Performing KMeansClustering with features: {Features}, autoSelectK: {AutoSelect}, seed: {Seed}", features, autoSelectK, seed);
            // Materialize data once for reuse
            var trainerModels = data as TrainerModel[] ?? data.ToArray();
            logger.LogInformation("Total input records: {Count}", trainerModels.Length);

            // Auto-select optimal k if requested
            if (autoSelectK || numberOfClusters < 2)
            {
                numberOfClusters = FindOptimalK(trainerModels, features, seed);
                logger.LogInformation("Auto-selected optimal k: {OptimalK}", numberOfClusters);
            }

            // Run final clustering with the selected k
            var (clusterGroups, clusterPredictions) = RunKMeans(trainerModels, features!, numberOfClusters, runs, null);

            // Compute quality metrics
            var qualityMetrics = ComputeQualityMetrics(trainerModels, features!, clusterPredictions, numberOfClusters);

            return new GroupedClusterResponse
            {
                Filters = [],
                ClusterGroups = clusterGroups,
                QualityMetrics = qualityMetrics
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while performing KMeans clustering with features: {Features}, number of clusters: {NumberOfClusters}, runs: {Runs}.", features, numberOfClusters, runs);
            throw;
        }
    }

    private (List<ClusterGroup> clusterGroups, List<ClusteredModel> clusterPredictions) RunKMeans(
        TrainerModel[] trainerModels, string[] features, int numberOfClusters, int runs, int? seed)
    {
        var dataView = mlContext.Data.LoadFromEnumerable(trainerModels);

        var selectedFeatureColumns = features.Select(f => IsCategoricalFeature(f) || IsFloat(f) ? $"{f}Encoded" : f).ToArray();
        IEstimator<ITransformer>? pipeline = AppendPipeline(mlContext, features, null);
        pipeline = pipeline.Append(mlContext.Transforms.Concatenate("Features", selectedFeatureColumns))
                           .Append(mlContext.Transforms.NormalizeMeanVariance("Features"))
                           .Append(mlContext.Clustering.Trainers.KMeans(numberOfClusters: numberOfClusters));

        double bestScore = double.MaxValue;
        ITransformer? bestModel = null;

        for (int i = 0; i < runs; i++)
        {
            var model = pipeline.Fit(dataView);
            var predictions = model.Transform(dataView);
            var metrics = mlContext.Clustering.Evaluate(predictions, scoreColumnName: "Score");

            if (metrics.AverageDistance < bestScore)
            {
                bestScore = metrics.AverageDistance;
                bestModel = model;
            }
        }

        var finalPredictions = bestModel!.Transform(dataView);
        var caseIdToTrainer = trainerModels.ToDictionary(x => x.CaseId ?? string.Empty);
        var clusterPredictions = mlContext.Data
            .CreateEnumerable<ClusteredModel>(finalPredictions, reuseRowObject: false, ignoreMissingColumns: true)
            .ToList();

        logger.LogInformation("Total records after prediction: {Count}", clusterPredictions.Count);

        var groupedClusters = new Dictionary<uint, List<ClusterItem>>();
        foreach (var item in clusterPredictions)
        {
            if (!groupedClusters.TryGetValue(item.ClusterId, out var list))
            {
                list = new List<ClusterItem>();
                groupedClusters[item.ClusterId] = list;
            }
            if (caseIdToTrainer.TryGetValue(item.CaseId, out var trainer))
            {
                list.Add(new ClusterItem
                {
                    CaseId = item.CaseId,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    Month = GetMonth(trainer.TimeStampUnix),
                    Year = GetYear(trainer.TimeStampUnix),
                    TimeOfDay = GetTimeOfDay(trainer.TimeStampUnix),
                    Precinct = (Domain.Enums.Barangay)trainer.PoliceDistrict,
                    CrimeType = (Domain.Enums.CrimeTypeEnum)trainer.CrimeType,
                    ClusterId = item.ClusterId
                });
            }
        }

        var clusterGroups = groupedClusters
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new ClusterGroup
            {
                ClusterId = kvp.Key,
                ClusterItems = kvp.Value
            })
            .ToList();

        return (clusterGroups, clusterPredictions);
    }

    private int FindOptimalK(TrainerModel[] trainerModels, string[] features, int? seed)
    {
        var maxK = Math.Min(15, trainerModels.Length / 2);
        if (maxK < 2) return 2;

        var scores = new Dictionary<int, double>();

        for (int k = 2; k <= maxK; k++)
        {
            try
            {
                var (_, predictions) = RunKMeans(trainerModels, features, k, runs: 1, null);
                var assignments = ExtractFeatureVectors(trainerModels, features, predictions);
                var result = ClusteringMetricsCalculator.Compute(assignments, k);
                scores[k] = result.SilhouetteScores.GetValueOrDefault(k, -1);
            }
            catch
            {
                scores[k] = -1;
            }
        }

        if (scores.Count == 0) return 3;
        return scores.OrderByDescending(s => s.Value).First().Key;
    }

    private static List<(double[] features, uint clusterId)> ExtractFeatureVectors(
        TrainerModel[] trainerModels, string[] features, List<ClusteredModel> predictions)
    {
        var caseIdToCluster = predictions.ToDictionary(p => p.CaseId ?? string.Empty, p => p.ClusterId);
        var result = new List<(double[] features, uint clusterId)>();

        var featureMin = new Dictionary<string, double>();
        var featureMax = new Dictionary<string, double>();

        foreach (var f in features)
        {
            if (IsCategoricalFeature(f) || IsFloat(f)) continue;
            var values = trainerModels.Select(t => (double)typeof(TrainerModel).GetProperty(f)?.GetValue(t)!).ToList();
            featureMin[f] = values.Min();
            featureMax[f] = values.Max();
        }

        foreach (var model in trainerModels)
        {
            if (model.CaseId == null || !caseIdToCluster.TryGetValue(model.CaseId, out var clusterId))
                continue;

            var vector = BuildFeatureVector(model, features, featureMin, featureMax);
            result.Add((vector, clusterId));
        }

        return result;
    }

    private static double[] BuildFeatureVector(TrainerModel model, string[] features,
        Dictionary<string, double> min, Dictionary<string, double> max)
    {
        var parts = new List<double>();

        foreach (var f in features)
        {
            if (IsCategoricalFeature(f))
            {
                var val = (int)(typeof(TrainerModel).GetProperty(f)?.GetValue(model) ?? 0);
                for (int i = 0; i <= 13; i++)
                    parts.Add(i == val ? 1.0 : 0.0);
            }
            else if (IsFloat(f))
            {
                parts.Add((double)(typeof(TrainerModel).GetProperty(f)?.GetValue(model) ?? 0.0));
            }
            else
            {
                var val = (double)(typeof(TrainerModel).GetProperty(f)?.GetValue(model) ?? 0.0);
                var mn = min.GetValueOrDefault(f, 0.0);
                var mx = max.GetValueOrDefault(f, 1.0);
                parts.Add(mx > mn ? (val - mn) / (mx - mn) : 0.5);
            }
        }

        return parts.ToArray();
    }

    private static ClusterQualityMetrics ComputeQualityMetrics(
        TrainerModel[] trainerModels, string[] features,
        List<ClusteredModel> predictions, int selectedK)
    {
        var assignments = ExtractFeatureVectors(trainerModels, features, predictions);
        return ClusteringMetricsCalculator.Compute(assignments, selectedK);
    }

    // Helper methods for extracting Month, Year, and TimeOfDay from Unix timestamp
    private static int GetMonth(long unixTimestamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        return dateTime.Month;
    }

    private static int GetYear(long unixTimestamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        return dateTime.Year;
    }

    private static string GetTimeOfDay(long unixTimestamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        var hour = dateTime.Hour;
        if (hour < 6) return "Night";
        if (hour < 12) return "Morning";
        if (hour < 18) return "Afternoon";
        return "Evening";
    }

    private static void FindBestKMeansModel(MLContext mlContext, int runs, IDataView dataView, IEstimator<ITransformer>? pipeline, ref double bestScore, ref ITransformer? bestModel)
    {
        for (var i = 0; i < runs; i++)
        {
            var model = (pipeline?.Fit(dataView)) ?? throw new InvalidOperationException("Pipeline fitting resulted in a null model.");
            var predictions = model.Transform(dataView);

            var metrics = mlContext.Clustering.Evaluate(predictions, scoreColumnName: "Score");

            if (metrics.AverageDistance < bestScore)
            {
                bestScore = metrics.AverageDistance;
                bestModel = model;
            }
        }
    }

    private static IEstimator<ITransformer>? AppendPipeline(MLContext mlContext, string[]? features, IEstimator<ITransformer>? pipeline)
    {
        if (features == null || features.Length == 0) return pipeline;
        foreach (var feature in features)
        {
            if (IsCategoricalFeature(feature))
            {
                // OneHotEncoding for categorical features.
                var oneHotEncoding = mlContext.Transforms.Categorical.OneHotEncoding(feature + "Encoded", feature);
                pipeline = pipeline == null ? oneHotEncoding : pipeline.Append(oneHotEncoding);
            }
            else if (IsFloat(feature))
            {
                // Convert float features to Single.
                var dataKindSingle = mlContext.Transforms.Conversion.ConvertType(feature + "Encoded", feature);
                pipeline = pipeline == null ? dataKindSingle : pipeline.Append(dataKindSingle);
            }
            else
            {
                // Normalizing numerical features.
                var normalize = mlContext.Transforms.NormalizeMinMax(feature);
                pipeline = pipeline == null ? normalize : pipeline.Append(normalize);
            }
        }

        return pipeline;
    }

    private static bool IsCategoricalFeature(string featureName)
    {
        // Define logic to determine if a feature is categorical
        var categoricalFeatures = new HashSet<string> { "CrimeType", "PoliceDistrict", "Weather", "Motive", "Severity" };
        return categoricalFeatures.Contains(featureName);
    }

    private static bool IsFloat(string featureName)
    {
        string[] floatFeatures = { "Longitude", "Latitude" };
        return floatFeatures.Contains(featureName);
    }

    #region Statistical Forecasting Methods

    public async Task<ForecastResponse> GenerateStatisticalForecast(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters)
    {
        logger.LogInformation("Generating statistical forecast with horizon: {Horizon}, model: {ModelType}", 
            parameters.Horizon, parameters.ModelType);
        return await temporalForecastService.GenerateForecast(clusterData, parameters);
    }

    public async Task<ForecastValidationResult> ValidateForecastModel(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters)
    {
        return await temporalForecastService.ValidateForecastModel(clusterData, parameters);
    }

    public async Task<DataQualityAssessment> AssessDataQuality(IEnumerable<ClusterGroup> clusterData)
    {
        try
        {
            logger.LogInformation("Assessing data quality for forecasting");

            var allItems = clusterData.SelectMany(c => c.ClusterItems).ToList();
            var totalDataPoints = allItems.Count;
            
            var issues = new List<string>();
            var recommendations = new List<string>();

            // Check data completeness
            if (totalDataPoints < 100)
            {
                issues.Add($"Limited data: Only {totalDataPoints} data points available");
                recommendations.Add("Collect more historical data for better forecasting accuracy");
            }

            // Check temporal coverage
            var dateRange = GetDateRange(allItems);
            var monthsCovered = ((dateRange.max.Year - dateRange.min.Year) * 12) + 
                              (dateRange.max.Month - dateRange.min.Month) + 1;

            if (monthsCovered < 24)
            {
                issues.Add($"Short time series: Only {monthsCovered} months of data");
                recommendations.Add("Collect at least 24 months of historical data for reliable forecasting");
            }

            // Detect outliers (simplified approach)
            var monthlyCounts = GetMonthlyCounts(allItems);
            var outliers = DetectOutliers(monthlyCounts);
            var outlierPercentage = (double)outliers.Count / monthlyCounts.Count * 100;

            if (outlierPercentage > 10)
            {
                issues.Add($"High outlier rate: {outlierPercentage:F1}% of data points are outliers");
                recommendations.Add("Review and validate outlier data points");
            }

            var isValid = issues.Count == 0 && totalDataPoints >= 100 && monthsCovered >= 24;

            return new DataQualityAssessment
            {
                IsValid = isValid,
                DataPoints = totalDataPoints,
                OutlierCount = outliers.Count,
                OutlierPercentage = outlierPercentage,
                Issues = issues,
                Recommendations = recommendations
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assessing data quality");
            throw;
        }
    }

    private (DateTime min, DateTime max) GetDateRange(List<ClusterItem> items)
    {
        var dates = items.Select(i => new DateTime(i.Year, i.Month, 1)).ToList();
        return (dates.Min(), dates.Max());
    }

    private List<float> GetMonthlyCounts(List<ClusterItem> items)
    {
        return items
            .GroupBy(i => new { i.Year, i.Month })
            .Select(g => (float)g.Count())
            .ToList();
    }

    private List<float> DetectOutliers(List<float> values)
    {
        if (values.Count < 4) return new List<float>();

        var sorted = values.OrderBy(v => v).ToList();
        var q1 = sorted[sorted.Count / 4];
        var q3 = sorted[3 * sorted.Count / 4];
        var iqr = q3 - q1;
        var lowerBound = q1 - 1.5f * iqr;
        var upperBound = q3 + 1.5f * iqr;

        return values.Where(v => v < lowerBound || v > upperBound).ToList();
    }
    


    
    #endregion

    public async Task<GeoJsonFeatureCollection> PredictHotspotsAsync(
        IEnumerable<ClusterGroup> clusterData,
        HotspotPredictionRequest request)
    {
        var clusterList = clusterData.ToList();

        var forecastParams = new ForecastParameters
        {
            Horizon = request.Horizon,
            ConfidenceLevel = request.ConfidenceLevel,
            ModelType = request.ModelType,
            IncludeSeasonality = request.IncludeSeasonality,
            WeightRecentData = request.WeightRecentData,
            IncludeTimeOfDay = request.IncludeTimeOfDay,
            IncludeMonthOfYear = request.IncludeMonthOfYear,
            IncludeTrend = request.IncludeTrend,
            CrimeTypeFilter = request.CrimeTypeFilter,
            SeverityFilter = request.SeverityFilter
        };

        var forecast = await GenerateStatisticalForecast(clusterList, forecastParams);

        var features = new List<GeoJsonFeature>();

        foreach (var series in forecast.Series)
        {
            var totalPredicted = series.Forecasts.Sum(f => f.Forecast);
            var avgConfidence = series.Forecasts.Average(f => f.Confidence);

            if (totalPredicted < 1) continue;

            var clusterGroup = clusterList.FirstOrDefault(c => c.ClusterId == series.ClusterId);
            if (clusterGroup?.ClusterItems.Count == 0) continue;

            var points = clusterGroup!.ClusterItems
                .Select(i => (i.Longitude, i.Latitude))
                .Distinct()
                .ToList();

            if (points.Count < 3) continue;

            var hull = ComputeConvexHull(points);
            var expandedHull = ExpandHull(hull, 0.002);

            var severity = totalPredicted switch
            {
                > 50 => "critical",
                > 20 => "high",
                > 10 => "medium",
                _ => "low"
            };

            features.Add(new GeoJsonFeature
            {
                Geometry = new GeoJsonGeometry
                {
                    Type = "Polygon",
                    Coordinates = new List<List<List<double>>>
                    {
                        expandedHull.Select(p => new List<double> { p.lon, p.lat }).ToList()
                    }
                },
                Properties = new Dictionary<string, object>
                {
                    ["precinct"] = ((Barangay)series.Precinct).ToString(),
                    ["crimeType"] = ((CrimeTypeEnum)series.CrimeType).ToString(),
                    ["clusterId"] = (int)series.ClusterId,
                    ["totalPredicted"] = Math.Round(totalPredicted, 1),
                    ["confidence"] = Math.Round(avgConfidence, 2),
                    ["severity"] = severity,
                    ["riskLevel"] = series.Forecasts.MaxBy(f => f.Forecast)?.RiskLevel ?? "medium",
                    ["trend"] = series.Forecasts.LastOrDefault()?.Trend ?? "stable",
                    ["forecastPoints"] = series.Forecasts.Select(f => new
                    {
                        month = f.Timestamp.Month,
                        year = f.Timestamp.Year,
                        value = Math.Round(f.Forecast, 1),
                        lower = Math.Round(f.LowerBound, 1),
                        upper = Math.Round(f.UpperBound, 1)
                    }).Cast<object>().ToList()
                }
            });
        }

        return new GeoJsonFeatureCollection { Features = features };
    }

    private static List<(double lon, double lat)> ComputeConvexHull(List<(double lon, double lat)> points)
    {
        if (points.Count < 3) return points;

        var sorted = points.OrderBy(p => p.lat).ThenBy(p => p.lon).ToList();
        var origin = sorted[0];

        var sortedByAngle = sorted.Skip(1)
            .OrderBy(p => Math.Atan2(p.lat - origin.lat, p.lon - origin.lon))
            .ThenBy(p => (p.lon - origin.lon) * (p.lon - origin.lon) + (p.lat - origin.lat) * (p.lat - origin.lat))
            .ToList();

        var hull = new List<(double lon, double lat)> { origin };

        foreach (var point in sortedByAngle)
        {
            while (hull.Count >= 2)
            {
                var a = hull[^2];
                var b = hull[^1];
                var cross = (b.lon - a.lon) * (point.lat - a.lat) - (b.lat - a.lat) * (point.lon - a.lon);
                if (cross > 0) break;
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(point);
        }

        return hull;
    }

    public async Task<List<AnomalyResult>> DetectAnomaliesAsync(IEnumerable<ClusterGroup> clusterData, AnomalyDetectionRequest request)
    {
        return await Task.Run(() =>
        {
            var items = clusterData.SelectMany(g => g.ClusterItems).ToList();
            if (items.Count < 4) return new List<AnomalyResult>();

            var monthlyCounts = items
                .GroupBy(i => new { i.Precinct, i.CrimeType, i.Year, i.Month })
                .Select(g => new
                {
                    g.Key.Precinct,
                    g.Key.CrimeType,
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var series = monthlyCounts
                .GroupBy(x => new { x.Precinct, x.CrimeType })
                .ToList();

            var methods = request.Method.ToLowerInvariant() switch
            {
                "iqr" => new[] { "iqr" },
                "zscore" => new[] { "zscore" },
                "movingavg" => new[] { "movingavg" },
                _ => new[] { "iqr", "zscore", "movingavg" }
            };

            var results = new ConcurrentBag<AnomalyResult>();

            Parallel.ForEach(series, s =>
            {
                var dataPoints = s.Select(x => (x.Year, x.Month, (float)x.Count)).ToList();
                var detected = new HashSet<(int Year, int Month, string Method)>();

                foreach (var method in methods)
                {
                    List<(int Year, int Month, float Count, double Deviation)> flagged = method switch
                    {
                        "iqr" => DetectIqr(dataPoints),
                        "zscore" => DetectZScore(dataPoints),
                        "movingavg" => DetectMovingAvg(dataPoints),
                        _ => new List<(int, int, float, double)>()
                    };

                    foreach (var (year, month, count, deviation) in flagged)
                    {
                        if (detected.Add((year, month, method)))
                        {
                            var factors = GetContributingFactors(dataPoints, year, month, count, deviation);
                            var severity = ClassifySeverity(deviation);

                            results.Add(new AnomalyResult
                            {
                                Precinct = s.Key.Precinct.ToString(),
                                CrimeType = s.Key.CrimeType.ToString(),
                                Year = year,
                                Month = month,
                                ActualCount = count,
                                ExpectedCount = count - deviation,
                                Deviation = deviation,
                                Method = method,
                                Severity = severity,
                                ContributingFactors = factors
                            });
                        }
                    }
                }
            });

            return results.OrderBy(r => r.Year).ThenBy(r => r.Month).ThenBy(r => r.Severity).ToList();
        }, CancellationToken.None);
    }

    private static List<(int Year, int Month, float Count, double Deviation)> DetectIqr(
        List<(int Year, int Month, float Count)> dataPoints)
    {
        if (dataPoints.Count < 4) return new List<(int, int, float, double)>();

        var sorted = dataPoints.Select(d => d.Count).OrderBy(v => v).ToList();
        var q1 = sorted[sorted.Count / 4];
        var q3 = sorted[3 * sorted.Count / 4];
        var iqr = q3 - q1;
        var lowerBound = q1 - 1.5f * iqr;
        var upperBound = q3 + 1.5f * iqr;

        return dataPoints
            .Where(d => d.Count < lowerBound || d.Count > upperBound)
            .Select(d =>
            {
                var baseline = d.Count < lowerBound ? q1 : q3;
                return (d.Year, d.Month, d.Count, (double)(d.Count - baseline));
            })
            .ToList();
    }

    private static List<(int Year, int Month, float Count, double Deviation)> DetectZScore(
        List<(int Year, int Month, float Count)> dataPoints)
    {
        if (dataPoints.Count < 3) return new List<(int, int, float, double)>();

        var mean = dataPoints.Average(d => d.Count);
        var stdDev = Math.Sqrt(dataPoints.Sum(d => Math.Pow(d.Count - mean, 2)) / dataPoints.Count);
        if (stdDev < 1e-10) return new List<(int, int, float, double)>();

        const double threshold = 2.5;

        return dataPoints
            .Where(d => Math.Abs((d.Count - mean) / stdDev) > threshold)
            .Select(d => (d.Year, d.Month, d.Count, (double)(d.Count - mean)))
            .ToList();
    }

    private static List<(int Year, int Month, float Count, double Deviation)> DetectMovingAvg(
        List<(int Year, int Month, float Count)> dataPoints)
    {
        if (dataPoints.Count < 4) return new List<(int, int, float, double)>();

        const int window = 3;
        var flagged = new List<(int Year, int Month, float Count, double Deviation)>();

        for (int i = window; i < dataPoints.Count; i++)
        {
            var windowValues = dataPoints.Skip(i - window).Take(window).Select(d => d.Count).ToList();
            var movingAvg = windowValues.Average();
            var variance = windowValues.Sum(v => Math.Pow(v - movingAvg, 2)) / window;
            var movingStd = Math.Sqrt(variance);
            if (movingStd < 1e-10) continue;

            var current = dataPoints[i];
            var deviation = current.Count - movingAvg;

            if (Math.Abs(deviation) > 2 * movingStd)
            {
                flagged.Add((current.Year, current.Month, current.Count, deviation));
            }
        }

        return flagged;
    }

    private static List<string> GetContributingFactors(
        List<(int Year, int Month, float Count)> dataPoints,
        int year, int month, float count, double deviation)
    {
        var factors = new List<string>();
        var absDeviation = Math.Abs(deviation);

        var mean = dataPoints.Average(d => d.Count);
        var deviationRatio = absDeviation / Math.Max(mean, 1);

        if (deviationRatio > 2.0)
            factors.Add("Extreme deviation from historical average");
        else if (deviationRatio > 1.0)
            factors.Add("Significant deviation from historical average");

        var prevPoint = dataPoints.FirstOrDefault(d => d.Year == year && d.Month == month - 1);
        if (prevPoint == default)
            prevPoint = dataPoints.FirstOrDefault(d => d.Year == year - 1 && d.Month == 12);

        if (prevPoint != default && prevPoint.Count > 0)
        {
            var monthOverMonthChange = Math.Abs(count - prevPoint.Count) / prevPoint.Count;
            if (monthOverMonthChange > 1.0)
                factors.Add($"Sharp {(count > prevPoint.Count ? "increase" : "decrease")} from previous month ({(monthOverMonthChange * 100):F0}%)");
        }

        var sameMonthHistorical = dataPoints
            .Where(d => d.Month == month && !(d.Year == year && d.Month == month))
            .Select(d => d.Count)
            .ToList();

        if (sameMonthHistorical.Count >= 2)
        {
            var seasonalMean = sameMonthHistorical.Average();
            var seasonalStd = Math.Sqrt(sameMonthHistorical.Sum(d => Math.Pow(d - seasonalMean, 2)) / sameMonthHistorical.Count);
            if (seasonalStd > 1e-10 && Math.Abs(count - seasonalMean) / seasonalStd > 2.0)
                factors.Add("Unusual for this time of year (seasonal anomaly)");
        }

        if (deviation > 0)
            factors.Add("Higher than expected incident count");
        else
            factors.Add("Lower than expected incident count");

        return factors;
    }

    private static string ClassifySeverity(double deviation)
    {
        var absDev = Math.Abs(deviation);
        return absDev switch
        {
            > 20 => "critical",
            > 10 => "high",
            > 5 => "medium",
            _ => "low"
        };
    }

    private static List<(double lon, double lat)> ExpandHull(List<(double lon, double lat)> hull, double buffer)
    {
        if (hull.Count < 3) return hull;

        var centroid = (lon: hull.Average(p => p.lon), lat: hull.Average(p => p.lat));

        return hull.Select(p =>
        {
            var dx = p.lon - centroid.lon;
            var dy = p.lat - centroid.lat;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1e-10) return p;
            var scale = 1.0 + buffer / dist;
            return (lon: centroid.lon + dx * scale, lat: centroid.lat + dy * scale);
        }).ToList();
    }
}
