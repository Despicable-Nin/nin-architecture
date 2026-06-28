using espasyo.Application.Common.Models.ML;

namespace espasyo.Application.Interfaces;

public interface ISeasonalForecastService
{
    Task<List<DecompositionRow>> Decompose(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters);
}
