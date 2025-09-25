using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.Streets.Commands.ClearStreets;

public class ClearStreetsCommand : IRequest
{
    
}

public class ClearStreetsHandler : IRequestHandler<ClearStreetsCommand>
{
    private readonly IStreetRepository _repository;
    
    public ClearStreetsHandler(IStreetRepository repository)
    {
        _repository = repository;
    }
    
    public async Task Handle(ClearStreetsCommand request, CancellationToken cancellationToken)
    {
        // Clear all streets - implementation will depend on your repository method
        // await _repository.ClearAllAsync();
    }
}
