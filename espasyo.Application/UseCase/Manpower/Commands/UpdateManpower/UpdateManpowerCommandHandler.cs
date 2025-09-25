using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.Manpower.Commands.UpdateManpower;

public class UpdateManpowerCommandHandler : IRequestHandler<UpdateManpowerCommand, bool>
{
    private readonly IManpowerRepository _manpowerRepository;

    public UpdateManpowerCommandHandler(IManpowerRepository manpowerRepository)
    {
        _manpowerRepository = manpowerRepository;
    }

    public async Task<bool> Handle(UpdateManpowerCommand request, CancellationToken cancellationToken)
    {
        var manpower = await _manpowerRepository.GetByIdAsync(request.Id);
        if (manpower == null)
        {
            throw new InvalidOperationException($"Manpower allocation with ID {request.Id} not found");
        }

        // Update head count
        manpower.UpdateHeadCount(request.HeadCount);
        
        var updatedManpower = await _manpowerRepository.UpdateAsync(manpower);
        return updatedManpower != null;
    }
}