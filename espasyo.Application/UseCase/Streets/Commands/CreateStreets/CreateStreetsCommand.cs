using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.UseCase.Streets.Commands.CreateStreets;

public record CreateStreetsCommand : IRequest<bool>
{
    public IEnumerable<StreetDto> Streets { get; init; } = [];
}

public record StreetDto
{
    public string? Street { get; init; }
    public Barangay Barangay { get; init; }
}

public class CreateStreetsHandler(ILogger<CreateStreetsHandler> logger, IStreetRepository repository) : IRequestHandler<CreateStreetsCommand, bool>
{
    public Task<bool> Handle(CreateStreetsCommand request, CancellationToken cancellationToken)
    {
        var streets = request.Streets.Select(x => new Street(x.Barangay, x.Street));

        var enumerable = streets as Street[] ?? streets.ToArray();
        if (enumerable!.Any())
        {
            return Task.FromResult(repository.CreateStreets(enumerable));
        }

        return Task.FromResult(false);
    }
}