using espasyo.Application.Common.Interfaces;
using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.Products.Queries.GetEnums;

public record GetEnumsQuery : IRequest<Dictionary<int, string>>
{
    public string Name { get; init; }
}

public class GetEnumsQueryHandler(IIncidentRepository repository) : IRequestHandler<GetEnumsQuery, Dictionary<int, string>>
{
    public Task<Dictionary<int, string>> Handle(GetEnumsQuery request, CancellationToken cancellationToken)
    {
        var name = request.Name ?? throw new ArgumentNullException(nameof(request.Name));
        return Task.FromResult(name.ToLower() switch
        {
            "severity" => repository.GetSeverityEnums(),
            "motive" => repository.GetMotiveEnums(),
            "weather" => repository.GetWeatherEnums(),
            "precinct" => repository.PoliceDistrictEnums(),
            "type" => repository.GetCrimeTypes(),
#pragma warning disable CA2208
            _ => throw new ArgumentOutOfRangeException(nameof(request.Name).ToLower(), @"Invalid request. Value can only be one of the ff. ['severity','motive','weather','precinct','type']")
#pragma warning restore CA2208
        });

    }
}