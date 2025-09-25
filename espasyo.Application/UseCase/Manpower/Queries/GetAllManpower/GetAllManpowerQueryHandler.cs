using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.Manpower.Queries.GetAllManpower;

public class GetAllManpowerQueryHandler : IRequestHandler<GetAllManpowerQuery, IEnumerable<ManpowerResponse>>
{
    private readonly IManpowerRepository _manpowerRepository;

    public GetAllManpowerQueryHandler(IManpowerRepository manpowerRepository)
    {
        _manpowerRepository = manpowerRepository;
    }

    public async Task<IEnumerable<ManpowerResponse>> Handle(GetAllManpowerQuery request, CancellationToken cancellationToken)
    {
        var manpowers = await _manpowerRepository.GetAllManpowerAsync();
        
        // For demonstration purposes, let's assume a standard requirement of 25 officers per precinct
        // In a real system, this would come from business rules or configuration
        const int standardRequirement = 25;

        return manpowers.Select(m => 
        {
            var variance = m.CalculateVariance(standardRequirement);
            var status = variance == 0 ? "Adequate" : 
                        variance > 0 ? "Overage" : "Shortage";
                        
            return new ManpowerResponse
            {
                Id = m.Id,
                PrecinctId = m.PrecinctId,
                PrecinctName = m.Precinct?.Name ?? "Unknown",
                PrecinctCode = m.Precinct?.Code ?? "N/A",
                Shift = m.Shift,
                ShiftDisplayName = m.GetShiftDisplayName(),
                HeadCount = m.HeadCount,
                LastUpdated = m.LastUpdated,
                RequiredCount = standardRequirement,
                Variance = variance,
                Status = status
            };
        });
    }
}