using espasyo.Application.Interfaces;
using MediatR;
using DomainEntities = espasyo.Domain.Entities;

namespace espasyo.Application.UseCase.Manpower.Commands.CreateManpower;

public class CreateManpowerCommandHandler : IRequestHandler<CreateManpowerCommand, Guid>
{
    private readonly IManpowerRepository _manpowerRepository;

    public CreateManpowerCommandHandler(IManpowerRepository manpowerRepository)
    {
        _manpowerRepository = manpowerRepository;
    }

    public async Task<Guid> Handle(CreateManpowerCommand request, CancellationToken cancellationToken)
    {
        // Check if manpower allocation already exists for this precinct and year
        var exists = await _manpowerRepository.ExistsAsync(request.Precinct, request.Year);
        if (exists)
        {
            throw new InvalidOperationException($"Manpower allocation already exists for {request.Precinct} in {request.Year}");
        }

        var manpower = new DomainEntities.Manpower(
            request.Precinct,
            request.Year,
            request.AllocatedCount,
            request.MildThreshold,
            request.ModerateThreshold,
            request.CriticalThreshold
        );

        var createdManpower = await _manpowerRepository.CreateAsync(manpower);
        return createdManpower.Id;
    }
}