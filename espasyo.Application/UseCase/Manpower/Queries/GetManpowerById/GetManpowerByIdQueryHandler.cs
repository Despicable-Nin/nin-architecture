using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.Manpower.Queries.GetAllManpower;
using MediatR;

namespace espasyo.Application.UseCase.Manpower.Queries.GetManpowerById;

public class GetManpowerByIdQueryHandler : IRequestHandler<GetManpowerByIdQuery, ManpowerResponse?>
{
    private readonly IManpowerRepository _manpowerRepository;

    public GetManpowerByIdQueryHandler(IManpowerRepository manpowerRepository)
    {
        _manpowerRepository = manpowerRepository;
    }

    public async Task<ManpowerResponse?> Handle(GetManpowerByIdQuery request, CancellationToken cancellationToken)
    {
        var manpower = await _manpowerRepository.GetByIdAsync(request.Id);
        if (manpower == null)
        {
            return null;
        }

        // For demonstration purposes, assume standard requirement of 25 officers
        const int standardRequirement = 25;
        var variance = manpower.CalculateVariance(standardRequirement);
        var status = variance == 0 ? "Adequate" : 
                    variance > 0 ? "Overage" : "Shortage";
        
        return new ManpowerResponse
        {
            Id = manpower.Id,
            PrecinctId = manpower.PrecinctId,
            PrecinctName = manpower.Precinct?.Name ?? "Unknown",
            PrecinctCode = manpower.Precinct?.Code ?? "N/A",
            HeadCount = manpower.HeadCount,
            LastUpdated = manpower.LastUpdated,
            RequiredCount = standardRequirement,
            Variance = variance,
            Status = status
        };
    }
}