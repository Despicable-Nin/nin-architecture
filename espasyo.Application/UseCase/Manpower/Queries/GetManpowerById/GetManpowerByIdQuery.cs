using espasyo.Application.UseCase.Manpower.Queries.GetAllManpower;
using MediatR;

namespace espasyo.Application.UseCase.Manpower.Queries.GetManpowerById;

public class GetManpowerByIdQuery : IRequest<ManpowerResponse?>
{
    public Guid Id { get; set; }
}