using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.UseCase.Manpower.Commands.CreateManpower;

public class UpsertManpowerCommand : IRequest<Guid>
{
    public Guid PrecinctId { get; set; }
    public ShiftEnum Shift { get; set; }
    public int HeadCount { get; set; }
}

// Keep the old command for backward compatibility if needed
[Obsolete("Use UpsertManpowerCommand instead")]
public class CreateManpowerCommand : UpsertManpowerCommand
{
}
