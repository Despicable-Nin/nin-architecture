using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.UseCase.Manpower.Queries.GetAllManpower;

public class GetAllManpowerQuery : IRequest<IEnumerable<ManpowerResponse>>
{
    public int? Year { get; set; }
    public Barangay? Precinct { get; set; }
}

public class ManpowerResponse
{
    public Guid Id { get; set; }
    public Guid PrecinctId { get; set; }
    public string PrecinctName { get; set; } = string.Empty;
    public string PrecinctCode { get; set; } = string.Empty;
    // Legacy enum property for backward compatibility
    public Barangay Precinct { get; set; }
    public int Year { get; set; }
    public int AllocatedCount { get; set; }
    public int MildThreshold { get; set; }
    public int ModerateThreshold { get; set; }
    public int CriticalThreshold { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
