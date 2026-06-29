using espasyo.Application.Common.Models.ML;

namespace espasyo.Application.Interfaces;

public interface ISeasonalForecastService
{
    Task<List<SeasonalPredictionRow>> PredictSeasonal(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters);
}
