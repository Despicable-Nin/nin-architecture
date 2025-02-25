using espasyo.Application.Interfaces;
using espasyo.Domain.Events;
using MediatR;

namespace espasyo.Application.Incidents.EventHandlers;

public class IncidentCreatedEventHandler(ILogger<IncidentCreatedEventHandler> logger, IGeocodeService geocodeService, IIncidentRepository repository) : INotificationHandler<IncidentCreatedEvent>
{
    public async Task Handle(IncidentCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handler invoked", [notification]);

        var latLongAddress = await geocodeService.GetLatLongAsync(notification.Address);

        var incident = await repository.GetIncidentByCaseIdAsync(notification.CaseId);
        
        if(incident == null) throw new KeyNotFoundException(nameof(notification.CaseId));

        incident.ChangeLatLong(latLongAddress.Latitude, latLongAddress.Longitude);

        logger.LogInformation("Nominatim:{lat} {lon}", latLongAddress.Latitude, latLongAddress.Longitude);

        incident.SanitizeAddress(latLongAddress.NewAddress);

        await repository.UpdateIncidentAsync(incident);
        
    }
}