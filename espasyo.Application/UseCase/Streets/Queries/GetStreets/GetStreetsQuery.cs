using espasyo.Domain.Entities;
using MediatR;

namespace espasyo.Application.UseCase.Streets.Queries.GetStreets;

public record GetStreetsQuery : IRequest<GetStreetsResponse>;

public record StreetResult
{
    public string? Street { get; init; } 
    public int? Barangay { get; init; } 
}

public record GetStreetsResponse
{
    public GetStreetsResponse(IEnumerable<StreetResult>? streets)
    {
        Streets = streets;
    }
    public IEnumerable<StreetResult>? Streets { get; } = [];
}