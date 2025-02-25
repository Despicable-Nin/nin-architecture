using espasyo.Domain.Entities;
using espasyo.Domain.Enums;

namespace espasyo.Application.Interfaces;

public interface IStreetRepository
{
    Task<IEnumerable<Street>> GetAllStreetsAsync();
    bool CreateStreets(IEnumerable<Street> streets);
}