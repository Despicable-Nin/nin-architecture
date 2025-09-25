using espasyo.Domain.Entities;
using espasyo.Domain.Enums;

namespace espasyo.Application.Interfaces;

public interface IManpowerRepository
{
    Task<IEnumerable<Manpower>> GetAllManpowerAsync();
    Task<Manpower?> GetByPrecinctIdAsync(Guid precinctId);
    Task<IEnumerable<Manpower>> GetByPrecinctIdAllShiftsAsync(Guid precinctId);
    Task<Manpower?> GetByPrecinctIdAndShiftAsync(Guid precinctId, ShiftEnum shift);
    Task<Manpower?> GetByIdAsync(Guid id);
    Task<Manpower> CreateAsync(Manpower manpower);
    Task<Manpower?> UpdateAsync(Manpower manpower);
    Task<Manpower> UpsertAsync(Guid precinctId, ShiftEnum shift, int headCount);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsByPrecinctIdAsync(Guid precinctId);
    Task<bool> ExistsByPrecinctIdAndShiftAsync(Guid precinctId, ShiftEnum shift);
    
    // Analysis methods
    Task<Dictionary<Guid, int>> GetTotalManpowerByPrecinctAsync();
    Task<IEnumerable<Manpower>> GetManpowerWithShortageAsync(Dictionary<Guid, int> requiredManpower);
}
