using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using FluentValidation;
using MediatR;

namespace espasyo.Application.Incidents.Commands.CreateIncident;

public record CreateIncidentCommand : IRequest<Guid>
{
    public string? CaseId { get; init; }
    public string? Address { get; init; }
    public int Severity { get; init; }
    public int CrimeType { get;  init; }
    public int Motive { get;  init; }
    public int Precinct { get;  init; }
    public string? AdditionalInfo { get; init; }
    public int Weather { get;  init; }
    public DateTimeOffset? TimeStamp { get; init; }
}

public class CreateIncidentCommandValidator : AbstractValidator<CreateIncidentCommand>
{
    public CreateIncidentCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Severity).NotEmpty();
        RuleFor(x => x.TimeStamp).NotEmpty();
        RuleFor(x => x).NotEmpty();
    }
}


public class CreateIncidentCommandHandler(
    IIncidentRepository incidentRepository,
    IGeocodeService geocodeService,
    ILogger<CreateIncidentCommandHandler> logger)
    : IRequestHandler<CreateIncidentCommand, Guid>
{
    public async Task<Guid> Handle(CreateIncidentCommand request, CancellationToken cancellationToken)
    {
        
        logger.LogInformation("Creating Incident. Request:{Request}", request);

        Incident incident = new (
            request.CaseId,
            request.Address,
            (SeverityEnum)request.Severity,
            (CrimeTypeEnum)request.CrimeType,
            (MotiveEnum)request.Motive,
            (Barangay)request.Precinct,
            (WeatherConditionEnum)request.Weather,
            request.AdditionalInfo,
            request.TimeStamp
        );
        
        logger.LogInformation("Getting nominatim api . . .");
        var latLong = await geocodeService.GetLatLongAsync(request.Address!);

        logger.LogInformation("Updating latlong . . .");
        incident.ChangeLatLong(latLong.Latitude, latLong.Longitude);

        logger.LogInformation("Updating new address . . .");
        incident.SanitizeAddress(latLong.NewAddress);
        
        var created = await incidentRepository.CreateIncidentAsync(incident);
        
        if(created == null) throw new Exception("Error creating incident");
        
        return created.Id;
    }
}