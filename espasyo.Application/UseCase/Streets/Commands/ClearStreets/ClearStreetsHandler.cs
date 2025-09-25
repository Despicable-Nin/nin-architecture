using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.Streets.Commands.ClearStreets;

public class ClearStreetsCommand : IRequest
{
    
}

public class ClearStreetsHandler : IRequestHandler<ClearStreetsCommand>
{
    private readonly IStreetRepository _repository;
    public async Task Handle(ClearStreetsCommand request, CancellationToken cancellationToken)
    {
        
    }
}