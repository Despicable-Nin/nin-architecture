using MediatR;

namespace espasyo.Application.UseCase.Manpower.Commands.CreateManpower;

public class CreateManpowerCommand : IRequest<Guid>
{
    public Guid PrecinctId { get; set; }
    public int HeadCount { get; set; }
}
