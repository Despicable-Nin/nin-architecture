using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.ForecastRuns.Commands.DeleteForecastRun;

public record DeleteForecastRunCommand(Guid Id) : IRequest<bool>;

public class DeleteForecastRunCommandHandler(
    IForecastRepository forecastRepository
) : IRequestHandler<DeleteForecastRunCommand, bool>
{
    public async Task<bool> Handle(DeleteForecastRunCommand request, CancellationToken cancellationToken)
    {
        return await forecastRepository.DeleteForecastRunAsync(request.Id);
    }
}
