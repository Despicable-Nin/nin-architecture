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
        Incident incident = new (request.CaseId,
            request.Address,
            (SeverityEnum)request.Severity,
            (CrimeTypeEnum)request.CrimeType,
            (MotiveEnum)request.Motive,
            (MuntinlupaPoliceDistrictEnum)request.PoliceDistrict,
            (WeatherConditionEnum)request.Weather,
            request.OtherMotive,
            request.TimeStamp
        );
        
        var created = await incidentRepository.CreateIncidentAsync(incident);
        
        if(created == null) throw new Exception("Error creating incident");

        return created.Id;
    }
}