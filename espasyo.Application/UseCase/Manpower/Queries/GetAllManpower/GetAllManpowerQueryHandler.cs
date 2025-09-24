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
        IEnumerable<espasyo.Domain.Entities.Manpower> manpowers;

        if (request.Year.HasValue && request.Precinct.HasValue)
        {
            var singleManpower = await _manpowerRepository.GetByPrecinctAndYearAsync(request.Precinct.Value, request.Year.Value);
            manpowers = singleManpower != null ? new[] { singleManpower } : Array.Empty<espasyo.Domain.Entities.Manpower>();
        }
        else if (request.Year.HasValue)
        {
            manpowers = await _manpowerRepository.GetByYearAsync(request.Year.Value);
        }
        else if (request.Precinct.HasValue)
        {
            manpowers = await _manpowerRepository.GetByPrecinctAsync(request.Precinct.Value);
        }
        else
        {
            manpowers = await _manpowerRepository.GetAllManpowerAsync();
        }

        return manpowers.Select(m => new ManpowerResponse
        {
            Id = m.Id,
            PrecinctId = m.PrecinctId,
            PrecinctName = m.Precinct?.Name ?? "Unknown",
            PrecinctCode = m.Precinct?.Code ?? "N/A",
            // Map legacy enum property for backward compatibility
            Precinct = m.PrecinctEnum,
            Year = m.Year,
            AllocatedCount = m.AllocatedCount,
            MildThreshold = m.MildThreshold,
            ModerateThreshold = m.ModerateThreshold,
            CriticalThreshold = m.CriticalThreshold,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        });
    }
}