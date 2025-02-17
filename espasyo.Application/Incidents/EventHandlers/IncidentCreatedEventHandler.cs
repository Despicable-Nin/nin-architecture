using espasyo.Domain.Events;
using MediatR;

namespace espasyo.Application.Incidents.EventHandlers;

public class IncidentCreatedEventHandler(ILogger<IncidentCreatedEventHandler> logger) : INotificationHandler<IncidentCreatedEvent>
{
    public Task Handle(IncidentCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handler invoked", [notification]);
        return Task.CompletedTask;
    }
}