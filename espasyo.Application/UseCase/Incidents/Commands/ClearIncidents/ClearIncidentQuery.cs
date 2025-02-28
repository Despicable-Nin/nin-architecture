using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.ClearIncidents;

public record ClearIncidentQuery() : IRequest<Unit>;

public class ClearIncidentQueryHandler(IIncidentRepository repository) : IRequestHandler<ClearIncidentQuery, Unit>
{
    public async Task<Unit> Handle(ClearIncidentQuery request, CancellationToken cancellationToken)
    {
        await repository.RemoveAllIncidentsAsync();
        
        return Unit.Value;
    }
}