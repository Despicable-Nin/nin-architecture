using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.UseCase.Streets.Queries.GetStreets;

public class GetStreetsQueryHandler(ILogger<GetStreetsQueryHandler> logger, IStreetRepository repository) : IRequestHandler<GetStreetsQuery, IEnumerable<StreetResult>>
{
    public async Task<IEnumerable<StreetResult>> Handle(GetStreetsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching streets");

        var streets = await repository.GetAllStreetsAsync();

        return streets.Select(x => new StreetResult
        {
            Street = x.Name,
            Barangay = (int)x.GetBarangay()
        });
    }
}