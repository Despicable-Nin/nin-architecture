using espasyo.Application.Common;
using espasyo.Application.Common.Interfaces;
using espasyo.Application.Common.Models;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.Incidents.Queries.GetPaginatedList;

public record GetPaginatedListQuery(int PageNumber, int PageSize) : IRequest<PaginatedList<IncidentResult>>
{
}

public record IncidentResult
{
    public Guid Id { get; init; }
    public string? CaseId { get; init; }
    public string? Address { get; init; }
    public int Severity { get; init; }
    public string SeverityText { get; init; } = string.Empty;
    public int CrimeType { get;  init; }
    public string CrimeTypeText { get; init; }
    public int Motive { get;  init; }
    public string MotiveText { get; init; } = string.Empty;
    public int PoliceDistrict { get;  init; }
    public string PoliceDistrictText { get; init; } = string.Empty;
    public string? OtherMotive { get; init; }
    public int Weather { get;  init; }
    public string WeatherText { get; init; } = string.Empty;
    public DateTimeOffset? TimeStamp { get; init; }
}

public class GetPaginatedListQueryHandler(IIncidentRepository repository) : IRequestHandler<GetPaginatedListQuery, PaginatedList<IncidentResult>>
{
    public async Task<PaginatedList<IncidentResult>> Handle(GetPaginatedListQuery request, CancellationToken cancellationToken)
    {
        
        var result = await repository.GetPaginatedIncidentsAsync(request.PageNumber, request.PageSize);

        var list = result.Item1.Select(x => new IncidentResult
            {
                CaseId = x.CaseId,
                Address = x.Address,
                Severity = (int)x.Severity,
                SeverityText = x.Severity.ToString(),
                Motive = (int)x.Motive,
                MotiveText = x.Motive.ToString(),
                PoliceDistrict = (int)x.PoliceDistrict,
                PoliceDistrictText = x.PoliceDistrict.ToString(),
                OtherMotive = x.AdditionalInformation,
                TimeStamp = x.TimeStamp,
                Weather = (int)x.Weather,
                WeatherText = x.Weather.ToString(),
                CrimeType = (int)x.CrimeType,
                CrimeTypeText = x.CrimeType.ToString(),
                Id = x.Id
            }).ToList()
            .AsReadOnly();
        
        return new PaginatedList<IncidentResult>(list, result.count, request.PageNumber, request.PageSize);

    }
}
