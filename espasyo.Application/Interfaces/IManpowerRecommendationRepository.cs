using espasyo.Domain.Entities;

namespace espasyo.Application.Interfaces;

public interface IManpowerRecommendationRepository
{
    Task<ManpowerRecommendation> SaveAsync(ManpowerRecommendation recommendation);
    Task<IEnumerable<ManpowerRecommendation>> GetByForecastRunIdAsync(Guid forecastRunId);
    Task<IEnumerable<ManpowerRecommendation>> GetByPrecinctIdAsync(Guid precinctId);
}
