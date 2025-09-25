using MediatR;

namespace espasyo.Application.UseCase.Manpower.Commands.UpdateManpower;

public class UpdateManpowerCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public int HeadCount { get; set; }
}
