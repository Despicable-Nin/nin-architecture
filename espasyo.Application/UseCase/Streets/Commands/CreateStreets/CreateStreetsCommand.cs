using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using MediatR;

namespace espasyo.Application.UseCase.Streets.Commands.CreateStreets;

public record CreateStreetsCommand : IRequest<bool>
{
    public IEnumerable<StreetDto> Streets { get; init; } = [];
}

public record StreetDto
{
    public string? Street { get; init; }
    public string PrecinctId { get; init; } = string.Empty;
}

public class CreateStreetsHandler(ILogger<CreateStreetsHandler> logger, IStreetRepository repository) : IRequestHandler<CreateStreetsCommand, bool>
{
    public Task<bool> Handle(CreateStreetsCommand request, CancellationToken cancellationToken)
    {
        var streets = request.Streets.Where(x => !string.IsNullOrEmpty(x.Street) && Guid.TryParse(x.PrecinctId, out _))
            .Select(x => new Street(Guid.Parse(x.PrecinctId), x.Street!));

        var enumerable = streets as Street[] ?? streets.ToArray();
        if (enumerable!.Any())
        {
            return Task.FromResult(repository.CreateStreets(enumerable));
        }

        return Task.FromResult(false);
    }
}
