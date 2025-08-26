using espasyo.Application.Common.Models.ML;

namespace espasyo.Application.Interfaces;

public interface IMachineLearningService
{
    IEnumerable<ClusteredModel> PerformKMeansClustering(IEnumerable<TrainerModel> data,string[]? features, int numberOfClusters = 3, int numberOrRuns = 1);

    GroupedClusterResponse PerformKMeansAndGetGroupedClusters(IEnumerable<TrainerModel> data, string[]? features,
        int numberOfClusters = 3, int runs = 10);

    // Statistical Forecasting Methods
    Task<ForecastResponse> GenerateStatisticalForecast(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters);
    Task<ForecastValidationResult> ValidateForecastModel(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters);
    Task<DataQualityAssessment> AssessDataQuality(IEnumerable<ClusterGroup> clusterData);
}
