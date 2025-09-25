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
        // Check if manpower allocation already exists for this precinct
        var exists = await _manpowerRepository.ExistsByPrecinctIdAsync(request.PrecinctId);
        if (exists)
        {
            throw new InvalidOperationException($"Manpower allocation already exists for precinct {request.PrecinctId}");
        }

        var manpower = new DomainEntities.Manpower(
            request.PrecinctId,
            request.HeadCount
        );

        var createdManpower = await _manpowerRepository.CreateAsync(manpower);
        return createdManpower.Id;
    }
}