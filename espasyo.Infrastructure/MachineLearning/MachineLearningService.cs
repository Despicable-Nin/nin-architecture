using Microsoft.ML;
using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Domain.Enums;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;

namespace espasyo.Infrastructure.MachineLearning;

public class MachineLearningService(
    MLContext mlContext,
    ILogger<MachineLearningService> logger
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
        try
        {
            logger.LogInformation("Generating statistical forecast with horizon: {Horizon}, model: {ModelType}", 
                parameters.Horizon, parameters.ModelType);

            var clusterDataList = clusterData.ToList(); // Cache for reuse
            var forecastSeries = new List<ForecastSeries>();

            // Group cluster data by precinct and crime type
            var groupedData = GroupClusterDataForForecasting(clusterDataList);

            foreach (var (key, timeSeriesData) in groupedData)
            {
                var (precinct, crimeType, clusterId) = key;

                if (timeSeriesData.Count < 12) // Need at least 12 months of data
                {
                    logger.LogWarning("Insufficient data for precinct {Precinct}, crime type {CrimeType}, cluster {ClusterId}. Skipping forecasting.", precinct, crimeType, clusterId);
                    
                    // TODO (Improvement): To handle sparse data, implement a fallback that aggregates this data 
                    // to a higher level (e.g., all crimes in this precinct, or grouping similar crime types) 
                    // before attempting to run Singular Spectrum Analysis (SSA), which requires dense time series.
                    continue;
                }

                var forecasts = await GenerateForecastForSeries(timeSeriesData, parameters, clusterDataList, precinct);
                
                forecastSeries.Add(new ForecastSeries
                {
                    Precinct = precinct,
                    CrimeType = crimeType,
                    ClusterId = clusterId,
                    Forecasts = forecasts,
                    Metadata = new Dictionary<string, object>
                    {
                        { "HistoricalDataPoints", timeSeriesData.Count },
                        { "ModelUsed", parameters.ModelType }
                    }
                });
            }

            // Calculate real accuracy metrics using a holdout pass over each series
            var metrics = await CalculateRealMetricsAsync(groupedData, parameters);

            return new ForecastResponse
            {
                Series = forecastSeries,
                Metrics = metrics,
                ModelUsed = parameters.ModelType,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating statistical forecast");
            throw;
        }
    }

    public async Task<ForecastValidationResult> ValidateForecastModel(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters)
    {
        try
        {
            logger.LogInformation("Validating forecast model: {ModelType}", parameters.ModelType);

            var groupedData = GroupClusterDataForForecasting(clusterData);
            var allMetrics = new List<ForecastMetrics>();
            var warnings = new List<string>();
            var recommendations = new List<string>();

            foreach (var (key, timeSeriesData) in groupedData)
            {
                if (timeSeriesData.Count < 24) // Need at least 24 months for validation
                {
                    warnings.Add($"Insufficient data for reliable validation (Precinct: {key.Item1}, Crime: {key.Item2}, Cluster: {key.Item3})");
                    continue;
                }

                // Use last 6 months as test data
                var trainSize = timeSeriesData.Count - 6;
                var trainData = timeSeriesData.Take(trainSize).ToList();
                var testData = timeSeriesData.Skip(trainSize).ToList();

                var testParameters = parameters with { Horizon = 6 };
                var predictions = await GenerateForecastForSeries(trainData, testParameters, clusterData);

                var metrics = CalculateValidationMetrics(testData, predictions);
                allMetrics.Add(metrics);
            }

            var overallMetrics = allMetrics.Count > 0 ? AverageMetrics(allMetrics) : new ForecastMetrics();
            var isReliable = overallMetrics.MeanAbsolutePercentageError < 25.0; // MAPE < 25% is generally acceptable

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

    private Dictionary<(int, int, uint), List<TimeSeriesData>> GroupClusterDataForForecasting(IEnumerable<ClusterGroup> clusterData)
    {
        var grouped = new Dictionary<(int, int, uint), List<TimeSeriesData>>();

        foreach (var cluster in clusterData)
        {
            foreach (var item in cluster.ClusterItems)
            {
                var key = ((int)item.Precinct, (int)item.CrimeType, item.ClusterId);
                
                if (!grouped.ContainsKey(key))
                    grouped[key] = new List<TimeSeriesData>();

                grouped[key].Add(new TimeSeriesData
                {
                    Date = new DateTime(item.Year, item.Month, 1),
                    Value = 1 // Each incident counts as 1
                });
            }
        }

        // Aggregate by month and sort
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

    private async Task<List<ForecastPoint>> GenerateForecastForSeries(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        return await Task.Run(() =>
        {
            var forecasts = new List<ForecastPoint>();

            try
            {
                // Choose forecasting method based on model type
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
                
                // Fallback to simple linear trend
                forecasts = GenerateLinearTrendForecast(data, parameters, clusterData, precinct);
            }

            return forecasts;
        });
    }

    private List<ForecastPoint> GenerateLinearTrendForecast(List<TimeSeriesData> data, ForecastParameters parameters, IEnumerable<ClusterGroup> clusterData, int? precinct = null)
    {
        var forecasts = new List<ForecastPoint>();
        var recent = data.TakeLast(Math.Min(12, data.Count)).ToList();
        
        // Calculate linear trend
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
            
            var (trend, riskLevel) = AnalyzeForecastTrend(forecastValue, recentAverage, clusterData, precinct);

            // Confidence decays with horizon: linear trend is less reliable further ahead (10% decay per month)
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

        // Ensure the window size satisfies SSA's constraint: trainSize > 2 * windowSize
        var windowSize = Math.Min(12, Math.Max(1, (data.Count - 1) / 2));

        // Use ML.NET's SSA (Singular Spectrum Analysis) for time series forecasting
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

            // Determine trend and risk level
            var (trend, riskLevel) = AnalyzeForecastTrend(forecastValue, recentAverage, clusterData, precinct);

            // SSA is the best model available; apply a gentle 3% confidence decay per month
            // to honestly represent that uncertainty compounds further into the future
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
        
        if (data.Count < 12)
        {
            // Not enough data for seasonal analysis, fall back to linear
            return GenerateLinearTrendForecast(data, parameters, clusterData, precinct);
        }

        // Calculate seasonal pattern (monthly averages)
        var monthlyAverages = data
            .GroupBy(d => d.Date.Month)
            .ToDictionary(g => g.Key, g => g.Average(d => d.Value));

        // Calculate overall trend
        var recent = data.TakeLast(Math.Min(12, data.Count)).ToList();
        var n = recent.Count;
        var sumX = n * (n + 1) / 2.0;
        var sumY = recent.Sum(d => d.Value);
        var sumXY = recent.Select((d, i) => (i + 1) * d.Value).Sum();
        var sumXX = n * (n + 1) * (2 * n + 1) / 6.0;
        
        var slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
        var intercept = (sumY - slope * sumX) / n;
        
        var lastDate = data.Max(d => d.Date);
        var recentAverage = recent.Average(d => d.Value);
        
        for (int i = 0; i < parameters.Horizon; i++)
        {
            var forecastDate = lastDate.AddMonths(i + 1);
            var trendValue = intercept + slope * (n + i + 1);
            
            // Apply seasonal adjustment
            var seasonalMultiplier = monthlyAverages.GetValueOrDefault(forecastDate.Month, recentAverage) / recentAverage;
            var forecastValue = Math.Max(0, trendValue * seasonalMultiplier);
            
            // Confidence bounds widen with horizon; seasonal multiplier adds extra uncertainty
            var errorMargin = recentAverage * (parameters.IncludeSeasonality ? 0.25 : 0.2) * (1 + i * 0.08);
            
            var (trend, riskLevel) = AnalyzeForecastTrend(forecastValue, recentAverage, clusterData, precinct);

            // Seasonal model: 7% confidence decay per month
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
        int? targetPrecinct = null)
    {
        // Calculate dynamic thresholds based on historical data distribution
        var thresholds = CalculateEnhancedDynamicRiskThresholds(allClusterData);
        
        // Use precinct-specific thresholds if available, otherwise fall back to global
        var activeThresholds = targetPrecinct.HasValue && thresholds.PrecinctSpecificThresholds.ContainsKey(targetPrecinct.Value)
            ? thresholds.PrecinctSpecificThresholds[targetPrecinct.Value]
            : thresholds.GlobalThresholds;
        
        // Trend analysis (keep simple thresholds)
        var trend = forecastValue > recentAverage * 1.1 ? "increasing" :
                   forecastValue < recentAverage * 0.9 ? "decreasing" : "stable";
                   
        // Risk analysis using dynamic thresholds
        var ratio = recentAverage > 0 ? forecastValue / recentAverage : 1.0;
        var riskLevel = ratio > activeThresholds.HighMax ? "critical" :
                       ratio > activeThresholds.MediumMax ? "high" :
                       ratio > activeThresholds.LowMax ? "medium" : "low";
                       
        return (trend, riskLevel);
    }
    
    private (double lowMax, double mediumMax, double highMax) CalculateDynamicRiskThresholds(IEnumerable<ClusterGroup> clusterData)
    {
        // Legacy method - maintained for backward compatibility
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
            
            // Group by precinct/crime type to get historical patterns
            var allGroups = clusterData
                .SelectMany(c => c.ClusterItems)
                .GroupBy(item => new { Precinct = (int)item.Precinct, CrimeType = (int)item.CrimeType })
                .Where(g => g.Count() > 6) // Need reasonable sample size
                .ToList();
            
            if (allGroups.Count == 0)
            {
                warnings.Add("No sufficient data for threshold calculation, using defaults");
                return CreateThresholdResult(result, warnings);
            }
            
            // Calculate ratios for each precinct-crime type combination
            var globalRatiosWithWeights = new List<(double ratio, int weight)>();
            var precinctRatios = new Dictionary<int, List<double>>();
            
            foreach (var group in allGroups)
            {
                var precinct = group.Key.Precinct;
                var items = group.OrderBy(i => new DateTime(i.Year, i.Month, 1)).ToList();
                
                if (items.Count < 12) continue; // Need at least 12 months
                
                // Calculate recent vs historical ratios
                var recentCount = items.TakeLast(6).Count();
                var olderItems = items.Take(items.Count - 6).ToList();
                var avgOlder = olderItems.Count / Math.Max(1.0, (items.Count - 6) / 6.0); // Average per 6-month period
                
                if (avgOlder > 0)
                {
                    var ratio = recentCount / avgOlder;
                    var weight = items.Count; // Weight by amount of data
                    
                    globalRatiosWithWeights.Add((ratio, weight));
                    
                    // Store for precinct-specific calculation
                    if (!precinctRatios.ContainsKey(precinct))
                        precinctRatios[precinct] = new List<double>();
                    precinctRatios[precinct].Add(ratio);
                    
                    // Update data points counter
                    dataPointsPerPrecinct[precinct] = dataPointsPerPrecinct.GetValueOrDefault(precinct, 0) + items.Count;
                }
            }
            
            // Calculate global weighted mean thresholds
            if (globalRatiosWithWeights.Count >= 5)
            {
                var globalThresholds = CalculateWeightedPercentileThresholds(globalRatiosWithWeights);
                result["GlobalThresholds"] = new 
                {
                    LowMax = globalThresholds.lowMax,
                    MediumMax = globalThresholds.mediumMax, 
                    HighMax = globalThresholds.highMax
                };
                
                // Calculate global statistics
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
            
            // Calculate precinct-specific thresholds
            foreach (var (precinct, ratios) in precinctRatios)
            {
                if (ratios.Count >= 3) // Minimum for precinct-specific calculation
                {
                    var sortedRatios = ratios.OrderBy(r => r).ToList();
                    var precinctThreshold = CalculateSimplePercentileThresholds(sortedRatios);
                    
                    precinctThresholds[precinct] = new 
                    {
                        LowMax = precinctThreshold.lowMax,
                        MediumMax = precinctThreshold.mediumMax,
                        HighMax = precinctThreshold.highMax
                    };
                    
                    // Calculate precinct statistics
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

    /// <summary>
    /// Calculates real forecast accuracy metrics by performing a holdout validation pass.
    /// For each precinct/crime-type group with sufficient data, the last 3 months are held
    /// out as a test set. A forecast is generated on the remaining history, then MAE, RMSE,
    /// and MAPE are computed by comparing the predictions to the held-out actuals.
    /// All per-group metrics are averaged to produce the overall result.
    /// Falls back to a clearly-labelled N/A result when insufficient data is available.
    /// </summary>
    private async Task<ForecastMetrics> CalculateRealMetricsAsync(
        Dictionary<(int, int, uint), List<TimeSeriesData>> groupedData,
        ForecastParameters parameters)
    {
        const int HoldoutMonths = 3;
        const int MinTrainMonths = 6; // need at least 6 months of training data

        var allMetrics = new List<ForecastMetrics>();

        foreach (var (_, timeSeriesData) in groupedData)
        {
            if (timeSeriesData.Count < MinTrainMonths + HoldoutMonths)
                continue; // not enough data for a meaningful holdout

            var trainData = timeSeriesData.Take(timeSeriesData.Count - HoldoutMonths).ToList();
            var testData  = timeSeriesData.Skip(timeSeriesData.Count - HoldoutMonths).ToList();

            try
            {
                // Forecast exactly HoldoutMonths into the future using training data only
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
            // Return clearly zeroed metrics so the frontend knows no measurement was possible
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

    private ForecastMetrics CalculateValidationMetrics(List<TimeSeriesData> actual, List<ForecastPoint> predicted)
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

    private ForecastMetrics AverageMetrics(List<ForecastMetrics> metrics)
    {
        return new ForecastMetrics
        {
            MeanAbsoluteError = metrics.Average(m => m.MeanAbsoluteError),
            RootMeanSquareError = metrics.Average(m => m.RootMeanSquareError),
            MeanAbsolutePercentageError = metrics.Average(m => m.MeanAbsolutePercentageError),
            ModelAccuracy = metrics.Average(m => m.ModelAccuracy)
        };
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
    
    private (double lowMax, double mediumMax, double highMax) CalculateWeightedPercentileThresholds(
        List<(double ratio, int weight)> weightedRatios)
    {
        // Sort by ratio value
        var sorted = weightedRatios.OrderBy(x => x.ratio).ToList();
        var totalWeight = sorted.Sum(x => x.weight);
        
        // Calculate weighted percentiles
        var percentile25 = CalculateWeightedPercentile(sorted, totalWeight, 0.25);
        var percentile75 = CalculateWeightedPercentile(sorted, totalWeight, 0.75);
        var percentile90 = CalculateWeightedPercentile(sorted, totalWeight, 0.90);
        
        // Apply safety bounds to prevent extreme values
        var lowMax = Math.Max(0.6, Math.Min(1.0, percentile25));
        var mediumMax = Math.Max(1.0, Math.Min(1.4, percentile75));
        var highMax = Math.Max(1.3, Math.Min(2.0, percentile90));
        
        return (lowMax, mediumMax, highMax);
    }
    
    private double CalculateWeightedPercentile(
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
                // Linear interpolation between adjacent values if needed
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
    
    private (double lowMax, double mediumMax, double highMax) CalculateSimplePercentileThresholds(
        List<double> sortedRatios)
    {
        if (sortedRatios.Count == 0) return (0.8, 1.2, 1.5);
        
        var percentile25 = sortedRatios[Math.Max(0, (int)(sortedRatios.Count * 0.25))];
        var percentile75 = sortedRatios[Math.Max(0, (int)(sortedRatios.Count * 0.75))];
        var percentile90 = sortedRatios[Math.Max(0, (int)(sortedRatios.Count * 0.90))];
        
        // Apply safety bounds
        var lowMax = Math.Max(0.6, Math.Min(1.0, percentile25));
        var mediumMax = Math.Max(1.0, Math.Min(1.4, percentile75));
        var highMax = Math.Max(1.3, Math.Min(2.0, percentile90));
        
        return (lowMax, mediumMax, highMax);
    }
    
    private ThresholdCalculationResult CreateThresholdResult(
        Dictionary<string, object> result, 
        List<string> warnings)
    {
        // Convert dictionary results to strongly typed objects
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
            WeightRecentData = request.WeightRecentData
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
