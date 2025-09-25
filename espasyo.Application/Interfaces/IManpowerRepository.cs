using espasyo.Domain.Entities;

namespace espasyo.Application.Interfaces;

public interface IManpowerRepository
{
    Task<IEnumerable<Manpower>> GetAllManpowerAsync();
    Task<Manpower?> GetByPrecinctIdAsync(Guid precinctId);
    Task<Manpower?> GetByIdAsync(Guid id);
    Task<Manpower> CreateAsync(Manpower manpower);
    Task<Manpower?> UpdateAsync(Manpower manpower);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsByPrecinctIdAsync(Guid precinctId);
    
    // Analysis methods
    Task<Dictionary<Guid, int>> GetTotalManpowerByPrecinctAsync();
    Task<IEnumerable<Manpower>> GetManpowerWithShortageAsync(Dictionary<Guid, int> requiredManpower);
}
