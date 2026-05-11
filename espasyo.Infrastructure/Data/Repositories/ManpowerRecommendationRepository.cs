using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories;

public class ManpowerRecommendationRepository : IManpowerRecommendationRepository
{
    private readonly ApplicationDbContext _context;

    public ManpowerRecommendationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ManpowerRecommendation> SaveAsync(ManpowerRecommendation recommendation)
    {
        _context.ManpowerRecommendations.Add(recommendation);
        await _context.SaveChangesAsync();
        return recommendation;
    }

    public async Task<IEnumerable<ManpowerRecommendation>> GetByForecastRunIdAsync(Guid forecastRunId)
    {
        return await _context.ManpowerRecommendations
            .Include(m => m.Precinct)
            .Where(m => m.ForecastRunId == forecastRunId)
            .OrderBy(m => m.Precinct.Code)
            .ThenBy(m => m.Shift)
            .ToListAsync();
    }

    public async Task<IEnumerable<ManpowerRecommendation>> GetByPrecinctIdAsync(Guid precinctId)
    {
        return await _context.ManpowerRecommendations
            .Include(m => m.Precinct)
            .Where(m => m.PrecinctId == precinctId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }
}
