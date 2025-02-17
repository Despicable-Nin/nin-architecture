using espasyo.Application.Common.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using espasyo.Domain.Events;
using MediatR;

namespace espasyo.Application.Incidents.Commands.CreateIncident;

public record CreateIncidentCommand : IRequest<Guid>
{
    public string? CaseId { get; init; }
    public string? Address { get; init; }
    public int Severity { get; init; }
    public int CrimeType { get;  init; }
    public int Motive { get;  init; }
    public int PoliceDistrict { get;  init; }
    public string? OtherMotive { get; init; }
    public int Weather { get;  init; }
    public DateTimeOffset? TimeStamp { get; init; }
}


public class CreateIncidentCommandHandler(
    IIncidentRepository incidentRepository,
    ILogger<CreateIncidentCommandHandler> logger)
    : IRequestHandler<CreateIncidentCommand, Guid>
{
    public async Task<Guid> Handle(CreateIncidentCommand request, CancellationToken cancellationToken)
    {
        var incident = new Incident(request.CaseId,
            request.Address,
            (SeverityEnum)request.Severity,
            (CrimeTypeEnum)request.CrimeType,
            (MotiveEnum)request.Motive,
            (MuntinlupaPoliceDistrictEnum)request.PoliceDistrict,
            (WeatherConditionEnum)request.Weather,
            request.OtherMotive,
            request.TimeStamp
        );

        var id = await incidentRepository.CreateIncidentAsync(incident);
        if (!id.HasValue) return Guid.Empty;
        
        logger.LogInformation($"Incident with id {id.Value} created");
        incident.AddDomainEvent(new IncidentCreatedEvent(incident.CaseId, incident.Address));
        return id.Value;
    }
}