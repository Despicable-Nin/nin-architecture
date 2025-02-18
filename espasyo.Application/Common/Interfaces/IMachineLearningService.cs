namespace espasyo.Application.Common.Interfaces;

public interface IMachineLearningService
{
    IEnumerable<object> PerformKMeansClustering(IEnumerable<TrainerModel> data, string[] feature);
}