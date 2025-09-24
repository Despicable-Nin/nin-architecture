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

        return new ManpowerResponse
        {
            Id = manpower.Id,
            PrecinctId = manpower.PrecinctId,
            PrecinctName = manpower.Precinct?.Name ?? "Unknown",
            PrecinctCode = manpower.Precinct?.Code ?? "N/A",
            // Map legacy enum property for backward compatibility
            Precinct = manpower.PrecinctEnum,
            Year = manpower.Year,
            AllocatedCount = manpower.AllocatedCount,
            MildThreshold = manpower.MildThreshold,
            ModerateThreshold = manpower.ModerateThreshold,
            CriticalThreshold = manpower.CriticalThreshold,
            CreatedAt = manpower.CreatedAt,
            UpdatedAt = manpower.UpdatedAt
        };
    }
}