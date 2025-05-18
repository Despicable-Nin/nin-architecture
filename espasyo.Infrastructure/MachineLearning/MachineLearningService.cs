using Microsoft.ML;
using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

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
            // Default to all features if none provided
            if (features == null || features.Length == 0)
            {
                features = ["CrimeType", "Severity", "PoliceDistrict", "Weather", "CrimeMotive"];
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
        try
        {
            // Default to all features if none provided.
            if (features == null || features.Length == 0)
            {
                features = ["CrimeType", "Severity", "PoliceDistrict", "Weather", "Motive"];
            }

            logger.LogInformation("Performing KMeansClustering with features: {Features}", features);
            // Materialize data once for reuse
            var trainerModels = data as TrainerModel[] ?? data.ToArray();
            logger.LogInformation("Total input records: {Count}", trainerModels.Length);

            var dataView = mlContext.Data.LoadFromEnumerable(trainerModels);

            // Build pipeline only once
            IEstimator<ITransformer>? pipeline = AppendPipeline(mlContext, features, null);
            var selectedFeatureColumns = features.Select(f => IsCategoricalFeature(f) || IsFloat(f) ? $"{f}Encoded" : f).ToArray();
            pipeline = pipeline.Append(mlContext.Transforms.Concatenate("Features", selectedFeatureColumns))
                               .Append(mlContext.Transforms.NormalizeMeanVariance("Features"))
                               .Append(mlContext.Clustering.Trainers.KMeans(numberOfClusters: numberOfClusters));

            double bestScore = double.MaxValue;
            ITransformer? bestModel = null;

            // Run multiple iterations to select the best model.
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

            // Transform the data using the best model.
            var finalPredictions = bestModel!.Transform(dataView);
            // Use a dictionary for fast CaseId lookup
            var caseIdToTrainer = trainerModels.ToDictionary(x => x.CaseId ?? string.Empty);
            var clusterPredictions = mlContext.Data
                .CreateEnumerable<ClusteredModel>(finalPredictions, reuseRowObject: false, ignoreMissingColumns: true)
                .ToList();

            logger.LogInformation("Total records after prediction: {Count}", clusterPredictions.Count);

//#if DEBUG
//            foreach (var prediction in clusterPredictions)
//            {
//                if (caseIdToTrainer.TryGetValue(prediction.CaseId, out var cluster))
//                {
//                    Console.WriteLine($"CaseId: {prediction.CaseId}, " +
//                                      $"CrimeType: {cluster.CrimeType}, " +
//                                      $"Severity: {cluster.Severity}, " +
//                                      $"Motive: {cluster.Motive}, " +
//                                      $"Weather: {cluster.Weather}, " +
//                                      $"Precinct: {cluster.PoliceDistrict}, " +
//                                      $"Assigned Cluster: {prediction.ClusterId}");
//                }
//            }
//#endif

            // Group predictions by clusterId efficiently
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
                        TimeOfDay = GetTimeOfDay(trainer.TimeStampUnix)
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

            return new GroupedClusterResponse
            {
                Filters = [],
                ClusterGroups = clusterGroups
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while performing KMeans clustering with features: {Features}, number of clusters: {NumberOfClusters}, runs: {Runs}.", features, numberOfClusters, runs);
            throw;
        }
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
}
