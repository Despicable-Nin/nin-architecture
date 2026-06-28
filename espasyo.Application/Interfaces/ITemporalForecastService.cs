using espasyo.Application.Common.Models.ML;

namespace espasyo.Application.Interfaces;

public interface ITemporalForecastService
{
    Task<ForecastResponse> GenerateForecast(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters);
    Task<ForecastValidationResult> ValidateForecastModel(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters);
}
