using espasyo.Domain.Entities;

namespace espasyo.Application.Interfaces;

public interface IForecastRepository
{
    Task<ForecastRun> SaveForecastRunAsync(ForecastRun forecastRun);
    Task SaveForecastResultsAsync(IEnumerable<ForecastResult> results);
    Task SaveSpatialForecastResultsAsync(IEnumerable<SpatialForecastResult> results);
    Task SaveSeasonalDecompositionResultsAsync(IEnumerable<SeasonalDecompositionResult> results);
    Task<ForecastRun?> GetForecastRunByIdAsync(Guid id);
    Task<IEnumerable<ForecastRun>> GetForecastRunsAsync(int page = 1, int pageSize = 20);
    Task<IEnumerable<ForecastResult>> GetForecastResultsAsync(Guid forecastRunId);
    Task<bool> DeleteForecastRunAsync(Guid id);
}
