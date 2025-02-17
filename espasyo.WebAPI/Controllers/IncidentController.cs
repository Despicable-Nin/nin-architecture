using espasyo.Application.Incidents.Commands.CreateIncident;
using espasyo.Application.Incidents.Queries.GetPaginatedList;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IncidentController( IMediator mediator) : ControllerBase
{
        
    // POST api/<IncidentController>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] CreateIncidentCommand incident)
    {
        var id = await mediator.Send( incident );
        return Created("api/incident", id);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaginated(int pageNumber = 1, int pageSize = 10)
    {
        return Ok(await mediator.Send(new GetPaginatedListQuery(pageNumber, pageSize)));
    }
}