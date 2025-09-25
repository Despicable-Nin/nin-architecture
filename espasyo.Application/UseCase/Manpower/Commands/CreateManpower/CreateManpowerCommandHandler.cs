using espasyo.Application.Interfaces;
using MediatR;
using DomainEntities = espasyo.Domain.Entities;

namespace espasyo.Application.UseCase.Manpower.Commands.CreateManpower;

public class UpsertManpowerCommandHandler : IRequestHandler<UpsertManpowerCommand, Guid>
{
    private readonly IManpowerRepository _manpowerRepository;

    public UpsertManpowerCommandHandler(IManpowerRepository manpowerRepository)
    {
        _manpowerRepository = manpowerRepository;
    }

    public async Task<Guid> Handle(UpsertManpowerCommand request, CancellationToken cancellationToken)
    {
        var manpower = await _manpowerRepository.UpsertAsync(
            request.PrecinctId,
            request.Shift,
            request.HeadCount
        );

        return manpower.Id;
    }
}

// Keep the old handler for backward compatibility if needed
[Obsolete("Use UpsertManpowerCommandHandler instead")]
public class CreateManpowerCommandHandler : IRequestHandler<CreateManpowerCommand, Guid>
{
    private readonly UpsertManpowerCommandHandler _upsertHandler;

    public CreateManpowerCommandHandler(IManpowerRepository manpowerRepository)
    {
        _upsertHandler = new UpsertManpowerCommandHandler(manpowerRepository);
    }

    public async Task<Guid> Handle(CreateManpowerCommand request, CancellationToken cancellationToken)
    {
        return await _upsertHandler.Handle(request, cancellationToken);
    }
}
