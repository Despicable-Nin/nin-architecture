using espasyo.Application.Common.Models.ML;

namespace espasyo.Application.Common.Interfaces;

public interface IMachineLearningService
{
    IEnumerable<ClusteredModel> PerformKMeansClustering(IEnumerable<TrainerModel> data);
}