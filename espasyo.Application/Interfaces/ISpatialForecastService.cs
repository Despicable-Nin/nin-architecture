using espasyo.Application.Common.Models.ML;

namespace espasyo.Application.Interfaces;

public interface ISpatialForecastService
{
    Task<List<SpatialForecastRow>> DistributeForecast(
        IEnumerable<ClusterGroup> clusterData,
        ForecastParameters parameters,
        List<ForecastSeries> temporalSeries);
}
