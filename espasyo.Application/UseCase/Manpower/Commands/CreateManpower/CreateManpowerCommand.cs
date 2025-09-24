using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.UseCase.Manpower.Commands.CreateManpower;

public class CreateManpowerCommand : IRequest<Guid>
{
    public Barangay Precinct { get; set; }
    public int Year { get; set; }
    public int AllocatedCount { get; set; }
    public int MildThreshold { get; set; }
    public int ModerateThreshold { get; set; }
    public int CriticalThreshold { get; set; }
}