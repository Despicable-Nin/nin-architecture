using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using FluentValidation;
using MediatR;

namespace espasyo.Application.Incidents.Commands.BulkCreateIncidents;

public record BulkCreateIncidentsCommand : IRequest<BulkCreateIncidentsResponse>
{
    public IEnumerable<CreateIncidentRequest> Incidents { get; init; } = [];
}

public record CreateIncidentRequest
{
    public string? CaseId { get; init; }
    public string? Address { get; init; }
    public int Severity { get; init; }
    public int CrimeType { get; init; }
    public int Motive { get; init; }
    public Guid PrecinctId { get; init; }
    public string? AdditionalInfo { get; init; }
    public int Weather { get; init; }
    public DateTimeOffset? TimeStamp { get; init; }
}

public record BulkCreateIncidentsResponse
{
    public IEnumerable<IncidentCreationResult> Results { get; init; } = [];
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
}

public record IncidentCreationResult
{
    public string? CaseId { get; init; }
    public bool Success { get; init; }
    public Guid? IncidentId { get; init; }
    public string? ErrorMessage { get; init; }
}

public class BulkCreateIncidentsCommandValidator : AbstractValidator<BulkCreateIncidentsCommand>
{
    public BulkCreateIncidentsCommandValidator()
    {
        RuleFor(x => x.Incidents)
            .NotEmpty()
            .WithMessage("At least one incident must be provided");

        RuleFor(x => x.Incidents)
            .Must(incidents => incidents.Count() <= 100)
            .WithMessage("Cannot create more than 100 incidents at once");

        RuleForEach(x => x.Incidents).SetValidator(new CreateIncidentRequestValidator());
    }
}

public class CreateIncidentRequestValidator : AbstractValidator<CreateIncidentRequest>
{
    public CreateIncidentRequestValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Severity).NotEmpty();
        RuleFor(x => x.TimeStamp).NotEmpty();
    }
}

public class BulkCreateIncidentsCommandHandler(
    IIncidentRepository incidentRepository,
    IGeocodeService geocodeService,
    ILogger<BulkCreateIncidentsCommandHandler> logger)
    : IRequestHandler<BulkCreateIncidentsCommand, BulkCreateIncidentsResponse>
{
    public async Task<BulkCreateIncidentsResponse> Handle(BulkCreateIncidentsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating {Count} incidents in bulk", request.Incidents.Count());

        var results = new List<IncidentCreationResult>();
        var incidentsToCreate = new List<Incident>();

        foreach (var incidentRequest in request.Incidents)
        {
            try
            {
                logger.LogInformation("Processing incident with CaseId: {CaseId}", incidentRequest.CaseId);

                var incident = new Incident(
                    incidentRequest.CaseId,
                    incidentRequest.Address,
                    (SeverityEnum)incidentRequest.Severity,
                    (CrimeTypeEnum)incidentRequest.CrimeType,
                    (MotiveEnum)incidentRequest.Motive,
                    Barangay.Alabang, // Placeholder - the real association is via PrecinctId
                    (WeatherConditionEnum)incidentRequest.Weather,
                    incidentRequest.AdditionalInfo,
                    incidentRequest.TimeStamp
                );

                incident.PrecinctId = incidentRequest.PrecinctId;

                // Geocode the address
                logger.LogInformation("Geocoding address for CaseId: {CaseId}", incidentRequest.CaseId);
                var latLong = await geocodeService.GetLatLongAsync(incidentRequest.Address!);

                incident.ChangeLatLong(latLong.Latitude, latLong.Longitude);
                incident.SanitizeAddress(latLong.NewAddress);

                incidentsToCreate.Add(incident);
                
                results.Add(new IncidentCreationResult
                {
                    CaseId = incidentRequest.CaseId,
                    Success = true,
                    IncidentId = incident.Id
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing incident with CaseId: {CaseId}", incidentRequest.CaseId);
                
                results.Add(new IncidentCreationResult
                {
                    CaseId = incidentRequest.CaseId,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Create all successful incidents in bulk
        if (incidentsToCreate.Count > 0)
        {
            try
            {
                logger.LogInformation("Creating {Count} incidents in database", incidentsToCreate.Count);
                await incidentRepository.CreateIncidentsAsync(incidentsToCreate);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during bulk database insertion");
                
                // Mark all as failed if bulk insert fails
                for (int i = 0; i < results.Count; i++)
                {
                    if (results[i].Success)
                    {
                        results[i] = results[i] with { Success = false, ErrorMessage = "Database insertion failed" };
                    }
                }
            }
        }

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        logger.LogInformation("Bulk creation completed. Success: {SuccessCount}, Failures: {FailureCount}", 
            successCount, failureCount);

        return new BulkCreateIncidentsResponse
        {
            Results = results,
            SuccessCount = successCount,
            FailureCount = failureCount
        };
    }
}