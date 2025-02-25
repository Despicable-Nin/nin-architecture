using espasyo.Application.Common.Models.ML;

namespace espasyo.Application.Interfaces;

public interface IMachineLearningService
{
    IEnumerable<ClusteredModel> PerformKMeansClustering(IEnumerable<TrainerModel> data,string[]? features, int numberOfClusters = 3, int numberOrRuns = 1);

    GroupedClusterResponse PerformKMeansAndGetGroupedClusters(IEnumerable<TrainerModel> data, string[]? features,
        int numberOfClusters = 3, int runs = 10);
}