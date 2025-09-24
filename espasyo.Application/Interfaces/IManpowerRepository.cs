using espasyo.Domain.Entities;
using espasyo.Domain.Enums;

namespace espasyo.Application.Interfaces;

public interface IManpowerRepository
{
    Task<IEnumerable<Manpower>> GetAllManpowerAsync();
    Task<IEnumerable<Manpower>> GetByYearAsync(int year);
    Task<Manpower?> GetByPrecinctAndYearAsync(Barangay precinct, int year);
    Task<IEnumerable<Manpower>> GetByPrecinctAsync(Barangay precinct);
    Task<Manpower?> GetByIdAsync(Guid id);
    Task<Manpower> CreateAsync(Manpower manpower);
    Task<Manpower?> UpdateAsync(Manpower manpower);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Barangay precinct, int year);
    
    // Analysis methods
    Task<Dictionary<Barangay, int>> GetTotalManpowerByPrecinctAsync(int year);
    Task<IEnumerable<Manpower>> GetManpowerRequiringAdjustmentAsync(int year, Dictionary<Barangay, int> predictedCaseCounts);
}