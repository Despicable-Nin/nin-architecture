using espasyo.Domain.Entities;

namespace espasyo.Application.Common.Interfaces;

public interface IIncidentRepository
{
    Task<List<Incident>> GetAllIncidentsAsync();
    Task<(IEnumerable<Incident>,int count)> GetPaginatedIncidentsAsync(int pageNumber, int pageSize);
    Task<Incident?> GetIncidentByCaseIdAsync(string caseId);
    Task<Incident?> GetIncidentByIdAsync(Guid id);
    Task<Guid> CreateIncidentAsync(Incident incident);
}