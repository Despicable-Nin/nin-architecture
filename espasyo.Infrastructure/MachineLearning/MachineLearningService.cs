using espasyo.Application.Common;
using espasyo.Application.Common.Interfaces;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace espasyo.Infrastructure.MachineLearning;

public class MachineLearningService(
    MLContext mlContext,
    ILogger<MachineLearningService> logger
) : IMachineLearningService
{

    public IEnumerable<object> PerformKMeansClustering(IEnumerable<TrainerModel> data, string[] features)
    {
        logger.LogInformation("Performing KMeansClustering. {Data} {Feature}", data, features);

        var schema = SchemaDefinition.Create(typeof(TrainerModel));

        var dataView = mlContext.Data.LoadFromEnumerable(data);

        var inputOutputColumnPairs = features.Select(x => new InputOutputColumnPair($"{x}_Single", x)).ToArray();

        var inputColumnNames = inputOutputColumnPairs.Select(x => x.OutputColumnName).ToArray();

        var pipeline = mlContext.Transforms.Conversion.ConvertType(inputOutputColumnPairs, DataKind.Single)
            .Append(mlContext.Transforms.Concatenate("Features", inputColumnNames))
            .Append(mlContext.Clustering.Trainers.KMeans("Features", numberOfClusters: 3));
        
        var model = pipeline.Fit(dataView);
        var predictions = model.Transform(dataView);

        var clusterPredictions = mlContext.Data.CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false).ToList();

        // Group the data by clusters and extract lat/long for each crime
        var groupedClusters = clusterPredictions
            .GroupBy(p => p.PredictedClusterId)
            .Select(group => new ClusterResult
            {
                ClusterId = group.Key,
              //  Crimes = group.Select(p => new { p.Latitude, p.Longitude }).ToList()
            })
            .ToList();
        
        // Print cluster assignments and lat/long
        // Console.WriteLine("Cluster Assignments:");
        // foreach (var cluster in groupedClusters)
        // {
        //     Console.WriteLine($"Cluster {cluster.ClusterId}:");
        //     foreach (var crime in cluster.)
        //     {
        //         Console.WriteLine($"Latitude: {crime.Latitude}, Longitude: {crime.Longitude}");
        //     }
        // }

        return   groupedClusters;
        
    }
    
    public class ClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId { get; set; }

        [ColumnName("CaseID")]
        public string CaseId { get; set; }
        [ColumnName("Latitude")]
        public string Latitude { get; set; }
        [ColumnName("Longitude")]
        public string Longitude { get; set; }
    }
    
    public class ClusterResult
    {
        public uint ClusterId { get; set; }
        public string Latitude { get; set; } = string.Empty;
        public string Longitude { get; set; } = string.Empty;
        public int Count { get; internal set; }
    }


}