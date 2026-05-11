using espasyo.Application.Interfaces;
using espasyo.Application.Incidents.Queries.GetPaginatedList;
using MediatR;

namespace espasyo.Application.Incidents.Queries.GetIncidentById;

public record GetIncidentByIdQuery(Guid Id) : IRequest<IncidentResult?>;

public class GetIncidentByIdQueryHandler(IIncidentRepository repository)
    : IRequestHandler<GetIncidentByIdQuery, IncidentResult?>
{
    public async Task<IncidentResult?> Handle(GetIncidentByIdQuery request, CancellationToken cancellationToken)
    {
        var incident = await repository.GetIncidentByIdAsync(request.Id);
        if (incident == null) return null;

        return new IncidentResult
        {
            Id = incident.Id,
            CaseId = incident.CaseId,
            Address = incident.Address,
            Severity = (int)incident.Severity,
            SeverityText = incident.Severity.ToString(),
            Motive = (int)incident.Motive,
            MotiveText = incident.Motive.ToString(),
            PoliceDistrict = (int)incident.PoliceDistrict,
            PoliceDistrictText = incident.PoliceDistrict.ToString(),
            OtherMotive = incident.AdditionalInformation,
            TimeStamp = incident.TimeStamp,
            Weather = (int)incident.Weather,
            WeatherText = incident.Weather.ToString(),
            CrimeType = (int)incident.CrimeType,
            CrimeTypeText = incident.CrimeType.ToString(),
        };
    }
}
