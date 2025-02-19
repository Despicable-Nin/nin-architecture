using espasyo.Application.Common.Interfaces;
using espasyo.Application.Common.Models.ML;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace espasyo.Infrastructure.MachineLearning;

public class MachineLearningService(
    MLContext mlContext,
    ILogger<MachineLearningService> logger
) : IMachineLearningService
{
    public IEnumerable<ClusteredModel> PerformKMeansClustering(IEnumerable<TrainerModel> data)
    {
        try
        {
            string[] features = ["CrimeType", "Latitude", "Longitude", "Severity", "PoliceDistrict", "Weather", "CrimeMotive"
            ];

            logger.LogInformation("Performing KMeansClustering. {Data} {Feature}", data, features);

            var schema = SchemaDefinition.Create(typeof(TrainerModel));
            var dataView = mlContext.Data.LoadFromEnumerable(data, schema);

            var inputOutputColumnPairs = features.Select(x => new InputOutputColumnPair($"{x}_Single", x)).ToArray();
            var inputColumnNames = inputOutputColumnPairs.Select(x => x.OutputColumnName).ToArray();

            var pipeline = mlContext
                .Transforms.Conversion.ConvertType(inputOutputColumnPairs, DataKind.Single)
                .Append(mlContext.Transforms.Concatenate("Features", inputColumnNames))
                .Append(mlContext.Clustering.Trainers.KMeans(numberOfClusters: 3));

            var model = pipeline.Fit(dataView);
            var predictions = model.Transform(dataView);
            
            var centroidModelParameters = model.LastTransformer.Model;

            // Get Centroids using `GetClusterCentroids`
            VBuffer<float>[] centroids = null;
            centroidModelParameters.GetClusterCentroids(ref centroids, k: out var numFeatures);

            // Output Centroids
            // Console.WriteLine("Centroids:");
            // for (var i = 0; i < centroids.Length; i++)
            // {
            //     var centroidArray = centroids[i].DenseValues().ToArray();
            //     Console.WriteLine($"Cluster {i} Centroid: {string.Join(", ", centroidArray)}");
            // }

            var clusterPredictions = mlContext
                .Data
                .CreateEnumerable<ClusteredModel>(predictions, reuseRowObject: false, true)
                .ToList();

            return clusterPredictions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while performing KMeans clustering.");
            throw;
        }
    }
}