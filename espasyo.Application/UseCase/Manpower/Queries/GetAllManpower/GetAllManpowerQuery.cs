using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.UseCase.Manpower.Queries.GetAllManpower;

public class GetAllManpowerQuery : IRequest<IEnumerable<ManpowerResponse>>
{
    // No filters needed for simplified version - get all manpower
}

public class ManpowerResponse
{
    public Guid Id { get; set; }
    public Guid PrecinctId { get; set; }
    public string PrecinctName { get; set; } = string.Empty;
    public string PrecinctCode { get; set; } = string.Empty;
    public ShiftEnum Shift { get; set; }
    public string ShiftDisplayName { get; set; } = string.Empty;
    public int HeadCount { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    
    // Calculated properties for shortage/overage analysis
    public int? RequiredCount { get; set; }
    public int? Variance { get; set; } // Positive = overage, Negative = shortage
    public string Status { get; set; } = string.Empty; // "Adequate", "Shortage", "Overage"
}
