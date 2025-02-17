using espasyo.Domain.Common;

namespace espasyo.Domain.Events;

public class IncidentCreatedEvent(string caseId, string address) : BaseEvent
{
    public string CaseId { get;  } = caseId;
    public string Address { get; } = address;
}