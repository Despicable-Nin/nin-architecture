using espasyo.Domain.Entities;

namespace espasyo.Application.Interfaces;

public interface IAnalysisRunRepository
{
    Task<AnalysisRun> SaveAsync(AnalysisRun run);
    Task<AnalysisRun?> GetByIdAsync(Guid id);
    Task<IEnumerable<AnalysisRun>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<bool> DeleteAsync(Guid id);
}
