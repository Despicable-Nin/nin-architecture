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
                features = ["CrimeType", "Severity", "PoliceDistrict", "Weather", "CrimeMotive"];
            }

            string message = $"Performing KMeansClustering with features: {features}";
            logger.LogInformation(message);
            var trainerModels = data as TrainerModel[] ?? data.ToArray();
            logger.LogInformation("Total input records: {Count}", trainerModels.Length);

            var dataView = mlContext.Data.LoadFromEnumerable(trainerModels);

            // Initialize a pipeline.
            IEstimator<ITransformer>? pipeline = null;

            // Dynamically append transformations for each feature.
            pipeline = AppendPipeline(mlContext, features, pipeline);

            // Concatenate all selected encoded features into a single "Features" column.
            var selectedFeatureColumns = features.Select(f => IsCategoricalFeature(f) || IsFloat(f) ? $"{f}Encoded" : f).ToArray();
            pipeline = pipeline.Append(mlContext.Transforms.Concatenate("Features", selectedFeatureColumns))
                                .Append(mlContext.Transforms.NormalizeMeanVariance("Features"))
                                .Append(mlContext.Clustering.Trainers.KMeans(numberOfClusters: numberOfClusters));

            double bestScore = double.MaxValue;
            ITransformer? bestModel = null;

            // Run multiple iterations to select the best model.
            FindBestKMeansModel(mlContext, runs, dataView, pipeline, ref bestScore, ref bestModel);

            // Transform the data using the best model.
            var finalPredictions = bestModel!.Transform(dataView);
            var clusterPredictions = mlContext.Data
                .CreateEnumerable<ClusteredModel>(finalPredictions, reuseRowObject: false, ignoreMissingColumns: true)
                .ToList();

            // Log the count of records after prediction.
            logger.LogInformation("Total records after prediction: {Count}", clusterPredictions.Count);

            // Optionally, print the predictions.
#if DEBUG
            foreach (var prediction in clusterPredictions)
            {
                var cluster = trainerModels.FirstOrDefault(x => x.CaseId == prediction.CaseId);
                Console.WriteLine($"CaseId: {prediction.CaseId}, " +
                                    $"CrimeType: {cluster!.CrimeType.ToString()}, " +
                                    $"Severity: {cluster.Severity.ToString()}, " +
                                    $"Motive: {cluster.CrimeMotive.ToString()}, " +
                                    $"Weather: {cluster.Weather.ToString()}, " +
                                    $"Precinct: {cluster.PoliceDistrict.ToString()}, " +
                                    $"Assigned Cluster: {prediction.ClusterId}");
            }
#endif
            
            var centroids = ExtractCentroids(bestModel, numberOfClusters, trainerModels);

            // Group the predictions by cluster ID and map to the desired output structure.
            var groupedClusters = clusterPredictions
                .GroupBy(x => x.ClusterId)
                .Select(g => new ClusterGroup()
                {
                    ClusterId = g.Key,
                    // Filters property is now on the same level as ClusterId; currently left empty.
                 //   Centroids = centroids.TryGetValue(g.Key, out float[]? value) ? value : [0, 0], // Assign centroid
                    ClusterItems = g.Select(item => new ClusterItem
                    {
                        CaseId = item.CaseId,
                        // Assuming ClusteredModel has Latitude and Longitude properties.
                        Latitude = item.Latitude,
                        Longitude = item.Longitude
                    }).ToList()
                })
                .ToList();

            return new GroupedClusterResponse()
            {
                Filters = [],
                ClusterGroups = groupedClusters.OrderBy(x => x.ClusterId).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while performing KMeans clustering with features: {Features}, number of clusters: {NumberOfClusters}, runs: {Runs}.", features, numberOfClusters, runs);
            throw;
        }
    }

    private static void FindBestKMeansModel(MLContext mlContext, int runs, IDataView dataView, IEstimator<ITransformer>? pipeline, ref double bestScore, ref ITransformer? bestModel)
    {
        for (int i = 0; i < runs; i++)
        {
            ITransformer? model = (pipeline?.Fit(dataView)) ?? throw new InvalidOperationException("Pipeline fitting resulted in a null model.");
            IDataView predictions = model.Transform(dataView);

            ClusteringMetrics metrics = mlContext.Clustering.Evaluate(predictions, scoreColumnName: "Score");

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
        foreach (string feature in features)
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

    private static Dictionary<uint, float[]> ExtractCentroids(ITransformer model, int numberOfClusters, TrainerModel[] data)
    {
        var centroids = new Dictionary<uint, float[]>();

        if (model is TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> kMeansModel)
        {
            var kMeansParameters = kMeansModel.LastTransformer.Model;

            if (kMeansParameters != null)
            {
                VBuffer<float>[] rawCentroids = null;
                kMeansParameters.GetClusterCentroids(ref rawCentroids, out int k);

                // Get min/max for latitude and longitude
                float minLat = (float)data.Min(x => x.Latitude);
                float maxLat = (float)data.Max(x => x.Latitude);
                float minLon = (float)data.Min(x => x.Longitude);
                float maxLon = (float)data.Max(x => x.Longitude);

                for (uint i = 0; i < numberOfClusters; i++)
                {
                    var centroid = rawCentroids[i].GetValues().ToArray();

                    // Reverse normalization (assuming NormalizeMinMax scaling was used)
                    float centroidLat = minLat + centroid[0] * (maxLat - minLat);
                    float centroidLon = minLon + centroid[1] * (maxLon - minLon);

                    centroids[i] = new float[] { centroidLat, centroidLon };
                }
            }
        }

        return centroids;
    }




    private static bool IsCategoricalFeature(string featureName)
    {
        // Define logic to determine if a feature is categorical
        var categoricalFeatures = new HashSet<string> { "CrimeType", "PoliceDistrict", "Weather", "CrimeMotive", "Severity" };
        return categoricalFeatures.Contains(featureName);
    }

    private static bool IsFloat(string featureName)
    {
        string[] floatFeatures = { "Longitude", "Latitude" };
        return floatFeatures.Contains(featureName);
    }


    


}
