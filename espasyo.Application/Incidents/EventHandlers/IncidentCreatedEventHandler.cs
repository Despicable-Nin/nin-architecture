using espasyo.Application.Common.Interfaces;
using espasyo.Domain.Events;
using MediatR;

namespace espasyo.Application.Incidents.EventHandlers;

public class IncidentCreatedEventHandler(ILogger<IncidentCreatedEventHandler> logger, IGeocodeService geocodeService, IIncidentRepository repository) : INotificationHandler<IncidentCreatedEvent>
{
    public async Task Handle(IncidentCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handler invoked", [notification]);

        (double? lat, double? lon) = await geocodeService.GetLatLongAsync(notification.Address);

        var incident = await repository.GetIncidentByCaseIdAsync(notification.CaseId);
        
        if(incident == null) throw new KeyNotFoundException(nameof(notification.CaseId));

        incident.ChangeLatLong(lat, lon);

        logger.LogInformation("Nominatum:{lat} {lon}", lat, lon);
    }
}