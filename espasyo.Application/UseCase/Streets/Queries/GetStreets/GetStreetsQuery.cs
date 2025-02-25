using espasyo.Domain.Entities;
using MediatR;

namespace espasyo.Application.UseCase.Streets.Queries.GetStreets;

public record GetStreetsQuery : IRequest<IEnumerable<StreetResult>>;

public record StreetResult
{
    public string? Street { get; init; } = null;
    public int? Barangay { get; init; } = null;
}