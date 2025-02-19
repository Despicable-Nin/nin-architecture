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
        string[] features = [ "CrimeType", "Latitude", "Longitude", "Severity", "PoliceDistrict", "Weather" ];
        
        logger.LogInformation("Performing KMeansClustering. {Data} {Feature}", data, features);

        var dataView = mlContext.Data.LoadFromEnumerable(data);

        var inputOutputColumnPairs = features.Select(x => new InputOutputColumnPair($"{x}_Single", x)).ToArray();

        var inputColumnNames = inputOutputColumnPairs.Select(x => x.OutputColumnName).ToArray();

        var pipeline = mlContext
            .Transforms.Conversion.ConvertType(inputOutputColumnPairs, DataKind.Single)
            .Append(mlContext.Transforms.Concatenate("Features", inputColumnNames))
            .Append(mlContext.Clustering.Trainers.KMeans("Features", numberOfClusters: 3));

        var model = pipeline.Fit(dataView);
        
        var predictions = model.Transform(dataView);
        
        model.Transform(dataView);

        // Extract cluster assignments and original data
        var clusterPredictions = mlContext
            .Data
            .CreateEnumerable<ClusteredModel>(predictions, reuseRowObject: false)
            .ToList();

        return clusterPredictions;

    }
}