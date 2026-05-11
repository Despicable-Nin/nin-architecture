using espasyo.Domain.Entities;

namespace espasyo.Application.Interfaces;

public interface IPrecinctRepository
{
    Task<IEnumerable<Precinct>> GetAllAsync();
    Task<Precinct?> GetByIdAsync(Guid id);
    Task<Precinct?> GetByBarangayAsync(Domain.Enums.Barangay barangay);
}
